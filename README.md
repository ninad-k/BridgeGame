# Honor Bridge ♠️♥️♣️♦️

**Honor Bridge** is a professional-grade, open-source Contract Bridge application built with modern .NET technologies. It features a robust Game Engine, a real-time SignalR Server, a WPF Client with a premium "Green Felt" interface, and an Advanced Monte Carlo AI.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Build Status](https://img.shields.io/github/actions/workflow/status/ninadk/HonorBridge/dotnet.yml?branch=main)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

## ✨ Key Features

-   **Advanced AI**: 
    -   **Bidding**: Configurable systems including **SAYC** (Standard American Yellow Card), Acol, and Goren.
    -   **Play**: Monte Carlo Simulation (Double Dummy) for optimal card play.
-   **Multiplayer Architecture**: Built on **SignalR** for real-time, low-latency communication.
-   **Universal Engine**: The core `HonorBridge.Engine` is a pure .NET library, portable to any platform (Mobile, Web, Desktop).
-   **Premium UI**: A WPF Client using **MVVM** architecture (CommunityToolkit) with a polished, responsive design.

## 🛠️ Technology Stack

-   **Core**: .NET 9.0 (C# 12)
-   **Server**: ASP.NET Core 9.0, SignalR
-   **Client**: WPF (Windows Presentation Foundation)
-   **Architecture**: Clean Architecture, Domain-Driven Design (DDD)
-   **Testing**: xUnit, Moq
-   **Logging**: Serilog

## 🚀 Getting Started

### Prerequisites
-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
-   Visual Studio 2022, JetBrains Rider, or VS Code

### Installation

1.  **Clone the repository**
    ```bash
    git clone https://github.com/ninadk/HonorBridge.git
    cd HonorBridge
    ```

2.  **Build the Project**
    
    *   **Windows (Full Experience)**
        ```bash
        dotnet build HonorBridge.sln
        ```
    *   **macOS / Linux (Backend Only)**
        Since WPF is Windows-only, use the Solution Filter to build the Engine, Server, and AI:
        ```bash
        dotnet build HonorBridge.Mac.slnf
        ```

3.  **Run the Game**
    
    *   **Server**: `dotnet run --project src/HonorBridge.Server`
    *   **Client**: `dotnet run --project src/HonorBridge.Client.Wpf` (Windows Only)

    *Tip: If using Rider, use the **"Play Game"** Run Configuration to launch both instantly.*

## 📖 Documentation

-   [Architecture Overview](docs/Architecture.md) - High-level design and diagrams.
-   [Sequence Diagrams](docs/SequenceDiagrams.md) - Game flow and AI interaction.
-   [Bidding Architecture](docs/BiddingArchitecture.md) - How the Parametric Bidder works.

## 🤝 Contributing

We welcome contributions from the community! Whether it's improving the AI, porting the client to MAUI/Blazor, or fixing bugs.

Please read our [Contributing Guide](CONTRIBUTING.md) for details on our Code of Conduct and the process for submitting Pull Requests.

## ⚖️ License

This project is licensed under the MIT License - see the LICENSE file for details.

---
*Built with ❤️ by Ninad Kulkarni and the Open Source Community.*
