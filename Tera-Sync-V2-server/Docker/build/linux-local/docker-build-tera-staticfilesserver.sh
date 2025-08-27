#!/bin/sh
cd ../../../
docker build -t kirin-xiv/terasync-staticfilesserver:latest . -f Docker/build/Dockerfile-TeraSyncV2StaticFilesServer --no-cache --pull --force-rm
cd Docker/build/linux-local