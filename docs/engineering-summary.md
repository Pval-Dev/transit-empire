# Engineering Summary

Transit Empire is a Unity/C# airline management simulation prototype focused on runtime systems rather than static scene content.

The project combines world generation, airport network simulation, autonomous aircraft operations, financial modeling, persistence, contextual UI, and external data preprocessing.

## Project Summary

Transit Empire models an airline company operating across a generated world map.

The simulation includes:

- Countries that can be purchased
- Airports that generate passenger demand
- Aircraft that operate autonomously
- Routes that connect airports
- Financial systems that create expansion constraints
- Save/load systems that preserve runtime state

## Core Engineering Idea

The core engineering idea is:

```text
Generate a world from data
→ Build a network of airport nodes
→ Allow route creation between nodes
→ Assign autonomous aircraft to routes
→ Convert aircraft operations into money and progression
→ Persist the entire simulation graph
```

## Main Technical Systems

| System | Description |
|---|---|
| World generation | Builds countries, airports, borders, colliders, and labels from JSON data |
| Airport simulation | Generates destination-specific passenger demand and manages infrastructure |
| Aircraft simulation | Runs autonomous aircraft state machines for boarding, flying, arrival, repair, and progression |
| Route system | Creates airport connections and tracks active/historical route metrics |
| Economy system | Handles revenue, loans, investors, maintenance, reputation, autonomy, and penalties |
| Persistence system | Saves and restores the complete simulation through JSON DTOs |
| UI system | Provides contextual controllers for countries, airports, aircraft, routes, finance, settings, and save slots |
| Data pipeline | Uses Python scripts to transform airport CSV and country GeoJSON into Unity resources |

## Runtime Architecture

Transit Empire uses a centralized orchestration model.

`GameManager` coordinates the main runtime state:

- Simulation time
- Money
- Airport ownership
- Aircraft purchase/equip/delete
- Route creation/deletion
- Reputation
- Financial penalties
- Autosave triggers
- UI mode flags

Specialized components handle specific areas:

```text
SaveManager = persistence
LoanManager = financial products
AirportGenerator = airport world generation
CountryBorderGenerator = map geometry generation
RouteManager = route UI flow
InformationManager = aircraft action UI
Plane = autonomous aircraft simulation
Airport = demand and infrastructure simulation
Country = territorial progression
```

## Runtime Entity Model

The main simulation entities are:

| Entity | Role |
|---|---|
| `Country` | Territory, airport reveal progression, ownership state |
| `Airport` | Passenger demand node, infrastructure, routes, maintenance |
| `AirportGarage` | Local aircraft inventory |
| `Plane` | Autonomous route executor |

These entities form the operational graph of the simulation.

## Simulation Loop

The simulation loop has two major parts:

### Continuous Runtime Loop

Aircraft and airports update continuously.

```text
Airport demand generation
→ Aircraft boarding
→ Aircraft flight
→ Arrival processing
→ Revenue and XP
→ Route metrics
```

### Daily Simulation Loop

`GameManager.AdvanceDay()` handles long-term pressure.

```text
Advance day
→ Process loans
→ Process investors
→ Apply financial penalties
→ Process maintenance
→ Update reputation
→ Trigger autosave
```

## Data-Driven World

The project uses external data transformed into runtime assets.

```text
Airport CSV
→ Python preprocessing
→ airports.json
→ AirportGenerator
→ Runtime airport objects
```

```text
Country GeoJSON
→ Python triangulation
→ countries.json
→ CountryBorderGenerator
→ Runtime country meshes
```

This makes the world scalable and generated rather than manually authored object by object.

## Persistence Model

`SaveManager` stores the simulation as structured JSON.

It saves:

- Global airline state
- Financial obligations
- Country states
- Airport states
- Route connections
- Passenger demand maps
- Aircraft stats and state machines

Loading is multi-pass because the world must be regenerated before references can be reconnected.

```text
Regenerate world
→ Build lookup by airport name
→ Restore countries
→ Restore airports
→ Restore routes
→ Restore passengers
→ Restore aircraft
```

## Financial Model

The economy system includes:

- Route revenue
- Aircraft costs
- Airport costs
- Country purchases
- Route purchases
- Loans
- Investors
- Maintenance debt
- Reputation
- Autonomy
- Purchase restrictions

Financial health affects what the user can do, making the economy part of the simulation rather than just a number display.

## UI Model

The UI is contextual.

Different panels handle different operational contexts:

| Context | Controller |
|---|---|
| Global dashboard | `InformationMain` |
| Country | `CountryUIManager` |
| Airport | `AirportInformationUiManager` |
| Aircraft | `InformationManager` |
| Garage/fleet | `GarageManager` |
| Routes | `RouteManager` |
| Loans/investors | `LoanManager` |
| Maintenance | `MantenimientoManager` |
| Save/load | `SaveSlots` |
| Settings | `SettingsMenu` |

Most UI controllers read selection state from `GameManager` and delegate actions back to simulation methods.

## Prototype Tradeoffs

Transit Empire was built as a functional prototype.

Important tradeoffs:

- `GameManager` contains many responsibilities.
- UI controllers are directly coupled to `GameManager`.
- Several services use singleton-style access.
- Some domain objects also handle visual or UI-related behavior.
- Financial logic and finance UI are partially mixed in `LoanManager`.

These tradeoffs are acceptable for a prototype and are documented transparently.

## Engineering Strengths

The strongest technical parts of the project are:

- Data-driven world generation
- Autonomous aircraft state machine
- Route network simulation
- Multi-pass save/load reconstruction
- Financial simulation with constraints
- Contextual UI workflows
- External preprocessing pipeline
- Persistent operational state

## Engineering Value

Transit Empire demonstrates the ability to build an interconnected simulation system.

The project shows practical experience with:

- Runtime object creation
- Stateful simulation entities
- Graph-like route networks
- Unity UI flows
- JSON persistence
- Data preprocessing
- System orchestration
- Incremental prototype architecture

## Final Summary

Transit Empire is best understood as:

```text
A data-driven airline management simulation prototype with generated world data, autonomous aircraft operations, route economics, financial constraints, and persistent runtime progression.
```

Its value comes from the interaction of multiple systems working together, not from any single isolated script.
