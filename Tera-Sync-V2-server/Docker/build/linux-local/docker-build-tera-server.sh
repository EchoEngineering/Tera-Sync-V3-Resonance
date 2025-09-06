#!/bin/sh
cd ../../../
docker build -t kirin-xiv/terasync-server:latest . -f Docker/build/Dockerfile-TeraSyncV2Server --no-cache --pull --force-rm
cd Docker/build/linux-local