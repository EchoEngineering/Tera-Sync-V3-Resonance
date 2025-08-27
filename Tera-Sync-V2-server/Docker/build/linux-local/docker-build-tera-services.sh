#!/bin/sh
cd ../../../
docker build -t kirin-xiv/tera-synchronos-services:latest . -f Docker/build/Dockerfile-TeraSyncV2Services --no-cache --pull --force-rm
cd Docker/build/linux-local