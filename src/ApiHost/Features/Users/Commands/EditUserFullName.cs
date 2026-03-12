using FluentValidation;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Users;

public class EditUserFullName
{
    public record EditCommand(string FirstName, string LastName);

    [WolverinePut("users/{id}/name")]
    public async Task<IResult> Put(Guid id, EditCommand command, IDocumentSession session)
    {
        var user = await session.LoadAsync<User>(id);
        if (user == null)
        {
            return Results.NotFound();
        }
        var evt = user.UpdateName(command.FirstName, command.LastName);
        session.Update(user);
        session.Events.Append(id, evt);
        return Results.NoContent();
    }

    public class Validator : AbstractValidator<EditCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        }
    }
}
