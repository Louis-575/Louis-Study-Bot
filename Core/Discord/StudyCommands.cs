namespace LouisStudyBot.Core.Discord;

[Group("study", "Track study sessions and study stats")]
public sealed class StudyCommands(IStudySessionStore store) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("start", "Begin a study session")]
    public async Task StartAsync()
    {
        if (!TryGetGuildId(out ulong guildId))
        {
            await RespondAsync("Study sessions are tracked per server, so this command needs to be used in a server.", ephemeral: true);
            return;
        }

        StudyStartResult result = await store.StartSessionAsync(guildId, Context.User.Id);
        if (!result.Started)
        {
            await RespondAsync(
                $"You already have a study session running. It started {DiscordTimestamp(result.Session.StartedAtUtc, "R")}.",
                ephemeral: true);
            return;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Study session started")
            .WithDescription($"Started {DiscordTimestamp(result.Session.StartedAtUtc, "F")}.\nUse `/study end` when you are done.")
            .WithColor(Color.Green);

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("end", "End your current study session")]
    public async Task EndAsync()
    {
        if (!TryGetGuildId(out ulong guildId))
        {
            await RespondAsync("Study sessions are tracked per server, so this command needs to be used in a server.", ephemeral: true);
            return;
        }

        StudySession? active = await store.GetActiveSessionAsync(guildId, Context.User.Id);
        if (active is null)
        {
            await RespondAsync("You do not have an active study session to end.", ephemeral: true);
            return;
        }

        Modal modal = new ModalBuilder()
            .WithTitle("End study session")
            .WithCustomId("study:end")
            .AddTextInput(
                label: "What did you work on?",
                customId: "summary",
                style: TextInputStyle.Paragraph,
                placeholder: "Example: Finished vectors worksheet and reviewed mistakes.",
                maxLength: 1000,
                required: true)
            .AddTextInput(
                label: "Subject tag",
                customId: "tag",
                style: TextInputStyle.Short,
                placeholder: "Example: Maths",
                maxLength: 50,
                required: true)
            .Build();

        await RespondWithModalAsync(modal);
    }

    [SlashCommand("history", "View your previous study sessions")]
    public async Task HistoryAsync(
        [Summary("limit", "How many sessions to show, from 1 to 20")]
        int limit = 10)
    {
        if (!TryGetGuildId(out ulong guildId))
        {
            await RespondAsync("Study sessions are tracked per server, so this command needs to be used in a server.", ephemeral: true);
            return;
        }

        IReadOnlyList<StudySession> sessions = await store.GetHistoryAsync(guildId, Context.User.Id, limit);
        if (sessions.Count == 0)
        {
            await RespondAsync("You do not have any completed study sessions yet.", ephemeral: true);
            return;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Your recent study sessions")
            .WithColor(Color.Purple);

        foreach (StudySession session in sessions)
        {
            string ended = session.EndedAtUtc is null ? "Unknown" : DiscordTimestamp(session.EndedAtUtc.Value, "f");
            string value = new StringBuilder()
                .AppendLine($"Subject: **{session.Tag}**")
                .AppendLine($"Time: **{FormatDuration(session.Duration)}**")
                .AppendLine($"Ended: {ended}")
                .Append(Truncate(session.Summary, 400))
                .ToString();

            embed.AddField(DiscordTimestamp(session.StartedAtUtc, "d"), value);
        }

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("stats", "View your study stats")]
    public async Task StatsAsync(
        [Summary("period", "The time period to show")]
        StudyPeriod period = StudyPeriod.Lifetime)
    {
        if (!TryGetGuildId(out ulong guildId))
        {
            await RespondAsync("Study sessions are tracked per server, so this command needs to be used in a server.", ephemeral: true);
            return;
        }

        StudyUserStats stats = await store.GetUserStatsAsync(guildId, Context.User.Id, period);
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle($"Your study stats - {PeriodLabel(period)}")
            .WithColor(Color.Teal)
            .AddField("Sessions", stats.Sessions.ToString(), inline: true)
            .AddField("Total time", FormatDuration(stats.TotalTime), inline: true);

        if (stats.Subjects.Count == 0)
        {
            embed.WithDescription("No completed sessions for this period yet.");
        }
        else
        {
            embed.AddField("By subject", FormatSubjectStats(stats.Subjects));
        }

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("leaderboard", "View the server study leaderboard")]
    public async Task LeaderboardAsync(
        [Summary("period", "The time period to show")]
        StudyPeriod period = StudyPeriod.Lifetime,
        [Summary("metric", "Rank by total time or number of sessions")]
        LeaderboardMetric metric = LeaderboardMetric.Time)
    {
        if (!TryGetGuildId(out ulong guildId))
        {
            await RespondAsync("Study leaderboards are tracked per server, so this command needs to be used in a server.", ephemeral: true);
            return;
        }

        IReadOnlyList<StudyLeaderboardEntry> entries = await store.GetLeaderboardAsync(guildId, period, metric, 10);
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle($"Study leaderboard - {PeriodLabel(period)}")
            .WithDescription(metric == LeaderboardMetric.Time ? "Ranked by total time studied." : "Ranked by completed sessions.")
            .WithColor(Color.Gold);

        if (entries.Count == 0)
        {
            embed.AddField("No sessions yet", "No completed study sessions for this period.");
        }
        else
        {
            StringBuilder board = new();
            for (int index = 0; index < entries.Count; index++)
            {
                StudyLeaderboardEntry entry = entries[index];
                board
                    .Append("**")
                    .Append(index + 1)
                    .Append(".** <@")
                    .Append(entry.UserId)
                    .Append("> - ")
                    .Append(FormatDuration(entry.TotalTime))
                    .Append(" across ")
                    .Append(entry.Sessions)
                    .AppendLine(entry.Sessions == 1 ? " session" : " sessions");
            }

            embed.AddField("Top students", board.ToString());
        }

        await RespondAsync(embed: embed.Build());
    }

    private bool TryGetGuildId(out ulong guildId)
    {
        if (Context.Guild is null)
        {
            guildId = 0;
            return false;
        }

        guildId = Context.Guild.Id;
        return true;
    }

    private static string FormatSubjectStats(IReadOnlyList<StudySubjectStats> subjects)
    {
        StringBuilder builder = new();
        foreach (StudySubjectStats subject in subjects.Take(15))
        {
            builder
                .Append("**")
                .Append(subject.Tag)
                .Append("** - ")
                .Append(FormatDuration(subject.TotalTime))
                .Append(" across ")
                .Append(subject.Sessions)
                .AppendLine(subject.Sessions == 1 ? " session" : " sessions");
        }

        return builder.ToString();
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

    private static string PeriodLabel(StudyPeriod period) => period switch
    {
        StudyPeriod.Today => "today",
        StudyPeriod.Week => "last 7 days",
        StudyPeriod.Lifetime => "lifetime",
        _ => "lifetime"
    };

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
