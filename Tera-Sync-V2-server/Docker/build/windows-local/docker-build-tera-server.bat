@echo off
cd ..\..\..
docker build -t kirin-xiv/tera-synchronos-server:latest . -f Docker/build/Dockerfile-TeraSyncV2Server --no-cache --pull --force-rm
cd Docker\build\windows-local