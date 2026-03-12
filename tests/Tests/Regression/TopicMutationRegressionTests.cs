// File này chứa các bài test kiểu "Regression" (Hồi quy).
// Mục đích là để bảo vệ hệ thống khỏi các lỗ hổng (edge cases), và đảm bảo rằng 
// khi user "cố tình" làm sai (VD gửi request chỉnh sửa một mục không tồn tại), hệ thống phải từ chối ngay.
// Nếu hệ thống không từ chối mà lại sinh ra rác trong Database (sự kiện rác) thì là LỖI NGHIÊM TRỌNG.
using ApiHost;
using Tests.Infrastructure;

namespace Tests.Regression;

[Collection(IntegrationTestCollection.Name)]
public sealed class TopicMutationRegressionTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task remove_todo_should_not_delete_topic_document_and_should_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Remove", "Todo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Regression Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "todo-to-remove" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(200);
        });

        Guid todoId;
        await using (var session = fixture.Store.QuerySession())
        {
            var topicDoc = await session.LoadAsync<Topic>(topic.Id);
            todoId = topicDoc!.Todos.Single().Id;
        }

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/topics/{topic.Id}/todos/{todoId}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var topicDoc = await session.LoadAsync<Topic>(topic.Id);
            if (topicDoc is null)
            {
                return false;
            }

            return topicDoc.Todos.All(x => x.Id != todoId);
        });

        await using (var session = fixture.Store.QuerySession())
        {
            var events = await session.Events.FetchStreamAsync(topic.Id);
            Assert.Contains(events, x => x.Data is TodoRemoved);
        }
    }

    [Fact]
    public async Task complete_todo_should_persist_completion_and_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Complete", "Todo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Completion Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "todo-to-complete" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(200);
        });

        Guid todoId;
        await using (var session = fixture.Store.QuerySession())
        {
            var topicDoc = await session.LoadAsync<Topic>(topic.Id);
            todoId = topicDoc!.Todos.Single().Id;
        }

        await fixture.Host.Scenario(api =>
        {
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/complete");
            api.StatusCodeShouldBe(200);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var topicDoc = await session.LoadAsync<Topic>(topic.Id);
            return topicDoc?.Todos.Single(x => x.Id == todoId).Completed == true;
        });

        await using (var session = fixture.Store.QuerySession())
        {
            var events = await session.Events.FetchStreamAsync(topic.Id);
            Assert.Contains(events, x => x.Data is TodoCompleted);
        }
    }

    // Test: Nếu cố Sửa một cái Todo mà truyền sai ID (ID không có thật),
    // máy chủ phải báo Lỗi Server thay vì chạy trơn tru, VÀ không được phép lưu sự kiện này vào CSDL.
    [Fact]
    public async Task edit_todo_with_missing_todo_id_should_return_server_error_and_not_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Edit", "MissingTodo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Missing Todo Topic");
        var missingTodoId = Guid.NewGuid(); // Tạo đại 1 cái ID bừa bãi không có thật

        // Assert.ThrowsAsync là hàm test yêu cầu code nằm bên trong bắt buộc phải sinh ra LỖI kiểu InvalidOperationException.
        // Nếu code bên trong KHÔNG LỖI (chạy qua lọt) -> Test thất bại (fail test)! Do đã vờn sai mà lại còn cho qua.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Host.Scenario(api =>
            {
                api.Put.Json(new { text = "updated text" }).ToUrl($"/topics/{topic.Id}/todos/{missingTodoId}");
            }));

        // Kiểm tra kép 2 lớp: Check ngay cuộn "Sổ tay" Event Store của Topic này...
        await using var session = fixture.Store.QuerySession();
        var events = await session.Events.FetchStreamAsync(topic.Id);
        // ... Đảm bảo không có thằng ảo tưởng nào ghi sự kiện "TodoEdited" với một cái ID rác vào sổ tay.
        Assert.DoesNotContain(events, x => x.Data is TodoEdited edited && edited.TodoId == missingTodoId);
    }

    [Fact]
    public async Task complete_todo_with_missing_todo_id_should_return_server_error_and_not_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Complete", "MissingTodo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Missing Todo Topic");
        var missingTodoId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Host.Scenario(api =>
            {
                api.Put.Url($"/topics/{topic.Id}/todos/{missingTodoId}/complete");
            }));

        await using var session = fixture.Store.QuerySession();
        var events = await session.Events.FetchStreamAsync(topic.Id);
        Assert.DoesNotContain(events, x => x.Data is TodoCompleted completed && completed.TodoId == missingTodoId);
    }

    [Fact]
    public async Task uncheck_todo_with_missing_todo_id_should_return_server_error_and_not_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Uncheck", "MissingTodo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Missing Todo Topic");
        var missingTodoId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Host.Scenario(api =>
            {
                api.Put.Url($"/topics/{topic.Id}/todos/{missingTodoId}/uncheck");
            }));

        await using var session = fixture.Store.QuerySession();
        var events = await session.Events.FetchStreamAsync(topic.Id);
        Assert.DoesNotContain(events, x => x.Data is TodoUnchecked uncheckedEvent && uncheckedEvent.TodoId == missingTodoId);
    }

    [Fact]
    public async Task remove_todo_with_missing_todo_id_should_return_server_error_and_not_append_event()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Remove", "MissingTodo");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Missing Todo Topic");
        var missingTodoId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Host.Scenario(api =>
            {
                api.Delete.Url($"/topics/{topic.Id}/todos/{missingTodoId}");
            }));

        await using var session = fixture.Store.QuerySession();
        var events = await session.Events.FetchStreamAsync(topic.Id);
        Assert.DoesNotContain(events, x => x.Data is TodoRemoved removed && removed.TodoId == missingTodoId);
    }
}
