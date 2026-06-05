# Docker Guide

## Setup

1. Copy `RenameMe.env.txt` to `.env`.
2. Put your Discord bot token in `DISCORD_TOKEN`.
3. Start the bot:

```bash
cd Install/Docker
docker compose up -d --build
```

## Files kept on the host

- `Data/study-sessions.json` stores study sessions.
- `logs/` stores bot logs.
- `.env` stores secrets and runtime settings.

## Useful commands

```bash
docker compose logs -f
docker compose restart louis-study-bot
docker compose down
docker compose up -d --build
```

The container mounts the project source at `/source`. If you edit the bot files, the startup script rebuilds the app the next time the container starts.
