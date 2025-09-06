#!/bin/bash

echo "Stopping TeraSyncV2 Standalone Server..."

# Stop TeraSyncV2 standalone services
docker compose -f compose/tera-standalone.yml down

echo "TeraSyncV2 Standalone Server stopped."