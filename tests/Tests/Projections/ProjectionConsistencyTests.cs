// File này test tính năng "Projections" (Máy chiếu) trong kiến trúc CQRS.
// Tưởng tượng: Khi có một sự kiện "Lưu Database" xảy ra (Write logic), hệ thống sẽ dùng Máy chiếu (Projection)
// để tự động cập nhật dữ liệu sang một Bảng Đọc (Read Model / View) riêng biệt giúp tối ưu tốc độ truy xuất.
// Quá trình cập nhật bảng Đọc này thường bất đồng bộ và cần kiên nhẫn để dữ liệu xuất hiện.
using ApiHost;
using Tests.Infrastructure;

namespace Tests.Projections;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProjectionConsistencyTests(IntegrationTestFixture fixture)
{
    // Test: Sau khi Tạo, Sửa, Xóa thông tin User ở đầu vào (API), bảng Đọc cũng phải theo dõi và cập nhật y chang.
    [Fact]
    public async Task user_view_should_track_create_update_and_delete_events()
    {
        await fixture.ResetDatabaseAsync();
        // 1. Gửi request tạo User
        var user = await ApiTestData.CreateUserAsync(fixture, "Alice", "Nguyen");

        // 2. Chờ cho tới khi Projection chạy xong và tạo ra một View "UserView" tương ứng bên bảng Đọc
        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<UserView>(user.Id); // Lấy data từ bảng Đọc
            return view is not null && view.FullName == "Alice Nguyen";
        });

        // 3. Giả vờ gọi API sửa tên User
        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { firstName = "Linh", lastName = "Tran" }).ToUrl($"/users/{user.Id}/name");
            api.StatusCodeShouldBe(204);
        });

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { format = 1 }).ToUrl($"/users/{user.Id}/display-name-format");
            api.StatusCodeShouldBe(200);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<UserView>(user.Id);
            return view is not null
                   && view.FullName == "Linh Tran"
                   && view.DisplayName == "Tran Linh";
        });

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{user.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<UserView>(user.Id) is null;
        });
    }

    [Fact]
    public async Task topic_view_should_track_todo_mutations_and_delete_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Topic", "Projection");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Projection Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "projection todo" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(200);
        });

        Guid todoId;
        await using (var session = fixture.Store.QuerySession())
        {
            todoId = (await session.LoadAsync<Topic>(topic.Id))!.Todos.Single().Id;
        }

        await fixture.Host.Scenario(api =>
        {
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/complete");
            api.StatusCodeShouldBe(200);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<TopicView>(topic.Id);
            if (view is null)
            {
                return false;
            }

            var todoView = view.Todos.SingleOrDefault(x => x.Id == todoId);
            return view.TotalTodoCount == 1 && todoView is not null && todoView.Complete;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/topics/{topic.Id}/todos/{todoId}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<TopicView>(topic.Id);
            return view is not null && view.TotalTodoCount == 0 && view.Todos.Count == 0;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/Topics/{topic.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<TopicView>(topic.Id) is null;
        });
    }

    [Fact]
    public async Task deleting_topic_should_decrease_owner_topic_count_without_affecting_other_users()
    {
        await fixture.ResetDatabaseAsync();

        var owner = await ApiTestData.CreateUserAsync(fixture, "Owner", "User");
        var otherUser = await ApiTestData.CreateUserAsync(fixture, "Other", "User");

        var ownerTopic1 = await ApiTestData.CreateTopicAsync(fixture, owner.Id, "Owner Topic 1");
        await ApiTestData.CreateTopicAsync(fixture, owner.Id, "Owner Topic 2");
        await ApiTestData.CreateTopicAsync(fixture, otherUser.Id, "Other User Topic");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var ownerView = await session.LoadAsync<UserView>(owner.Id);
            var otherUserView = await session.LoadAsync<UserView>(otherUser.Id);

            return ownerView is not null
                   && otherUserView is not null
                   && ownerView.TopicCount == 2
                   && otherUserView.TopicCount == 1;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/Topics/{ownerTopic1.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var ownerView = await session.LoadAsync<UserView>(owner.Id);
            var otherUserView = await session.LoadAsync<UserView>(otherUser.Id);

            return ownerView is not null
                   && otherUserView is not null
                   && ownerView.TopicCount == 1
                   && otherUserView.TopicCount == 1;
        });
    }
}
