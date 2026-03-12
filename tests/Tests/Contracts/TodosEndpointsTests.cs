// File này chứa các kịch bản test (Integration Test) cho các API quản lý Todos (Việc cần làm).
// Nó kiểm tra từ lúc gửi User gửi Request lên hệ thống (API) mặt ngoài, cho đến lúc dữ liệu xử lý xong thành công.
using ApiHost;
using Tests.Infrastructure;

namespace Tests.Contracts;

// Đánh dấu class này tham gia vào nhóm (Collection) dùng chung cấu hình IntegrationTestFixture.
// Điều này giúp tái sử dụng lại cái Database PostgreSQL (đã dựng bằng Docker) rất mượt, khỏi tạo đi tạo lại.
[Collection(IntegrationTestCollection.Name)]
public sealed class TodosEndpointsTests(IntegrationTestFixture fixture)
{
    // [Fact] là cách thư viện xUnit nhận diện một hàm logic bên dưới chính là một bài test (test case).
    [Fact]
    public async Task should_add_edit_complete_and_uncheck_todo()
    {
        // === BƯỚC 1: ARRANGE (CHUẨN BỊ DỮ LIỆU) ===
        // Luôn phải xóa sạch dữ liệu cũ trong DB để test không lụa nhầm "dữ liệu rác" của test khác bỏ quên.
        await fixture.ResetDatabaseAsync();
        // Cần phải có trước một User và một Topic (Chủ đề) trong Database thì mới có chỗ nhét Todo vào.
        var user = await ApiTestData.CreateUserAsync(fixture, "Todo", "Owner");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Todo Topic");

        // === BƯỚC 2: ACT & ASSERT (HÀNH ĐỘNG VÀ KIỂM TRA CHO HÀM THÊM TODO) ===
        // Giả lập một yêu cầu HTTP gửi đến API từ bên ngoài. Thư viện Alba giúp gọi API trong code vô cùng dễ dàng.
        await fixture.Host.Scenario(api =>
        {
            // Yêu cầu POST có chứa JSON (body) gửi thẳng đến đường dẫn URL.
            api.Post.Json(new { text = "first item" }).ToUrl($"/topics/{topic.Id}/todos");
            // Kiểm tra luôn kết quả xem máy chủ có phản hồi mã HTTP là 200 (Thành công) hay không.
            api.StatusCodeShouldBe(200);
        });

        // Giờ ta cần ID của cái Todo vừa mới được tạo ra ở trên để dùng cho các bước Sửa và Cập Nhật status bên dưới.
        Guid todoId;
        // Mở một "Phiên Đọc" (QuerySession) đến Database của Marten.
        await using (var session = fixture.Store.QuerySession())
        {
            var dbTopic = await session.LoadAsync<Topic>(topic.Id);
            todoId = dbTopic!.Todos.Single().Id; // Lấy ID của phần tử trong danh sách Todos do Topic này sở hữu.
        }

        // === TIẾP TỤC TEST CÁC YÊU CẦU KHÁC DỰA VÀO TODO ID VỪA LẤY ===
        await fixture.Host.Scenario(api =>
        {
            // Dùng hàm PUT HTTP để cập nhật lại "text" (nội dung)
            api.Put.Json(new { text = "updated item" }).ToUrl($"/topics/{topic.Id}/todos/{todoId}");
            api.StatusCodeShouldBe(200);
        });

        await fixture.Host.Scenario(api =>
        {
            // Yêu cầu API bật cờ Hoàn Thành (Complete)
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/complete");
            api.StatusCodeShouldBe(200);
        });

        await fixture.Host.Scenario(api =>
        {
            // Yêu cầu API tắt cờ Hoàn Thành (Uncheck)
            api.Put.Url($"/topics/{topic.Id}/todos/{todoId}/uncheck");
            api.StatusCodeShouldBe(200);
        });
    }

    [Fact]
    public async Task should_validate_todo_payload()
    {
        // Test này kiểm tra xem API có chặn (Validation) những trường hợp nhập liệu tào lao hay không.
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture);
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Validation Topic");

        await fixture.Host.Scenario(api =>
        {
            // Gửi chữ rỗng đành lừa API
            api.Post.Json(new { text = "" }).ToUrl($"/topics/{topic.Id}/todos");
            // API phải nhả ra Bad Request 400 (Lỗi của client gửi lên) thì test này mới đúng.
            api.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task should_validate_edit_todo_payload()
    {
        await fixture.ResetDatabaseAsync();
        var user = await ApiTestData.CreateUserAsync(fixture, "Todo", "Validation");
        var topic = await ApiTestData.CreateTopicAsync(fixture, user.Id, "Edit Todo Validation");

        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { text = "existing" }).ToUrl($"/topics/{topic.Id}/todos");
            api.StatusCodeShouldBe(200);
        });

        Guid todoId;
        await using (var session = fixture.Store.QuerySession())
        {
            todoId = (await session.LoadAsync<Topic>(topic.Id))!.Todos.Single().Id;
        }

        await fixture.Host.Scenario(api =>
        {
            api.Put.Json(new { text = "" }).ToUrl($"/topics/{topic.Id}/todos/{todoId}");
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
