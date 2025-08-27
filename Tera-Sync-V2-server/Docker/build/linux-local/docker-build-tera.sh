#!/bin/sh
./docker-build-tera-server.sh
./docker-build-tera-authservice.sh
./docker-build-tera-services.sh
./docker-build-tera-staticfilesserver.sh