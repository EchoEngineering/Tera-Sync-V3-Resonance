# TeraSyncV2 Server Deployment Guide

This guide walks through deploying TeraSyncV2 server on Ubuntu/Linux with Docker, nginx, and SSL certificates for terasync.app domain.

## Prerequisites

- Ubuntu 20.04+ server
- Root or sudo access
- Domain `terasync.app` pointing to server IP
- Discord bot token (for user registration)
- XIVAPI key (optional)

## 1. Initial Server Setup

### Update system packages
```bash
sudo apt update && sudo apt upgrade -y
```

### Install Docker
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add user to docker group
sudo usermod -aG docker $USER

# Install Docker Compose
sudo apt install docker-compose-plugin -y

# Start Docker service
sudo systemctl enable docker
sudo systemctl start docker
```

### Install nginx and certbot
```bash
sudo apt install nginx certbot python3-certbot-nginx -y
```

## 2. Domain and SSL Setup

### Configure nginx for terasync.app
```bash
sudo nano /etc/nginx/sites-available/terasync.app
```

Add the following configuration:
```nginx
server {
    server_name terasync.app;
    
    # Main server (SignalR)
    location / {
        proxy_pass http://127.0.0.1:6000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
    
    # File server (CDN)
    location /cache/ {
        proxy_pass http://127.0.0.1:6200/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:
```bash
sudo ln -s /etc/nginx/sites-available/terasync.app /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Get SSL certificate
```bash
sudo certbot --nginx -d terasync.app
```

## 3. Build TeraSyncV2 Images Locally (Windows)

### Build Docker images on your Windows machine
First, ensure Docker Desktop is installed and running on Windows.

Navigate to your TeraSyncV2 server directory:
```cmd
cd F:\source\repos\Tera-Sync-V2\Tera-Sync-V2-server
```

Build all Docker images:
```cmd
cd Docker\build\linux-local
docker-build-tera.bat
```

Or build individually:
```cmd
docker-build-tera-server.bat
docker-build-tera-authservice.bat  
docker-build-tera-services.bat
docker-build-tera-staticfilesserver.bat
```

### Export Docker images as tar.gz
```cmd
# Export all images to tar files
docker save kirin-xiv/tera-synchronos-server:latest > tera-server.tar
docker save kirin-xiv/tera-synchronos-authservice:latest > tera-authservice.tar
docker save kirin-xiv/tera-synchronos-services:latest > tera-services.tar
docker save kirin-xiv/tera-synchronos-staticfilesserver:latest > tera-staticfilesserver.tar

# Compress them
tar -czf terasync-docker-images.tar.gz *.tar

# Or create one large compressed archive
docker save kirin-xiv/tera-synchronos-server:latest kirin-xiv/tera-synchronos-authservice:latest kirin-xiv/tera-synchronos-services:latest kirin-xiv/tera-synchronos-staticfilesserver:latest | gzip > terasync-all-images.tar.gz
```

### Transfer to server
```cmd
# Copy to server via SCP (replace YOUR_SERVER_IP with your server's IP)
scp terasync-all-images.tar.gz root@YOUR_SERVER_IP:/tmp/

# Also copy the configuration files
scp -r F:\source\repos\Tera-Sync-V2\Tera-Sync-V2-server\Docker\run root@YOUR_SERVER_IP:/opt/terasync/
```

## 4. Server Setup

### Create directory structure and load images
```bash
# Create working directory
sudo mkdir -p /opt/terasync
cd /opt/terasync

# Load Docker images
gunzip -c /tmp/terasync-all-images.tar.gz | docker load

# Verify images are loaded
docker images | grep tera-synchronos
```

### Create environment configuration
```bash
cd /opt/terasync/run
nano .env
```

Add the following environment variables:
```bash
# CDN URL - must match your domain
DEV_TERA_CDNURL=https://terasync.app/cache/

# XIVAPI Key (optional, get from https://xivapi.com/)
DEV_TERA_XIVAPIKEY=your_xivapi_key_here

# Discord Bot Configuration (required for user registration)
DEV_TERA_DISCORDTOKEN=your_discord_bot_token_here
DEV_TERA_DISCORDCHANNEL=your_discord_channel_id_here
```

### Start TeraSyncV2 services
```bash
cd /opt/Tera-Sync-V2/Tera-Sync-V2-server/Docker/run
chmod +x *.sh

# Start in background (daemon mode)
docker compose -f compose/tera-standalone.yml --env-file .env up -d
```

## 4. Verify Deployment

### Check service status
```bash
docker compose -f compose/tera-standalone.yml ps
```

### Check logs
```bash
# View all logs
docker compose -f compose/tera-standalone.yml logs

# View specific service logs
docker compose -f compose/tera-standalone.yml logs tera-server
docker compose -f compose/tera-standalone.yml logs tera-files
```

### Test endpoints
```bash
# Test main server health
curl https://terasync.app/health

# Test file server
curl https://terasync.app/cache/
```

## 5. Discord Bot Setup

### Create Discord Application
1. Go to https://discord.com/developers/applications
2. Create new application named "TeraSyncV2"
3. Go to "Bot" section and create bot
4. Copy the bot token to your .env file
5. Enable "Message Content Intent" if needed

### Add Bot to Server
1. Go to OAuth2 > URL Generator
2. Select "bot" and "applications.commands" scopes
3. Select required permissions (Embed Links, Send Messages, etc.)
4. Use generated URL to add bot to your Discord server
5. Copy the channel ID where you want notifications

## 6. Maintenance Commands

### Update TeraSyncV2
```bash
cd /opt/Tera-Sync-V2
git pull
cd Tera-Sync-V2-server/Docker/build/linux-local
./docker-build-tera.sh
cd ../../run
docker compose -f compose/tera-standalone.yml --env-file .env down
docker compose -f compose/tera-standalone.yml --env-file .env up -d
```

### View logs
```bash
cd /opt/Tera-Sync-V2/Tera-Sync-V2-server/Docker/run
docker compose -f compose/tera-standalone.yml logs -f
```

### Stop services
```bash
docker compose -f compose/tera-standalone.yml down
```

### Backup database
```bash
docker exec -t $(docker compose -f compose/tera-standalone.yml ps -q postgres) pg_dump -U terasync terasync > backup_$(date +%Y%m%d_%H%M%S).sql
```

## 7. Configuration Files

The server uses several configuration files that can be customized:

- `config/standalone/server-standalone.json` - Main server settings
- `config/standalone/authservice-standalone.json` - Authentication service settings  
- `config/standalone/services-standalone.json` - Discord bot settings
- `config/standalone/files-standalone.json` - File server settings

Modify these files as needed and restart services:
```bash
docker compose -f compose/tera-standalone.yml restart
```

## Firewall Configuration

Ensure these ports are open:
- 80 (HTTP - for Let's Encrypt)  
- 443 (HTTPS - for public access)
- 22 (SSH - for administration)

Internal Docker services use ports 5432 (PostgreSQL), 6000 (main server), and 6200 (file server), but these should not be exposed publicly.

## Troubleshooting

### Common Issues

1. **Services won't start**: Check Docker logs and ensure .env file is properly configured
2. **Can't connect**: Verify nginx configuration and SSL certificates
3. **Database connection errors**: Ensure PostgreSQL container is healthy
4. **File upload failures**: Check file server logs and disk space

### Getting Help

- Check logs: `docker compose logs <service-name>`
- View container status: `docker compose ps`
- Restart individual services: `docker compose restart <service-name>`
- Full restart: `docker compose down && docker compose up -d`