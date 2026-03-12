using FluentValidation;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Users;

public class CreateUser
{
    public record CreateUserCommand(string FirstName, string LastName);

    [WolverinePost("v1/users")]
    public User PostV1(CreateUserCommand command, IDocumentSession session)
    {
        var user = new User(command.FirstName, command.LastName);
        session.Store(user);
        var evt = new UserCreated(user.Id, user.DisplayName, user.FullName);
        session.Events.Append(user.Id, evt);
        return user;
    }

    [WolverinePost("v2/users")]
    public (User, IMartenOp) PostV2(CreateUserCommand command)
    {
        var user = new User(command.FirstName, command.LastName);
        var evt = new UserCreated(user.Id, user.DisplayName, user.FullName);
        return (user, MartenOps.StartStream<UserView>(user.Id, evt));
    }

    public class Validator : AbstractValidator<CreateUserCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        }
    }
}
