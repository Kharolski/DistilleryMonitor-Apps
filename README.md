# DistilleryMonitor Apps

Cross-platform mobile and desktop applications for real-time distillery temperature monitoring. Built with .NET MAUI, connecting to ESP32 sensor system.

## 🚀 Features (Planned)

- **Real-time Temperature Monitoring** - Live data from 3 sensors (Kolv, Destillat, Kylare)
- **Configurable Alerts** - Set custom temperature thresholds via mobile app
- **LED Status Indicators** - Visual feedback with color-coded alerts
- **Cross-Platform** - iOS, Android, Windows, macOS support
- **Settings Management** - Configure ESP32 IP address from app

## 🏗️ Architecture

```
DistilleryMonitor-Apps/
├── src/
│   ├── DistilleryMonitor.Core/     # Shared business logic
│   ├── DistilleryMonitor.Mobile/   # MAUI cross-platform app
│   └── DistilleryMonitor.Tests/    # Unit tests
└── README.md
```

## 🛠️ Tech Stack

- **.NET 8** - Latest LTS framework
- **.NET MAUI** - Cross-platform UI framework
- **C#** - Primary programming language
- **xUnit** - Testing framework
- **ESP32 Integration** - REST API communication

## 📱 Current Status

**✅ Completed:**
- Project structure setup
- Core models for ESP32 API integration
- Settings service with persistent storage
- HTTP client for ESP32 communication
- Dependency injection configuration

**🚧 In Progress:**
- Mobile UI development
- Real-time data display
- Configuration management interface

**📋 Planned:**
- Splash screen and animations
- Push notifications
- Data logging and history
- Export functionality

## 🔧 Development Setup

### Prerequisites
- Visual Studio 2022 with MAUI workload
- .NET 8 SDK
- ESP32 device with DistilleryMonitor firmware

### Getting Started
1. Clone the repository
2. Open `DistilleryMonitor.sln` in Visual Studio
3. Build solution
4. Configure ESP32 IP address in app settings
5. Run on desired platform

## 📡 ESP32 API Integration

The app communicates with ESP32 via REST API:

- `GET /api/temperatures` - Fetch current sensor readings
- `GET /api/config` - Get temperature thresholds
- `POST /api/config` - Update sensor configuration

## 🤝 Contributing

This is a learning project showcasing modern .NET development practices and IoT integration.

## 📄 License

MIT License - see LICENSE file for details.
```

### **Steg 2: Commit & Push**

```bash
git add .
```

```bash
git commit -m "feat: implement core architecture and ESP32 API integration

- Add temperature and configuration models
- Implement ApiService for ESP32 communication  
- Add SettingsService with persistent storage
- Configure dependency injection in MAUI
- Support configurable ESP32 IP address
- Add comprehensive code documentation"
```

```bash
git push origin main
