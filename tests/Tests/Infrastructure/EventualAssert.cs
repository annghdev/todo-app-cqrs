namespace Tests.Infrastructure;

public static class EventualAssert
{
    public static async Task TrueAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(30);
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(200);
        var deadline = DateTime.UtcNow.Add(maxWait);

        while (DateTime.UtcNow <= deadline)
        {
            try
            {
                if (await condition())
                {
                    return;
                }
            }
            catch
            {
                // Keep polling until timeout when eventual consistency is expected.
            }

            await Task.Delay(interval);
        }

        throw new TimeoutException($"Condition was not met within {maxWait.TotalSeconds} seconds.");
    }
}
