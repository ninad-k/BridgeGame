# Contributing to Honor Bridge

We welcome contributions! To ensure high quality, please follow this workflow.

## 1. Branching Strategy

We use a simplified Feature Branch workflow.

-   **`main`**: The protected stable branch. Direct commits are restricted.
-   **`feature/{feature-name}`**: Create a new branch for every feature or bugfix.

### Naming Convention
-   Features: `feature/advanced-ai`, `feature/ui-polish`
-   Bugfixes: `fix/scoring-bug`, `fix/connection-retry`

## 2. Pull Request (PR) Policy

1.  **Create Branch**: `git checkout -b feature/my-cool-feature`
2.  **Commit**: Keep commits atomic and descriptive.
3.  **Push**: `git push origin feature/my-cool-feature`
4.  **Open PR**: Open a Pull Request against `main`.

### Requirements for Merging
-   **CI/CD**: The automated build must pass (Compile + Tests).
-   **Review**: The Repository Owner must approve the PR.

## 3. Development Setup
1.  Open `HonorBridge.sln` in VS Code or Visual Studio.
2.  Run `dotnet restore`.
    -   **On macOS/Linux**: Use the solution filter to avoid building the WPF client:
        ```bash
        dotnet build HonorBridge.Mac.slnf
        ```
3.  Run Server: `dotnet run --project src/HonorBridge.Server`.
4.  Run Client: `dotnet run --project src/HonorBridge.Client.Wpf` (Windows Only).

## 4. Coding Standards
-   Use standard C# Coding Conventions.
-   Ensure all logic (non-UI) has Unit Tests.
