using ApiHost;
using Tests.Infrastructure;

namespace Tests.Contracts;

[Collection(IntegrationTestCollection.Name)]
public sealed class TopicsEndpointsTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task should_create_topic_and_get_it_in_list()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Topic", "Owner");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Backend Tests");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<TopicView>(topic.Id) is not null;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url("/topics?page=1&size=10");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_return_not_found_for_unknown_topic_update()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { title = "Updated" }).ToUrl($"/Topics/{Guid.NewGuid()}");
            api.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task should_return_not_found_when_adding_todo_to_unknown_topic()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "todo item" }).ToUrl($"/topics/{Guid.NewGuid()}/todos");
            api.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task should_create_and_delete_topic()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Delete", "Case");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Disposable Topic");

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/Topics/{topic.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<Topic>(topic.Id) is null;
        });
    }
}
