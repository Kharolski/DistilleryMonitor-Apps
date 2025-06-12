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
## 📁 Detailed Project Structure

```
DistilleryMonitor-Apps/
├── src/
│   ├── DistilleryMonitor.Core/           # Shared business logic & models
│   │   ├── Models/                       # Data models for ESP32 integration
│   │   │   ├── TemperatureReading.cs     ✅ ESP32 sensor data model
│   │   │   ├── ConfigurationResponse.cs  ✅ ESP32 config model
│   │   │   ├── HistoryEntry.cs           📋 Temperature history storage
│   │   │   └── AppSettings.cs            📋 Application configuration
│   │   ├── Services/                     # Business services & interfaces
│   │   │   ├── ISettingsService.cs       ✅ Settings abstraction
│   │   │   ├── ApiService.cs             ✅ ESP32 HTTP communication
│   │   │   ├── IDataService.cs           📋 Data persistence interface
│   │   │   ├── INotificationService.cs   📋 Notification abstraction
│   │   │   └── IHistoryService.cs        📋 Temperature history service
│   │   ├── Data/                         # Data access layer
│   │   │   ├── IRepository.cs            📋 Repository pattern interface
│   │   │   ├── TemperatureRepository.cs  📋 Temperature data access
│   │   │   └── SqliteContext.cs          📋 Local database context
│   │   └── Utilities/                    # Helper classes & extensions
│   │       ├── ApiResponseParser.cs      📋 JSON parsing utilities
│   │       └── TemperatureCalculator.cs  📋 Temperature calculations
│   │
│   ├── DistilleryMonitor.Mobile/         # MAUI cross-platform app
│   │   ├── MauiProgram.cs                ✅ App startup & DI configuration
│   │   ├── App.xaml                      ✅ Global app configuration
│   │   ├── AppShell.xaml                 📋 Navigation structure
│   │   ├── Platforms/                    # Platform-specific implementations
│   │   ├── Resources/                    # App resources
│   │   │   ├── Images/                   # Icons, logos, splash screens
│   │   │   ├── Fonts/                    # Custom typography
│   │   │   └── Styles/                   # Global UI styles
│   │   ├── Views/                        # XAML pages (UI screens)
│   │   │   ├── MainPage.xaml             📋 Temperature monitoring dashboard
│   │   │   ├── SettingsPage.xaml         📋 ESP32 IP & app configuration
│   │   │   ├── ConfigurationPage.xaml    📋 Temperature threshold settings
│   │   │   ├── HistoryPage.xaml          📋 Temperature history & graphs
│   │   │   └── AboutPage.xaml            📋 App information & credits
│   │   ├── ViewModels/                   # MVVM data binding logic
│   │   │   ├── MainPageViewModel.cs      📋 Dashboard logic & real-time updates
│   │   │   ├── SettingsViewModel.cs      📋 Settings management logic
│   │   │   ├── ConfigurationViewModel.cs 📋 ESP32 configuration logic
│   │   │   └── BaseViewModel.cs          📋 Common ViewModel functionality
│   │   ├── Controls/                     # Reusable UI components
│   │   │   ├── TemperatureCard.xaml      📋 Temperature display card
│   │   │   ├── StatusIndicator.xaml      📋 LED status visualization
│   │   │   └── ConnectionStatus.xaml     📋 ESP32 connection indicator
│   │   ├── Services/                     # Platform-specific services
│   │   │   ├── SettingsService.cs        ✅ SecureStorage implementation
│   │   │   ├── NotificationService.cs    📋 Push notification handling
│   │   │   └── TimerService.cs           📋 Background data updates
│   │   ├── Converters/                   # XAML value converters
│   │   │   ├── TemperatureColorConverter.cs 📋 Temp to color binding
│   │   │   └── BoolToVisibilityConverter.cs 📋 Boolean to UI visibility
│   │   └── Helpers/                      # Utility classes
│   │       ├── Constants.cs              📋 Application constants
│   │       └── Extensions.cs             📋 Extension methods
│   │
│   └── DistilleryMonitor.Tests/          # Unit & integration tests
│       ├── Core/                         # Core library tests
│       │   ├── Services/                 # Service layer tests
│       │   └── Models/                   # Model validation tests
│       └── Mobile/                       # Mobile app tests
│           ├── ViewModels/               # ViewModel unit tests
│           └── Services/                 # Platform service tests
│
├── docs/                                 # Additional documentation
│   ├── API.md                            📋 ESP32 API documentation
│   ├── ARCHITECTURE.md                   📋 Detailed architecture guide
│   └── DEPLOYMENT.md                     📋 Build & deployment guide
├── .gitignore                            ✅ Git ignore patterns
├── README.md                             ✅ Project overview & setup
└── DistilleryMonitor.sln                 ✅ Visual Studio solution file
```

### 📊 Implementation Status Legend
- ✅ **Completed** - Fully implemented and tested
- 🚧 **In Progress** - Currently being developed
- 📋 **Planned** - Scheduled for future implementation

### 🎯 Key Architecture Decisions

**Separation of Concerns:**
- **Core** - Business logic, models, and abstractions (platform-agnostic)
- **Mobile** - UI, platform services, and user interaction (MAUI-specific)
- **Tests** - Comprehensive testing for reliability

**Design Patterns:**
- **MVVM** - Clean separation between UI and business logic
- **Repository Pattern** - Abstracted data access layer
- **Dependency Injection** - Loose coupling and testability
- **Service Layer** - Centralized business operations

**Cross-Platform Strategy:**
- Shared Core library for maximum code reuse
- Platform-specific implementations where needed
- Responsive UI design for various screen sizes

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

