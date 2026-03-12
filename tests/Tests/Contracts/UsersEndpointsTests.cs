using ApiHost;
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
}
