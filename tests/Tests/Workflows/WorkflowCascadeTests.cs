// File này test các kịch bản "Workflow Cascade" - tức là các hiệu ứng domino (phản ứng dây chuyền)
// trong hệ thống phân tán hoặc CQRS/Event Sourcing. 
// Ví dụ: Khi một User bị xóa, thì tất cả các Topic (Chủ đề) của người đó cũng phải tự động "bốc hơi" theo.
using ApiHost;
using ApiHost.Topics;
using Marten;
using Tests.Infrastructure;

namespace Tests.Workflows;

// Đánh dấu class này tham gia nhóm (Collection) dùng chung Database thử nghiệm.
[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowCascadeTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task deleting_user_should_emit_user_deleted_event()
    {
        // === BƯỚC 1: ARRANGE (CHUẨN BỊ) ===
        // Xóa sạch DB cũ, và tạo một con User nháp.
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Cascade", "Owner");

        // === BƯỚC 2: ACT (HÀNH ĐỘNG) ===
        // Gọi thẳng vào API Xóa User từ bên ngoài.
        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{user.Id}");
            // Khi xóa thành công, API quy chuẩn thường trả về 204 (No Content - Không có dữ liệu trả về)
            api.StatusCodeShouldBe(204);
        });

        // === BƯỚC 3: ASSERT (KIỂM TRA TÍNH ĐỒNG BỘ DỮ LIỆU ĐỌC/GHI - EVENTUAL CONSISTENCY) ===
        // Trong hệ thống xịn kiến trúc CQRS, dữ liệu sau khi Ghi có thể mất vài mini-giây để chép sang bên Đọc (View).
        // EventualAssert.TrueAsync là cú pháp test "Chờ đợi có chủ đích" (loop liên tục kiểm tra) 
        // với thời gian tối đa là 30 giây để xác nhận khi nào bên bảng Đọc báo đã "bay màu" là ok.
        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var userView = await session.LoadAsync<UserView>(user.Id); // Ngó vào bảng Query/View của User.
            return userView is null; // Nếu nó đã thành null tức là tiến trình đồng bộ xóa đã xong.
        }, timeout: TimeSpan.FromSeconds(30));

        // Test tiếp cái thứ hai: Xem ở kho "nhật ký sự kiện" (Event Store của Marten) 
        // đã ghi nhận vết tích lịch sử là có sự kiện "UserDeleted" xảy ra chưa.
        await using (var session = fixture.Store.QuerySession())
        {
            // Moi "cuốn sổ nhật ký" chứa tất cả các events liên quan trực tiếp đến ông user.Id này ra
            var userEvents = await session.Events.FetchStreamAsync(user.Id);
            // Soi xem trong đống nhật ký đó có dòng chữ "UserDeleted" hay không.
            Assert.Contains(userEvents, x => x.Data is UserDeleted);
        }
    }

    [Fact]
    public async Task user_deleted_handler_should_generate_commands_and_delete_topics()
    {
        // Kịch bản test này KHÔNG đi qua API HTTP, mà luồn thẳng vào "ruột" gọi TRỰC TIẾP các Handler 
        // (Trung tâm đầu não xử lý Logic Backend) để xem hiệu ứng dây chuyền xóa Topic có hoạt động chuẩn không.
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Cascade", "Handler");
        var topic1 = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Topic A");
        var topic2 = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Topic B");

        await using (var session = fixture.Store.LightweightSession())
        {
            var messages = await UserDeletedHandler.Handle(new UserDeleted(user.Id), session);
            var commands = messages.OfType<DeleteTopicCommand>().ToList();

            Assert.Equal(2, commands.Count);
            Assert.Contains(commands, x => x.TopicId == topic1.Id);
            Assert.Contains(commands, x => x.TopicId == topic2.Id);

            foreach (var command in commands)
            {
                DeleteTopicHandler.Handle(command, session);
            }

            await session.SaveChangesAsync();
        }

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var topicDoc1 = await session.LoadAsync<Topic>(topic1.Id);
            var topicDoc2 = await session.LoadAsync<Topic>(topic2.Id);
            return topicDoc1 is null && topicDoc2 is null;
        });

        await using (var session = fixture.Store.QuerySession())
        {
            var topic1Events = await session.Events.FetchStreamAsync(topic1.Id);
            var topic2Events = await session.Events.FetchStreamAsync(topic2.Id);

            Assert.Contains(topic1Events, x => x.Data is TopicDeleted deleted && deleted.TopicId == topic1.Id);
            Assert.Contains(topic2Events, x => x.Data is TopicDeleted deleted && deleted.TopicId == topic2.Id);
        }
    }

    [Fact]
    public async Task deleting_user_a_should_not_delete_topics_of_user_b()
    {
        await fixture.ResetDatabaseAsync();
        var userA = await ApiTestData.CreateUserAsync(fixture, "User", "A");
        var userB = await ApiTestData.CreateUserAsync(fixture, "User", "B");

        await ApiTestData.CreateTopicAsync(fixture, userA.Id, "Topic of A");
        var userBTopic1 = await ApiTestData.CreateTopicAsync(fixture, userB.Id, "Topic B1");
        var userBTopic2 = await ApiTestData.CreateTopicAsync(fixture, userB.Id, "Topic B2");

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{userA.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var userAView = await session.LoadAsync<UserView>(userA.Id);
            var userBView = await session.LoadAsync<UserView>(userB.Id);

            var userBTopicDoc1 = await session.LoadAsync<Topic>(userBTopic1.Id);
            var userBTopicDoc2 = await session.LoadAsync<Topic>(userBTopic2.Id);

            return userAView is null
                   && userBView is not null
                   && userBView.TopicCount == 2
                   && userBTopicDoc1 is not null
                   && userBTopicDoc2 is not null;
        }, timeout: TimeSpan.FromSeconds(30));
    }
}
