@echo off
cd ..\..\..
docker build -t kirin-xiv/terasync-services:latest . -f Docker/build/Dockerfile-TeraSyncV2Services --no-cache --pull --force-rm
cd Docker\build\windows-local