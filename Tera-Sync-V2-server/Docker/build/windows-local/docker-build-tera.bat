@echo off
echo Building all TeraSyncV2 Docker images...

call docker-build-tera-server.bat
if errorlevel 1 (
    echo Failed to build server image
    exit /b 1
)

call docker-build-tera-authservice.bat
if errorlevel 1 (
    echo Failed to build auth service image
    exit /b 1
)

call docker-build-tera-services.bat
if errorlevel 1 (
    echo Failed to build services image
    exit /b 1
)

call docker-build-tera-staticfilesserver.bat
if errorlevel 1 (
    echo Failed to build static files server image
    exit /b 1
)

echo All TeraSyncV2 Docker images built successfully!