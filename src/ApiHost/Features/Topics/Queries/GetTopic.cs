using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class GetTopic
{
    [WolverineGet("/topics/{id}")]
    public static async Task<TopicView?> Handle(Guid id, IQuerySession session)
    {
        return await session.LoadAsync<TopicView>(id);
    }
}
