namespace LouisStudyBot.Core.Models;

public sealed record StudySubjectStats(string Tag, int Sessions, TimeSpan TotalTime);

public sealed record StudyUserStats(int Sessions, TimeSpan TotalTime, IReadOnlyList<StudySubjectStats> Subjects);

public sealed record StudyLeaderboardEntry(ulong UserId, int Sessions, TimeSpan TotalTime);

public sealed record StudyStartResult(bool Started, StudySession Session);
