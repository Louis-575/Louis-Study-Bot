namespace LouisStudyBot.Core.Services;

public interface IStudySessionStore
{
    Task<StudyStartResult> StartSessionAsync(ulong guildId, ulong userId, string tag);

    Task<StudySession?> GetActiveSessionAsync(ulong guildId, ulong userId);

    Task<StudySession?> EndSessionAsync(ulong guildId, ulong userId, string summary, string tag);

    Task<IReadOnlyList<string>> GetTagsAsync(ulong guildId, int limit);

    Task<IReadOnlyList<StudySession>> GetHistoryAsync(ulong guildId, ulong userId, int limit);

    Task<StudyUserStats> GetUserStatsAsync(ulong guildId, ulong userId, StudyPeriod period);

    Task<IReadOnlyList<StudyLeaderboardEntry>> GetLeaderboardAsync(ulong guildId, StudyPeriod period, LeaderboardMetric metric, int limit);

    Task<int> AutoEndExpiredSessionsAsync(DateTimeOffset nowUtc);
}
