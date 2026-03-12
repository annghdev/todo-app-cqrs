using FluentValidation;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Users;

public class UpdateDisplayNameFormant
{
    public record UpdateDisplayNameFormatCommand(DisplayNameFormat Format);

    [WolverinePut("users/{id}/display-name-format")]
    public async Task<IResult> Put(Guid id, UpdateDisplayNameFormatCommand command, IDocumentSession session)
    {
        var user = await session.LoadAsync<User>(id);
        if (user == null)
        {
            return Results.NotFound();
        }
        var evt = user.UpdateDisplayNameFormat(command.Format);
        session.Update(user);
        session.Events.Append(id, evt);
        return Results.Ok();
    }

    public class Validator : AbstractValidator<UpdateDisplayNameFormatCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Format).IsInEnum().WithMessage("Invalid display name format.");
        }
    }
}
