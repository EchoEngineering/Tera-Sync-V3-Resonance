@echo off
cd ..\..\..
docker build -t kirin-xiv/tera-synchronos-authservice:latest . -f Docker/build/Dockerfile-TeraSyncV2AuthService --no-cache --pull --force-rm
cd Docker\build\windows-local