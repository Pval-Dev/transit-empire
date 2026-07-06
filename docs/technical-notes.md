# Technical Notes

This document summarizes important engineering notes about Transit Empire’s implementation, architecture style, strengths, and known prototype tradeoffs.

Transit Empire was developed as a functional Unity/C# simulation prototype. The codebase prioritizes working systems, runtime behavior, and feature integration over strict architectural separation.

## Project Nature

Transit Empire is best described as:

```text
A data-driven airline management simulation prototype built in Unity/C#.
```

It includes:

- Runtime world generation
- Airport network simulation
- Autonomous aircraft behavior
- Financial systems
- Save/load persistence
- UI controllers
- Localization
- Desktop and mobile map navigation

The project should be evaluated as a simulation system, not only as a game scene.

## Architecture Style

The architecture uses a centralized manager model.

Main characteristics:

- `GameManager` acts as the main runtime orchestrator.
- Domain entities such as `Airport`, `Plane`, and `Country` own significant simulation logic.
- UI controllers interact directly with `GameManager` and runtime entities.
- Services are accessed through singleton-style instances.
- Save/load uses DTO classes and JSON files.
- World generation is data-driven through preprocessed JSON.

## Strong Technical Areas

### Data-Driven World Generation

The project uses external data and preprocessing scripts to generate runtime world content.

Strengths:

- Airport data is generated from structured source data.
- Country geometry is generated from GeoJSON.
- Unity builds countries and airports at runtime.
- The simulation world is not manually placed object by object.

### Autonomous Aircraft State Machine

`Plane` uses a state machine to control operation.

It handles:

- Boarding
- Flying
- Arrival
- Waiting
- Turnaround
- Out-of-service repair

This gives aircraft independent runtime behavior.

### Multi-Pass Persistence

`SaveManager` restores a complex runtime graph.

It reconstructs:

- Countries
- Airports
- Routes
- Passenger maps
- Aircraft
- Loans
- Investors
- Global state

References are restored by stable names instead of direct Unity object references.

### Financial Simulation

The economy includes:

- Cash flow
- Loans
- Investors
- Maintenance debt
- Reputation
- Autonomy
- Purchase restrictions
- Penalty thresholds

This creates economic pressure beyond simple money accumulation.

### Contextual UI

The UI system changes behavior based on current operational context.

Examples:

- Aircraft shop mode
- Garage mode
- Route assignment mode
- Active route fleet mode
- Flying aircraft inspection
- Country purchase mode
- Country status mode

## Prototype Tradeoffs

Transit Empire contains several intentional or historical prototype tradeoffs.

### Centralized GameManager

`GameManager` owns many responsibilities:

- Simulation time
- Money
- Airport ownership
- Plane purchase/equip/delete
- Route creation
- Reputation
- Loan execution
- UI flags
- Global metrics

This made development faster, but it also means multiple systems are coupled to a single central class.

### Direct UI Coupling

Many UI scripts call `GameManager` directly.

This is practical in Unity prototypes, but it means UI and domain logic are tightly connected.

Example pattern:

```text
Button click
→ UI controller
→ GameManager method
→ Runtime entity mutation
```

### Singleton Services

Several services are accessed as singletons:

```text
GameManager.Instance
SaveManager.Instance
AudioManager.Instance
NotificatorManager.Instance
PlaneSpritesManager.Instance
Globalsettings.instance
Textexporter.Instance
```

This simplifies access but increases global coupling.

### Mixed Responsibilities In Runtime Entities

Some domain entities contain more than one responsibility.

Examples:

- `Airport` handles simulation, visual state, progression, and maintenance.
- `Plane` handles movement, revenue, wear, XP, and route logic.
- `LoanManager` handles both financial offer logic and UI generation.

These are acceptable in a prototype, but they would be candidates for extraction in a production refactor.

## Refactor Opportunities

If the project were refactored for production, good extraction targets would be:

| Current Area | Possible Extraction |
|---|---|
| `GameManager` money logic | `EconomyService` |
| `GameManager` route logic | `RouteService` |
| `GameManager` fleet logic | `FleetService` |
| `GameManager` date logic | `SimulationClock` |
| `Airport` passenger generation | `DemandGenerator` |
| `Plane` revenue calculation | `RevenueCalculator` |
| `Plane` wear logic | `MaintenanceModel` |
| `LoanManager` UI creation | `LoanUIController` |
| `LoanManager` finance rules | `FinanceService` |
| UI state flags | `UIStateController` |

These refactors are not required for the documentation repository, but they are useful to understand the system’s natural boundaries.

## Engineering Patterns Present

Even as a prototype, the project demonstrates several recognizable engineering patterns.

| Pattern | Example |
|---|---|
| Singleton-style managers | `GameManager`, `SaveManager`, `AudioManager` |
| Runtime builders | `AirportGenerator`, `CountryBorderGenerator` |
| DTO persistence | `GameSave`, `AirportSave`, `PlaneSave` |
| State machine | `Plane.PlaneState` |
| Contextual UI controllers | `InformationManager`, `GarageManager` |
| Data-driven configuration | `airports.json`, `countries.json` |
| Observer-like flow | `tutorial.StepsValidator()` |
| Service layer | `NotificatorManager`, `Textexporter`, `PlaneSpritesManager` |
| Input abstraction | `CameraInputHandler.InputData` |

## Documentation Strategy

This repository documents Transit Empire as a software system.

The documentation focuses on:

- Architecture
- Runtime systems
- Simulation loops
- Persistence
- Data pipelines
- Component responsibilities
- Technical value

It avoids presenting the project only as a game prototype.

## Suggested Repository Positioning

Recommended short description:

```text
Technical documentation for Transit Empire, a data-driven airline management simulation built in Unity/C#.
```

Recommended engineering positioning:

```text
Transit Empire demonstrates runtime world generation, autonomous simulation entities, route network modeling, financial systems, and multi-pass JSON persistence inside a Unity/C# prototype.
```

## Summary

Transit Empire is not architecturally perfect, but it is technically meaningful.

Its value comes from the interaction of several systems:

```text
External data
→ Runtime world generation
→ Airport network simulation
→ Autonomous aircraft operations
→ Financial constraints
→ Persistent progression
→ Contextual UI control
```

That combination makes it a strong portfolio project when documented honestly as a functional simulation prototype.
