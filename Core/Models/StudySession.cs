namespace LouisStudyBot.Core.Models;

public sealed class StudySession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ulong GuildId { get; set; }

    public ulong UserId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Tag { get; set; } = "Uncategorized";

    public string EndReason { get; set; } = string.Empty;

    public bool IsActive => EndedAtUtc is null;

    public TimeSpan Duration => (EndedAtUtc ?? DateTimeOffset.UtcNow) - StartedAtUtc;
}
