using Wolverine;
using Wolverine.Runtime;

namespace ApiHost.Middlewares;

public class LoggingMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(
        MessageContext context,
        Envelope envelope,
        Func<Task> next)
    {
        var messageType = envelope.Message!.GetType().Name;

        _logger.LogInformation(
            "Handling message {MessageType} with Id {MessageId}",
            messageType,
            envelope.Id);

        try
        {
            await next();

            _logger.LogInformation(
                "Handled message {MessageType} successfully",
                messageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling message {MessageType}",
                messageType);

            throw;
        }
    }
}
