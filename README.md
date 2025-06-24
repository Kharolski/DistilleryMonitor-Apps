# DistilleryMonitor Apps

Cross-platform mobile and desktop applications for real-time distillery temperature monitoring. Built with .NET MAUI, connecting to ESP32 sensor system with live data visualization and intelligent alerting.

## 🎯 **LIVE FEATURES** ✅

### 📊 **Real-Time Monitoring**
- **Live Temperature Display** - Real-time data from 3 sensors (Kolv, Destillat, Kylare)
- **Interactive Graphs** - 2-hour temperature history with dynamic scaling
- **Visual Status Indicators** - Color-coded temperature zones (🔵 Kalt, 🟢 Optimal, 🟡 Varning, 🔴 Kritisk)
- **Auto-Refresh** - Configurable update intervals (1-30 seconds)

### ⚙️ **Smart Configuration**
- **Dual Settings System** - Local app settings + ESP32 synchronization
- **Per-Sensor Thresholds** - Individual temperature limits for each sensor
- **Real-Time Updates** - Settings changes instantly update graphs and alerts
- **Mock Data Mode** - Full testing environment without hardware

### 📱 **Modern Mobile Experience**
- **Responsive Design** - Optimized for phones and tablets
- **Intuitive Navigation** - Tap sensors for detailed views
- **Live Connection Status** - Always know your data source
- **Professional UI** - Dark theme with smooth animations

### 🔧 **Advanced Features**
- **ESP32 Integration** - Direct communication with hardware
- **Settings Synchronization** - App ↔ ESP32 two-way sync
- **Fallback Systems** - Graceful handling of connection issues
- **Developer Mode** - Advanced debugging and testing tools

## 🏗️ **Architecture**

```
DistilleryMonitor-Apps/
├── src/
│   ├── DistilleryMonitor.Core/     # ✅ Business logic & ESP32 models
│   ├── DistilleryMonitor.Mobile/   # ✅ MAUI app with live graphs
│   └── DistilleryMonitor.Tests/    # 📋 Unit tests (planned)
```

## 📊 **Implementation Status**

### ✅ **COMPLETED & WORKING**
- **Core Architecture** - Models, services, dependency injection
- **ESP32 API Integration** - Full REST API communication
- **Real-Time Data Display** - Live temperature monitoring
- **Interactive Temperature Graphs** - 2-hour history with reference lines
- **Settings Management** - Local + ESP32 synchronization
- **Responsive Mobile UI** - Professional design
- **Auto-Update System** - Configurable refresh intervals
- **Mock Data System** - Complete testing environment
- **Temperature Threshold Service** - Real-time graph updates
- **Status Color Coding** - Visual temperature zone indicators
- **Connection Management** - Robust error handling

### 📋 **PLANNED ENHANCEMENTS**
- Unit test coverage
- iOS/Android platform optimizations
- Data export functionality
- Historical data analysis

## 🛠️ **Tech Stack**
- **.NET 8** - Latest LTS framework
- **.NET MAUI** - Cross-platform UI framework
- **Microsoft.Maui.Graphics** - Custom graph rendering
- **CommunityToolkit.Mvvm** - Modern MVVM implementation
- **ESP32 REST API** - Real-time sensor communication

## 📱 **Screenshots & Features**

### Main Dashboard
- Live sensor grid with current temperatures
- Color-coded status indicators
- Connection status display
- Quick access to detailed views
- Interactive 2-hour temperature graph for all 3 sensors

### Temperature Detail View
- Interactive 2-hour temperature graph
- Real-time reference lines (optimal/warning/critical)
- Live temperature display with status
- Direct access to sensor settings

### Settings Management
- Individual sensor threshold configuration
- ESP32 synchronization controls
- Update interval configuration

### About Page & Developer Mode
- **Hidden Developer Mode** - Tap version 7 times to activate
- **Advanced Logging** - View up to 100 recent log entries
- **Mock Data Toggle** - Switch between live and test data
- **Debug Controls** - Advanced troubleshooting tools
- **Developer Mode Exit** - Clean return to normal operation
  
## 🔧 **Development Setup**

### Prerequisites
- Visual Studio 2022 with MAUI workload
- .NET 8 SDK
- ESP32 device with DistilleryMonitor firmware (optional - mock data available)

### Quick Start
1. Clone repository
2. Open `DistilleryMonitor.sln`
3. Set `DistilleryMonitor.Mobile` as startup project
4. Enable "Mock Data Mode" in settings for testing
5. Run and explore!

## 📡 **ESP32 API Integration**

### Supported Endpoints
- `GET /api/temperatures` - Live sensor readings
- `GET /api/config/{sensorName}` - Sensor-specific settings
- `POST /api/config/{sensorName}` - Update sensor thresholds
- Full error handling and fallback systems

### Features
- **Automatic Discovery** - App finds ESP32 on network
- **Two-Way Sync** - Settings sync between app and hardware
- **Offline Mode** - Graceful degradation with mock data
- **Real-Time Updates** - Live data streaming

## 🎨 **Design Philosophy**
- **User-First** - Intuitive interface for non-technical users
- **Real-Time Focus** - Live data with immediate visual feedback
- **Professional Quality** - Production-ready monitoring solution
- **Extensible** - Clean architecture for future enhancements

## 🤝 **Contributing**
This project demonstrates modern .NET MAUI development with real-world IoT integration. Perfect example of:
- Clean Architecture principles
- Real-time data visualization
- Cross-platform mobile development
- Hardware-software integration

## 📄 **License**
MIT License - see LICENSE file for details.

---
*Built with ❤️ using .NET MAUI C# and ESP32 C/C++*
