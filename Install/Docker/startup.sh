#!/bin/bash
set -e

APP_DIR="/app"
SOURCE_DIR="/source"

if [ ! -f "$APP_DIR/.env" ]; then
    echo "ERROR: No .env file found at $APP_DIR/.env"
    echo "Copy RenameMe.env.txt to .env and add your Discord bot token."
    exit 1
fi

if [ "${AUTO_UPDATE:-false}" = "true" ] && [ -d "$SOURCE_DIR/.git" ]; then
    echo "Checking for source updates..."
    cd "$SOURCE_DIR"
    git pull || echo "Warning: git pull failed, continuing with existing source"
    cd "$APP_DIR"
fi

BUILD_MARKER="$APP_DIR/.last-build"

need_rebuild() {
    if [ ! -f "$APP_DIR/LouisStudyBot.dll" ]; then
        echo "No binaries found, rebuild needed."
        return 0
    fi

    if [ ! -d "$SOURCE_DIR" ] || [ ! -f "$SOURCE_DIR/LouisStudyBot.csproj" ]; then
        return 1
    fi

    if [ ! -f "$BUILD_MARKER" ]; then
        echo "No build marker found, rebuild needed."
        return 0
    fi

    NEWEST_SOURCE=$(find "$SOURCE_DIR" -type d \( -name bin -o -name obj \) -prune -o -type f \( -name "*.cs" -o -name "*.csproj" \) -newer "$BUILD_MARKER" -print -quit 2>/dev/null)

    if [ -n "$NEWEST_SOURCE" ]; then
        echo "Source is newer than last build, rebuild needed."
        return 0
    fi

    return 1
}

if need_rebuild; then
    echo "Rebuilding Louis Study Bot from source..."
    cd "$SOURCE_DIR"
    dotnet restore LouisStudyBot.csproj
    dotnet publish LouisStudyBot.csproj -c Release -o "$APP_DIR"
    touch "$BUILD_MARKER"
    echo "Rebuild complete."
fi

echo "Starting Louis Study Bot..."
cd "$APP_DIR"
exec dotnet LouisStudyBot.dll
