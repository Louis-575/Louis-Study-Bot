namespace LouisStudyBot.Main;

public static class StudyBotMain
{
    public static async Task Main(string[] args)
    {
        try
        {
            EnvConfig.Initialize();
            Logs.Initialize(EnvConfig.Get("LOG_PATH", "logs/study-bot-[year]-[month]-[day].log"));
            Logs.Info("Starting Louis Study Bot");

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddStudyBotServices();
                    services.AddHostedService<BotHostedService>();
                    services.AddHostedService<StudySessionCleanupService>();
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Logs.Error($"Fatal error: {ex}");
        }
        finally
        {
            Logs.Shutdown();
        }
    }
}
