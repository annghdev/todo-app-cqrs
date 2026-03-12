using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class RemoveTodo
{
    [WolverineDelete("topics/{topicId}/todos/{todoId}")]
    public static async Task<IResult> Delete(Guid topicId, Guid todoId, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }

        var evt = topic.RemoveTodo(todoId);

        session.Update(topic);

        session.Events.Append(topicId, evt);

        return Results.NoContent();
    }
}
