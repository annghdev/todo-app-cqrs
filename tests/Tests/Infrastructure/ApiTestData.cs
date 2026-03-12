using ApiHost;

namespace Tests.Infrastructure;

public static class ApiTestData
{
    public static async Task<User> CreateUserAsync(
        IntegrationTestFixture fixture,
        string firstName = "John",
        string lastName = "Doe")
    {
        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { firstName, lastName }).ToUrl("/v1/users");
            api.StatusCodeShouldBe(200);
        });

        return scenario.ReadAsJson<User>();
    }

    public static async Task<Topic> CreateTopicAsync(
        IntegrationTestFixture fixture,
        Guid userId,
        string title = "New Topic")
    {
        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { userId, title }).ToUrl("/Topics");
            api.StatusCodeShouldBe(201);
        });

        return scenario.ReadAsJson<Topic>();
    }
}
