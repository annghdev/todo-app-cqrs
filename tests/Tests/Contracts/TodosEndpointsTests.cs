using ApiHost;
using Tests.Infrastructure;

namespace Tests.Contracts;

[Collection(IntegrationTestCollection.Name)]
public sealed class TodosEndpointsTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task should_add_edit_complete_and_uncheck_todo()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Todo", "Owner");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Todo Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "first item" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(200);
        });

        Guid todoId;
        await using (var session = fixture.Store.QuerySession())
        {
            var dbTopic = await session.LoadAsync<Topic>(topic.Id);
            todoId = dbTopic!.Todos.Single().Id;
        }

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { text = "updated item" }).ToUrl($"/topics/{topic.Id}/todos/{todoId}");
            api.StatusCodeShouldBe(200);
        });

        await fixture.Host.Scenario(api =>
        {
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/complete");
            api.StatusCodeShouldBe(200);
        });

        await fixture.Host.Scenario(api =>
        {
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/uncheck");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_validate_todo_payload()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture);
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Validation Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task should_return_not_found_when_removing_todo_from_unknown_topic()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/topics/{Guid.NewGuid()}/todos/{Guid.NewGuid()}");
            api.StatusCodeShouldBe(404);
        });
    }
}
