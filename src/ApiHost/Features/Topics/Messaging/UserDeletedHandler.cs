using Marten;
using Wolverine;
using Wolverine.Marten;

namespace ApiHost.Topics;

public class UserDeletedHandler
{
    public static async Task<IEnumerable<object>> Handle(UserDeleted e, IDocumentSession session)
    {
        // 1. Tìm tất cả ID của Topic thuộc về User này
        var topicIds = await session.Query<Topic>()
            .Where(t => t.CreatedBy == e.UserId)
            .Select(t => t.Id)
            .ToListAsync();

        // 2. Tạo danh sách các lệnh cần thực hiện
        var messages = new List<object>();
        foreach (var id in topicIds)
        {
            // Chúng ta gửi một command yêu cầu xóa Topic
            messages.Add(new DeleteTopicCommand(id, e.UserId));
        }

        return messages; // Wolverine sẽ tự động gửi tất cả các lệnh này
    }
}

public record DeleteTopicCommand(Guid TopicId, Guid UserId);
public class DeleteTopicHandler
{
    public static void Handle(DeleteTopicCommand command, IDocumentSession session)
    {
        // Ghi Event vào stream của Topic đó
        // Marten v7 cho phép append event vào stream mà không cần StartStream nếu đã biết Id
        session.Events.Append(command.TopicId, new TopicDeleted(command.TopicId, command.UserId));

        // Xóa Aggregate (nếu bạn muốn xóa cả dữ liệu gốc)
        session.Delete<Topic>(command.TopicId);
    }
}
