namespace LouisStudyBot.Main;

public static class ServiceRegistration
{
    public static IServiceCollection AddStudyBotServices(this IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Debug,
            AlwaysDownloadUsers = false,
            UseInteractionSnowflakeDate = false,
            UseSystemClock = false
        }));

        services.AddSingleton(provider => new InteractionService(
            provider.GetRequiredService<DiscordSocketClient>(),
            new InteractionServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            }));

        services.AddSingleton<DiscordEventHandler>();
        services.AddSingleton<IStudySessionStore>(_ =>
        {
            string path = EnvConfig.Get("STUDY_DATA_PATH", "Data/study-sessions.json");
            return new StudySessionStore(path);
        });

        return services;
    }
}
