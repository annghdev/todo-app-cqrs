using Marten;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Users;

public class DeleteUser
{
    [WolverineDelete("users/{id}")]
    public async Task<IResult> Delete(Guid id, IDocumentSession session, IMessageBus bus)
    {
        var user = await session.LoadAsync<User>(id);
        if (user == null)
        {
            return Results.NotFound();
        }

        var evt = user.MarkDelete();
        session.Delete(user);
        session.Events.Append(id, evt);
        await bus.PublishAsync(evt);
        return Results.NoContent();
    }
}
