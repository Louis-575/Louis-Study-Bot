# Louis Study Bot

A simple Discord study tracker and timer bot.

## Features

- `/study start` begins a timed study session.
- `/study end` opens a form asking what you studied and which subject tag to use.
- Sessions are tracked separately per user and per server.
- `/study history` shows your recent completed sessions.
- `/study stats` shows your study totals and groups them by subject.
- `/study leaderboard` shows daily, weekly, and lifetime server leaderboards by time or session count.

## Setup

1. Copy `RenameMe.env.txt` to `.env`.
2. Add your Discord bot token to `DISCORD_TOKEN`.
3. Run the bot:

```powershell
dotnet run --project .\LouisStudyBot.csproj
```

The bot stores session data in `Data/study-sessions.json` by default.

## Docker

The bot can also run in Docker:

```bash
cd Install/Docker
docker compose up -d --build
```

See `Docs/Docker-Guide.md` for the Docker details.
