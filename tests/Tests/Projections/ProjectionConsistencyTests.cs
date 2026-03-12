using ApiHost;
using Tests.Infrastructure;

namespace Tests.Projections;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProjectionConsistencyTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task user_view_should_track_create_update_and_delete_events()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Alice", "Nguyen");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<UserView>(user.Id);
            return view is not null && view.FullName == "Alice Nguyen";
        });

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
}
