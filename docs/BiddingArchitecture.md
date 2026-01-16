# Bidding Architecture

Honot Bridge uses a modular Bidding System architecture to allow AI players to support different conventions (SAYC, Acol, Goren, etc.).

## Core Components

### 1. `IBiddingSystem`
The interface that defines a bidding engine.
```csharp
public interface IBiddingSystem
{
    string Name { get; }
    Bid GetBestBid(Auction auction, Hand hand);
}
```

### 2. `ParametricBidder`
A concrete implementation that can be configured with rule parameters, avoiding the need for separate classes for every variation.
**Parameters:**
- `NtMin` / `NtMax`: The High Card Point range for opening 1 NoTrump.
- `MajorMinLength`: Minimum length (4 or 5) to open a Major suit.

**Presets:**
- **SAYC**: 15-17 NT, 5-card Majors.
- **Acol**: 12-14 NT, 4-card Majors.
- **Goren**: 16-18 NT, 4-card Majors.

### 3. `MonteCarloAI` integration
The `MonteCarloAI` class accepts an `IBiddingSystem` in its constructor. When `GetBidAsync` is called, it delegates the logic to the injected system.

## Adding a New System

To add a new system (e.g., Precision):
1.  Extend `ParametricBidder` if it fits the model, or implement `IBiddingSystem` directly if the logic is fundamentally different (e.g., Strong Club systems).
2.  Register the new system in `SettingsViewModel`.
3.  Update `GameRoom` or `LobbyService` to map the string selection to the new Instance.
