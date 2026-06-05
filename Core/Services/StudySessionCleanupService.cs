namespace LouisStudyBot.Core.Services;

public sealed class StudySessionCleanupService(IStudySessionStore store) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int endedCount = await store.AutoEndExpiredSessionsAsync(DateTimeOffset.UtcNow);
                if (endedCount > 0)
                {
                    Logs.Info($"Automatically ended {endedCount} expired study session(s).");
                }
            }
            catch (Exception ex)
            {
                Logs.Error($"Study cleanup failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
