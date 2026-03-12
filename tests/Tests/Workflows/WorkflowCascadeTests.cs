using ApiHost;
using Tests.Infrastructure;

namespace Tests.Workflows;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowCascadeTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task deleting_user_should_cascade_delete_owned_topics()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Cascade", "Owner");
        var topic1 = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Topic 1");
        var topic2 = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Topic 2");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var view = await session.LoadAsync<UserView>(user.Id);
            return view is not null && view.TopicCount == 2;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{user.Id}");
            api.StatusCodeShouldBe(204);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();

            var userView = await session.LoadAsync<UserView>(user.Id);
            var topicDoc1 = await session.LoadAsync<Topic>(topic1.Id);
            var topicDoc2 = await session.LoadAsync<Topic>(topic2.Id);
            var topicView1 = await session.LoadAsync<TopicView>(topic1.Id);
            var topicView2 = await session.LoadAsync<TopicView>(topic2.Id);

            return userView is null
                   && topicDoc1 is null
                   && topicDoc2 is null
                   && topicView1 is null
                   && topicView2 is null;
        }, timeout: TimeSpan.FromSeconds(20));
    }
}
