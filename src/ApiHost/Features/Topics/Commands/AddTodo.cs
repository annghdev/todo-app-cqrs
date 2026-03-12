using FluentValidation;
using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class AddTodo
{
    public record AddTodoCommand(string Text);

    [WolverinePost("topics/{id}/todos")]
    public static async Task<IResult> Post(Guid id, AddTodoCommand command, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(id);
        if (topic is null)
        {
            return Results.NotFound();
        }
        var evt = topic.AddTodo(command.Text);
        session.Update(topic);
        session.Events.Append(id, evt);
        return Results.Ok();
    }

    public class Validator : AbstractValidator<AddTodoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Todo text is required.");
        }
    }
}
