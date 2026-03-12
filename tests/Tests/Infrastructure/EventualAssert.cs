// File này chứa thuật toán hỗ trợ kiểm tra dữ liệu theo cơ chế "Eventual Consistency".
// Khi dùng CQRS, luồng Ghi diễn ra rất nhanh nhưng luồng Đọc có thể cần một chút thì giờ đồng bộ.
// Công cụ này giúp bài test "kiên nhẫn" lặp đi lặp lại việc kiểm tra thay vì tạch luôn ngay lần so sánh đầu tiên.
namespace Tests.Infrastructure;

public static class EventualAssert
{
    // Hàm nhận vào một Func (Một khối code điều kiện), giới hạn thời gian lặp (timeout) 
    // và thời gian nghỉ giữa mỗi lần lặp (pollInterval).
    public static async Task TrueAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(30); // Giới hạn kiên nhẫn mặc định là 30 giây.
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(200); // Mỗi lần tra hỏi lại DB, nghỉ xả hơi 0.2 giây.
        var deadline = DateTime.UtcNow.Add(maxWait); // Thời điểm "hết kiên nhẫn" (hiện tại + 30s)

        // Vòng lặp liên tục dội vào DB để tra hỏi xem dữ liệu đã đồng bộ chưa...
        while (DateTime.UtcNow <= deadline)
        {
            try
            {
                if (await condition()) // Gọi khối code kiểm tra (do bài test viết).
                {
                    return; // Nếu chuẩn rồi (trả về true) thì thoát hàm báo thành công ngay lập tức!
                }
            }
            catch
            {
                // Nếu gặp lỗi giật cục do chưa kịp khởi tạo bảng hoặc lỗi bất chợt, "cười trừ" nuốt lỗi (catch trống) 
                // và kiên trì đợi lần hỏi tiếp theo.
            }

            // Tạm dừng chạy code (Sleep) 0.2 giây trước khi lặp lại vòng lặp.
            await Task.Delay(interval);
        }

        // Thoát khỏi vòng lặp tức là đã hết giờ mà điều kiện vẫn không thỏa, lúc này mới ném thông báo Thất Bại (Fail Test).
        throw new TimeoutException($"Condition was not met within {maxWait.TotalSeconds} seconds.");
    }
}
