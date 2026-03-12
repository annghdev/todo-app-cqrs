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
}
