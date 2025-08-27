#!/bin/bash

echo "Starting TeraSyncV2 Standalone Server..."

# Check if .env file exists
if [ ! -f .env ]; then
    echo "Error: .env file not found. Please create .env file with required environment variables."
    echo "Required variables:"
    echo "  DEV_TERA_CDNURL=https://terasync.app/cache/"
    echo "  DEV_TERA_XIVAPIKEY=your_xivapi_key"
    echo "  DEV_TERA_DISCORDTOKEN=your_discord_bot_token"
    echo "  DEV_TERA_DISCORDCHANNEL=your_discord_channel_id"
    exit 1
fi

# Load environment variables
source .env

# Start TeraSyncV2 standalone services
docker compose -f compose/tera-standalone.yml --env-file .env up

echo "TeraSyncV2 Standalone Server stopped."