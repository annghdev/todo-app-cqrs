using ApiHost;
using System.Text.Json;
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
    public async Task should_validate_create_topic_payload_and_return_problem_details()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Invalid", "Topic");

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { userId = user.Id, title = "" }).ToUrl("/Topics");
            api.StatusCodeShouldBe(400);
        });

        var payload = scenario.ReadAsJson<Dictionary<string, JsonElement>>();
        Assert.Contains(payload.Keys, key => key.Equals("title", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.Keys, key => key.Equals("status", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.Keys, key => key.Equals("errors", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public async Task should_get_topic_by_id()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Query", "Topic");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Read Me");

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            return await session.LoadAsync<TopicView>(topic.Id) is not null;
        });

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url($"/topics/{topic.Id}");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_return_not_found_for_unknown_topic_query()
    {
        await fixture.ResetDatabaseAsync();

        await fixture.Host.Scenario(api =>
        {
            api.Get.Url($"/topics/{Guid.NewGuid()}");
            api.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task should_edit_topic_title_successfully()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Edit", "Topic");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Old Title");

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { title = "New Title" }).ToUrl($"/Topics/{topic.Id}");
            api.StatusCodeShouldBe(200);
        });

        await EventualAssert.TrueAsync(async () =>
        {
            await using var session = fixture.Store.QuerySession();
            var topicView = await session.LoadAsync<TopicView>(topic.Id);
            return topicView is not null && topicView.Title == "New Title";
        });
    }

    [Fact]
    public async Task should_validate_edit_topic_payload()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Edit", "Validation");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Valid Title");

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { title = "" }).ToUrl($"/Topics/{topic.Id}");
            api.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task should_filter_topics_by_name_in_topic_list()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Search", "User");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "Alpha Project");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "Beta Board");

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Get.Url("/topics?page=1&size=10&name=Alpha");
            api.StatusCodeShouldBe(200);
        });

        var payload = scenario.ReadAsJson<Dictionary<string, JsonElement>>();
        var data = GetArray(payload);
        Assert.Single(data);
        Assert.Equal("Alpha Project", GetString(data[0], "title"));
    }

    [Fact]
    public async Task should_sort_topics_by_title_ascending()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Sort", "User");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "Zulu");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "Alpha");

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Get.Url("/topics?page=1&size=10&orderBy=title&isDescending=false");
            api.StatusCodeShouldBe(200);
        });

        var payload = scenario.ReadAsJson<Dictionary<string, JsonElement>>();
        var titles = GetArray(payload)
            .Select(node => GetString(node, "title"))
            .ToArray();

        Assert.Equal(new[] { "Alpha", "Zulu" }, titles);
    }

    [Fact]
    public async Task should_return_paging_metadata_for_topic_list()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Paging", "User");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "T1");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "T2");
        await ApiTestData.CreateTopicAsync(fixture, user.Id, "T3");

        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Get.Url("/topics?page=2&size=1&orderBy=title&isDescending=false");
            api.StatusCodeShouldBe(200);
        });

        var payload = scenario.ReadAsJson<Dictionary<string, JsonElement>>();
        Assert.Equal(2, GetIntByCandidates(payload, "pageNumber", "page", "pageIndex"));
        Assert.Equal(1, GetIntByCandidates(payload, "pageSize", "size", "pageSizeValue"));
        Assert.Equal(3, GetIntByCandidates(payload, "totalCount", "total", "totalItemCount"));
        Assert.Single(GetArray(payload));
    }

    private static JsonElement[] GetArray(Dictionary<string, JsonElement> payload)
    {
        foreach (var (_, value) in payload)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray().ToArray();
            }
        }

        throw new InvalidOperationException("No array payload property was found.");
    }

    private static int GetIntByCandidates(Dictionary<string, JsonElement> payload, params string[] candidates)
    {
        foreach (var key in candidates)
        {
            var hit = payload.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(hit.Key) && hit.Value.ValueKind == JsonValueKind.Number)
            {
                return hit.Value.GetInt32();
            }
        }

        throw new InvalidOperationException($"None of the keys '{string.Join(", ", candidates)}' was found.");
    }

    private static string GetString(JsonElement payload, string key)
    {
        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException($"Key '{key}' was not found in item payload.");
    }
}
