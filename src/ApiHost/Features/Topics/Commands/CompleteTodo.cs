using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class CompleteTodo
{
    [WolverinePut("topics/{topicId}/todos/{todoId}/complete")]
    public static async Task<IResult> Put(Guid topicId, Guid todoId, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }
        var evt = topic.CompleteTodo(todoId);
        session.Update(topic);
        session.Events.Append(topicId, evt);
        return Results.Ok();
    }
}
