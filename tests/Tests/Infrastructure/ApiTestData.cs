// File này đóng vai trò như một "nhà máy sản xuất" dữ liệu giả (Dummy Data) 
// để các bài test gọi đến thay vì phải lặp đi lặp lại những đoạn code tạo User/Topic giống hệt nhau.
using ApiHost;

namespace Tests.Infrastructure;

// static class tức là class này chỉ dùng để chứa các hàm công cụ (helper functions), 
// không cần tạo mới (new) nó ra khi xài.
public static class ApiTestData
{
    // Hàm này giúp tạo nhanh một User (Người dùng) thông qua gọi tắt API bề mặt.
    public static async Task<User> CreateUserAsync(
        IntegrationTestFixture fixture,
        string firstName = "John",
        string lastName = "Doe")
    {
        // Nhờ thư viện Alba, ta dùng Host.Scenario để đóng giả thành một client gửi thông tin đăng ký lên API.
        var scenario = await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { firstName, lastName }).ToUrl("/v1/users");
            api.StatusCodeShouldBe(200); // Đảm bảo API không báo lỗi.
        });

        // Lấy cái JSON máy chủ trả về và tự động ép kiểu (Parse/Deserialize) thành dạng đối tượng User (C# object).
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
