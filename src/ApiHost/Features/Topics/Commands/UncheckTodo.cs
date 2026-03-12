using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class UncheckTodo
{
    [WolverinePut("topics/{topicId}/todos/{todoId}/uncheck")]
    public static async Task<IResult> Put(Guid topicId, Guid todoId, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }
        var evt = topic.UncheckTodo(todoId);
        session.Update(topic);
        session.Events.Append(topicId, evt);
        return Results.Ok();
    }
}
