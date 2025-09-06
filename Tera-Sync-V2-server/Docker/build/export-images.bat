@echo off
echo Exporting TeraSyncV2 Docker images...

echo Exporting server image...
docker save kirin-xiv/tera-synchronos-server:latest > tera-server.tar
if errorlevel 1 (
    echo Failed to export server image
    exit /b 1
)

echo Exporting auth service image...
docker save kirin-xiv/tera-synchronos-authservice:latest > tera-authservice.tar
if errorlevel 1 (
    echo Failed to export auth service image
    exit /b 1
)

echo Exporting services image...
docker save kirin-xiv/tera-synchronos-services:latest > tera-services.tar
if errorlevel 1 (
    echo Failed to export services image
    exit /b 1
)

echo Exporting static files server image...
docker save kirin-xiv/tera-synchronos-staticfilesserver:latest > tera-staticfilesserver.tar
if errorlevel 1 (
    echo Failed to export static files server image
    exit /b 1
)

echo Creating compressed archive...
tar -czf terasync-all-images.tar.gz tera-server.tar tera-authservice.tar tera-services.tar tera-staticfilesserver.tar
if errorlevel 1 (
    echo Failed to create compressed archive
    exit /b 1
)

echo Cleaning up individual tar files...
del tera-server.tar tera-authservice.tar tera-services.tar tera-staticfilesserver.tar

echo TeraSyncV2 Docker images exported successfully to terasync-all-images.tar.gz
echo File size:
dir terasync-all-images.tar.gz