using FluentValidation;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace ApiHost.Topics;

public class EditTopic
{
    public record EditTopicCommand(string Title);

    [WolverinePut("Topics/{id}")]
    public static async Task<IResult> Post(Guid id, EditTopicCommand command, IDocumentSession session)
    {
        var topic = await session.LoadAsync<Topic>(id);

        if (topic is null)
        {
            return Results.NotFound();
        }

        var evt = topic.Edit(command.Title);

        session.Update(topic);

        session.Events.Append(id, evt);

        return Results.Ok();
    }

    public class Validator : AbstractValidator<EditTopicCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Topic name is required.");
        }
    }
}
