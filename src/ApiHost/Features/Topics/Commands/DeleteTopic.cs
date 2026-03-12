using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class DeleteTopic
{
    [WolverineDelete("Topics/{id}")]
    public static async Task<IResult> Delete(Guid id, IDocumentSession session)
    {
        var userId = session.Query<Topic>().Where(t => t.Id == id)
            .Select(t => t.CreatedBy)
            .FirstOrDefault();

        if(userId == Guid.Empty)
        {
            return Results.NotFound();
        }

        session.Delete<Topic>(id);

        session.Events.Append(id, new TopicDeleted(id, userId));

        return Results.NoContent();
    }
}
