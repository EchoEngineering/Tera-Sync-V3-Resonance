# Tera Sync V2

**Advanced FFXIV character appearance and mod synchronization plugin for Dalamud**

Share your character appearance, mods, and glamour configurations with friends in real-time. Built on the latest Mare Synchronos codebase with complete rebranding and modernized dependencies.

## ✨ Features

- **Real-time synchronization** of character appearance and mods
- **Penumbra integration** - Share mod configurations seamlessly  
- **Glamourer compatibility** - Sync glamour plates and customization
- **Group management** - Create private sync groups with friends
- **File caching** - Efficient mod file distribution
- **Discord integration** - User registration and management via Discord bot
- **Cross-platform** - Windows client with Linux server deployment

## 🏗️ Architecture

### Client (Dalamud Plugin)
- **TeraSyncV2** - Main client plugin
- **TeraSyncV2.API** - Shared API definitions
- Built with .NET 9.0 and latest Dalamud SDK

### Server Components
- **TeraSyncMainServer** - Core SignalR hub for real-time sync
- **TeraSyncAuthService** - JWT-based authentication service
- **TeraSyncServices** - Discord bot integration and user management
- **TeraSyncStaticFilesServer** - CDN for mod file distribution
- **TeraSyncShared** - Shared models and database context

## 🚀 Quick Start

### Client Installation
1. Install via Dalamud plugin installer (coming soon)
2. Or build from source (see [Client Build Guide](#client-build))

### Server Deployment
See [Docker Deployment Guide](Tera-Sync-V2-server/Docker/DEPLOYMENT_GUIDE.md) for complete server setup instructions.

## 🛠️ Development

### Client Build

#### Prerequisites
- Visual Studio 2022 or VS Code
- .NET 9.0 SDK
- FFXIV with Dalamud installed

#### Build Steps
```bash
cd Tera-Sync-V2-client
dotnet restore
dotnet build -c Release
```

#### Testing
```bash
dotnet test
```

### Server Build

#### Prerequisites
- Docker Desktop
- .NET 8.0 SDK (for local development)

#### Build Docker Images
```bash
cd Tera-Sync-V2-server/Docker/build/windows-local
docker-build-tera.bat
```

#### Local Development
```bash
cd Tera-Sync-V2-server/TeraSyncServer
dotnet run --project TeraSyncMainServer
```

## 📋 Configuration

### Environment Variables
Required for server deployment:

```bash
# CDN Configuration
DEV_TERA_CDNURL=https://yourdomain.com/cache/

# XIVAPI Integration (optional)
DEV_TERA_XIVAPIKEY=your_xivapi_key

# Discord Bot (required for user registration)
DEV_TERA_DISCORDTOKEN=your_discord_bot_token
DEV_TERA_DISCORDCHANNEL=your_discord_channel_id
```

### Client Configuration
- Settings accessible via Dalamud plugin UI
- Server connection configured in-game
- Privacy and sync preferences per-character

## 🐳 Docker Deployment

The server runs as containerized microservices:

- **PostgreSQL** - User data and metadata
- **Redis** - Session caching
- **TeraSyncV2 Server** - Main application
- **TeraSyncV2 Auth** - Authentication service
- **TeraSyncV2 Services** - Discord bot
- **TeraSyncV2 Files** - Static file server

See [Docker Deployment Guide](Tera-Sync-V2-server/Docker/DEPLOYMENT_GUIDE.md) for detailed setup.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style
- Follow existing C# conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new functionality

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Mare Synchronos** - Original codebase foundation
- **Dalamud Team** - Plugin framework and APIs
- **Penumbra Team** - Mod framework integration
- **Glamourer Team** - Character customization APIs
- **FFXIV Community** - Testing and feedback
- **[opensynchronos](https://github.com/opensynchronos)** - Providing up to date Mare source code
https://github.com/opensynchronos
## 🔗 Links

- **Discord**: [Join our server](https://discord.gg/your-invite)
- **Documentation**: [Wiki](https://github.com/kirin-xiv/Tera-Sync-V2/wiki)
- **Issues**: [Bug reports](https://github.com/kirin-xiv/Tera-Sync-V2/issues)
- **Releases**: [Download latest](https://github.com/kirin-xiv/Tera-Sync-V2/releases)

## ⚠️ Disclaimer

This plugin is not affiliated with Square Enix. Use at your own risk. Always backup your character data before using synchronization features.
