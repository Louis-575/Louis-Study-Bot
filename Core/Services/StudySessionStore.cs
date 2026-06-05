namespace LouisStudyBot.Core.Services;

public sealed class StudySessionStore(string dataPath) : IStudySessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _dataPath = Path.GetFullPath(dataPath);

    public async Task<StudyStartResult> StartSessionAsync(ulong guildId, ulong userId)
    {
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);

            StudySession? active = data.Sessions.FirstOrDefault(session =>
                session.GuildId == guildId && session.UserId == userId && session.IsActive);

            if (active is not null)
            {
                await SaveAsync(data);
                return new StudyStartResult(false, active);
            }

            StudySession session = new()
            {
                Id = Guid.NewGuid(),
                GuildId = guildId,
                UserId = userId,
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            data.Sessions.Add(session);
            await SaveAsync(data);
            return new StudyStartResult(true, session);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<StudySession?> GetActiveSessionAsync(ulong guildId, ulong userId)
    {
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            bool changed = AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);
            if (changed)
            {
                await SaveAsync(data);
            }

            return data.Sessions.FirstOrDefault(session =>
                session.GuildId == guildId && session.UserId == userId && session.IsActive);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<StudySession?> EndSessionAsync(ulong guildId, ulong userId, string summary, string tag)
    {
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            bool changed = AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);

            StudySession? active = data.Sessions.FirstOrDefault(session =>
                session.GuildId == guildId && session.UserId == userId && session.IsActive);

            if (active is null)
            {
                if (changed)
                {
                    await SaveAsync(data);
                }

                return null;
            }

            active.EndedAtUtc = DateTimeOffset.UtcNow;
            active.Summary = CleanSummary(summary);
            active.Tag = CleanTag(tag);
            active.EndReason = "Manual";

            await SaveAsync(data);
            return active;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<StudySession>> GetHistoryAsync(ulong guildId, ulong userId, int limit)
    {
        limit = Math.Clamp(limit, 1, 20);
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            bool changed = AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);
            if (changed)
            {
                await SaveAsync(data);
            }

            return data.Sessions
                .Where(session => session.GuildId == guildId && session.UserId == userId && !session.IsActive)
                .OrderByDescending(session => session.EndedAtUtc)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<StudyUserStats> GetUserStatsAsync(ulong guildId, ulong userId, StudyPeriod period)
    {
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            bool changed = AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);
            if (changed)
            {
                await SaveAsync(data);
            }

            List<StudySession> sessions = FilterCompletedSessions(data.Sessions, guildId, period)
                .Where(session => session.UserId == userId)
                .ToList();

            return BuildUserStats(sessions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<StudyLeaderboardEntry>> GetLeaderboardAsync(ulong guildId, StudyPeriod period, LeaderboardMetric metric, int limit)
    {
        limit = Math.Clamp(limit, 1, 25);
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            bool changed = AutoEndExpiredSessions(data, DateTimeOffset.UtcNow);
            if (changed)
            {
                await SaveAsync(data);
            }

            IEnumerable<StudyLeaderboardEntry> entries = FilterCompletedSessions(data.Sessions, guildId, period)
                .GroupBy(session => session.UserId)
                .Select(group => new StudyLeaderboardEntry(
                    group.Key,
                    group.Count(),
                    TimeSpan.FromTicks(group.Sum(session => session.Duration.Ticks))));

            entries = metric == LeaderboardMetric.Sessions
                ? entries.OrderByDescending(entry => entry.Sessions).ThenByDescending(entry => entry.TotalTime)
                : entries.OrderByDescending(entry => entry.TotalTime).ThenByDescending(entry => entry.Sessions);

            return entries.Take(limit).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> AutoEndExpiredSessionsAsync(DateTimeOffset nowUtc)
    {
        await _lock.WaitAsync();
        try
        {
            StudyDataFile data = await LoadAsync();
            int endedCount = 0;

            foreach (StudySession session in data.Sessions.Where(session => session.IsActive))
            {
                if (nowUtc - session.StartedAtUtc <= TimeSpan.FromHours(24))
                {
                    continue;
                }

                EndExpiredSession(session);
                endedCount++;
            }

            if (endedCount > 0)
            {
                await SaveAsync(data);
            }

            return endedCount;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static StudyUserStats BuildUserStats(IReadOnlyList<StudySession> sessions)
    {
        IReadOnlyList<StudySubjectStats> subjects = sessions
            .GroupBy(session => CleanTag(session.Tag), StringComparer.OrdinalIgnoreCase)
            .Select(group => new StudySubjectStats(
                group.Key,
                group.Count(),
                TimeSpan.FromTicks(group.Sum(session => session.Duration.Ticks))))
            .OrderByDescending(subject => subject.TotalTime)
            .ThenByDescending(subject => subject.Sessions)
            .ToList();

        return new StudyUserStats(
            sessions.Count,
            TimeSpan.FromTicks(sessions.Sum(session => session.Duration.Ticks)),
            subjects);
    }

    private static IEnumerable<StudySession> FilterCompletedSessions(IEnumerable<StudySession> sessions, ulong guildId, StudyPeriod period)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset start = period switch
        {
            StudyPeriod.Today => new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero),
            StudyPeriod.Week => now.AddDays(-7),
            _ => DateTimeOffset.MinValue
        };

        return sessions.Where(session =>
            session.GuildId == guildId
            && session.EndedAtUtc is not null
            && session.EndedAtUtc >= start);
    }

    private static bool AutoEndExpiredSessions(StudyDataFile data, DateTimeOffset nowUtc)
    {
        bool changed = false;
        foreach (StudySession session in data.Sessions.Where(session => session.IsActive))
        {
            if (nowUtc - session.StartedAtUtc <= TimeSpan.FromHours(24))
            {
                continue;
            }

            EndExpiredSession(session);
            changed = true;
        }

        return changed;
    }

    private static void EndExpiredSession(StudySession session)
    {
        session.EndedAtUtc = session.StartedAtUtc.AddHours(24);
        session.Summary = "Automatically ended after 24 hours.";
        session.Tag = "Auto-ended";
        session.EndReason = "Automatic";
    }

    private async Task<StudyDataFile> LoadAsync()
    {
        if (!File.Exists(_dataPath))
        {
            return new StudyDataFile();
        }

        string json = await File.ReadAllTextAsync(_dataPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StudyDataFile();
        }

        return JsonSerializer.Deserialize<StudyDataFile>(json, JsonOptions) ?? new StudyDataFile();
    }

    private async Task SaveAsync(StudyDataFile data)
    {
        string? directory = Path.GetDirectoryName(_dataPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(_dataPath, json);
    }

    private static string CleanSummary(string summary)
    {
        summary = summary.Trim();
        return string.IsNullOrWhiteSpace(summary) ? "No summary provided." : summary;
    }

    private static string CleanTag(string tag)
    {
        tag = tag.Trim();
        return string.IsNullOrWhiteSpace(tag) ? "Uncategorized" : tag;
    }

    private sealed class StudyDataFile
    {
        public List<StudySession> Sessions { get; set; } = [];
    }
}
