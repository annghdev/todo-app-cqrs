using FluentValidation;
using Marten;
using Wolverine.Http;

namespace ApiHost.Topics;

public class CreateTopic
{
    public record CreateTopicCommand(Guid UserId, string Title);


    [WolverinePost("Topics")]
    public static IResult Post(CreateTopicCommand command, IDocumentSession session)
    {
        var topic = new Topic(command.UserId, command.Title);
        session.Store(topic);
        var evt = new TopicCreated(topic.Id, command.UserId, command.Title);
        session.Events.Append(topic.Id, evt);

        return Results.Created($"/topics/{topic.Id}", topic);
    }

    public class Validator : AbstractValidator<CreateTopicCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Topic name is required.");
        }
    }
}
