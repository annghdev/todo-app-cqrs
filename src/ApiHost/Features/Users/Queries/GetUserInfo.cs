using Marten;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace ApiHost.Users;

public class GetUserInfo
{
    [WolverineGet("v1/users/{id}")]
    public async Task<UserView?> Get(Guid id, IQuerySession session)
    {
        return await session.LoadAsync<UserView>(id);
    }

    [WolverineGet("v2/users/{id}")]
    public UserView GetV2(Guid id, [Document] UserView view) => view;
}
