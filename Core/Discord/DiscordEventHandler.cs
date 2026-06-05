namespace LouisStudyBot.Core.Services;

public sealed class DiscordEventHandler(
    DiscordSocketClient client,
    InteractionService interactions,
    IStudySessionStore store,
    IServiceProvider services)
{
    private bool _modulesRegistered;

    public Task InitializeAsync()
    {
        client.Log += LogAsync;
        interactions.Log += LogAsync;
        client.Ready += ReadyAsync;
        client.InteractionCreated += HandleInteractionAsync;
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        if (!_modulesRegistered)
        {
            await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            _modulesRegistered = true;
        }

        string registrationMode = EnvConfig.Get("COMMAND_REGISTRATION", "Guild");
        if (registrationMode.Equals("Global", StringComparison.OrdinalIgnoreCase))
        {
            await interactions.RegisterCommandsGloballyAsync();
            Logs.Info("Registered slash commands globally");
        }
        else
        {
            foreach (SocketGuild guild in client.Guilds)
            {
                await interactions.RegisterCommandsToGuildAsync(guild.Id);
                Logs.Info($"Registered slash commands in {guild.Name}");
            }
        }

        await client.SetGameAsync("/study start", type: ActivityType.Listening);
        Logs.Info($"Louis Study Bot is ready in {client.Guilds.Count} server(s)");
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            if (interaction is SocketModal modal && modal.Data.CustomId == "study:end")
            {
                await HandleStudyEndModalAsync(modal);
                return;
            }

            SocketInteractionContext context = new(client, interaction);
            IResult result = await interactions.ExecuteCommandAsync(context, services);
            if (result.IsSuccess || interaction is SocketAutocompleteInteraction)
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(result.ErrorReason)
                ? "That command could not be completed."
                : result.ErrorReason;

            if (interaction.HasResponded)
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            Logs.Error($"Interaction error: {ex}");
            if (interaction is SocketAutocompleteInteraction)
            {
                return;
            }

            try
            {
                if (interaction.HasResponded)
                {
                    await interaction.FollowupAsync("Something went wrong while handling that command.", ephemeral: true);
                }
                else
                {
                    await interaction.RespondAsync("Something went wrong while handling that command.", ephemeral: true);
                }
            }
            catch
            {
                Logs.Debug("Could not send interaction error response.");
            }
        }
    }

    private async Task HandleStudyEndModalAsync(SocketModal modal)
    {
        SocketInteractionContext context = new(client, modal);
        if (context.Guild is null)
        {
            await modal.RespondAsync("Study sessions are tracked per server, so this can only be used in a server.", ephemeral: true);
            return;
        }

        IReadOnlyCollection<SocketMessageComponentData> components = modal.Data.Components;
        string summary = components.FirstOrDefault(component => component.CustomId == "summary")?.Value ?? string.Empty;
        string tag = components.FirstOrDefault(component => component.CustomId == "tag")?.Value ?? string.Empty;

        StudySession? session = await store.EndSessionAsync(context.Guild.Id, modal.User.Id, summary, tag);
        if (session is null)
        {
            await modal.RespondAsync("That session is no longer active. It may have already been ended automatically.", ephemeral: true);
            return;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Study session finished")
            .WithColor(Color.Blue)
            .AddField("Time studied", FormatDuration(session.Duration), inline: true)
            .AddField("Subject", session.Tag, inline: true)
            .AddField("Started", DiscordTimestamp(session.StartedAtUtc, "f"), inline: true)
            .AddField("Summary", Truncate(session.Summary, 1000));

        await modal.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private static Task LogAsync(LogMessage message)
    {
        string text = $"[Discord] {message.Source}: {message.Message} {message.Exception}".Trim();
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Logs.Error(text);
                break;
            case LogSeverity.Warning:
                Logs.Warning(text);
                break;
            case LogSeverity.Debug:
            case LogSeverity.Verbose:
                Logs.Debug(text);
                break;
            default:
                Logs.Info(text);
                break;
        }

        return Task.CompletedTask;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalMinutes < 1)
        {
            return "<1m";
        }

        int hours = (int)duration.TotalHours;
        int minutes = duration.Minutes;
        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }

    private static string DiscordTimestamp(DateTimeOffset timestamp, string format)
    {
        return $"<t:{timestamp.ToUnixTimeSeconds()}:{format}>";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "No summary provided.";
        }

        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
