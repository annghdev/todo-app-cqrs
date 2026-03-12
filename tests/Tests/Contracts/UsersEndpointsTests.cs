using ApiHost;
using System.Text.Json;
using Tests.Infrastructure;

namespace Tests.Contracts;

[Collection(IntegrationTestCollection.Name)]
public sealed class UsersEndpointsTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task should_boot_api_and_redirect_root()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url("/");
            api.StatusCodeShouldBe(302);
        });
    }

    [Fact]
    public async Task should_create_user_and_read_user_projection()
    {
        await fixture.ResetDatabaseAsync();

        var user = await ApiTestData.CreateUserAsync(fixture, "Alice", "Nguyen");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var userView = await session.LoadAsync<UserView>(user.Id);
            return userView is not null && userView.FullName == "Alice Nguyen";
        });

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url($"/v1/users/{user.Id}");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_create_user_v2_and_get_user_v2()
    {
        await fixture.ResetDatabaseAsync();

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { firstName = "V2", lastName = "User" }).ToUrl("/v2/users");
            api.StatusCodeShouldBe(200);
        });

        var user = scenario.ReadAsJson<User>();

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<UserView>(user.Id) is not null;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url($"/v2/users/{user.Id}");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_return_not_found_for_unknown_user_v2_query()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url($"/v2/users/{Guid.NewGuid()}");
            api.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task should_validate_user_create_payload()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { firstName = "", lastName = "" }).ToUrl("/v1/users");
            api.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task should_return_not_found_for_unknown_user_update()
    {
        await fixture.ResetDatabaseAsync();
        var unknownUserId = Guid.NewGuid();

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { firstName = "A", lastName = "B" }).ToUrl($"/users/{unknownUserId}/name");
            api.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task should_validate_display_name_format_payload_and_return_problem_details()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Display", "Name");

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { format = 999 }).ToUrl($"/users/{user.Id}/display-name-format");
            api.StatusCodeShouldBe(400);
        });

        var payload = scenario.ReadAsJson<Dictionary<string, JsonElement>>();
        Assert.Contains(payload.Keys, key => key.Equals("title", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.Keys, key => key.Equals("status", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.Keys, key => key.Equals("errors", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task should_delete_existing_user()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Bob", "Tran");

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{user.Id}");
            api.StatusCodeShouldBe(204);
        });
    }

    [Fact]
    public async Task should_return_not_found_when_deleting_unknown_user()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Delete.Url($"/users/{Guid.NewGuid()}");
            api.StatusCodeShouldBe(404);
        });
    }
}
