#!/bin/sh
cd ../../../
docker build -t kirin-xiv/terasync-authservice:latest . -f Docker/build/Dockerfile-TeraSyncV2AuthService --no-cache --pull --force-rm
cd Docker/build/linux-local