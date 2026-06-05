namespace LouisStudyBot.Main;

public sealed class BotHostedService(
    DiscordSocketClient client,
    DiscordEventHandler eventHandler) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string token = EnvConfig.Get("DISCORD_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("DISCORD_TOKEN is missing. Copy RenameMe.env.txt to .env and add your bot token.");
        }

        await eventHandler.InitializeAsync();
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        Logs.Info("Discord connection started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logs.Info("Stopping Discord connection");
        await client.StopAsync();
        await client.LogoutAsync();
    }
}
