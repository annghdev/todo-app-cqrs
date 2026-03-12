using FluentValidation;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Topics;

public class EditTodo
{
    public record EditTodoCommand(string Text);

    [WolverinePut("topics/{topicId}/todos/{todoId}")]
    public static async Task<IResult> Put(Guid topicId, Guid todoId, EditTodoCommand command, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }
        var evt = topic.EditTodo(todoId, command.Text);
        session.Update(topic);
        session.Events.Append(topicId, evt);
        return Results.Ok();
    }

    public class Validator : AbstractValidator<EditTodoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Todo text is required.");
        }
    }
}
