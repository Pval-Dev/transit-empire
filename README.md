# Transit Empire

> Unity/C# airline-management simulation prototype focused on runtime systems, route/fleet orchestration, passenger demand, aircraft lifecycle state, and persistence.

**Transit Empire** is an archived management-simulation prototype built in Unity. The player grows an airline by purchasing airports, creating routes, assigning aircraft, transporting direct and connecting passengers, and reinvesting revenue into airport and fleet progression.

This repository is a **curated technical portfolio**, not a full Unity project export. The documentation and code samples below were rebuilt from the original project source so the repository only claims behavior that can be verified in that source snapshot.

![Transit Empire world map](screenshots/world-map.png)

---

## Engineering Highlights

- **Data-driven airport generation** from a JSON resource into a continent/country hierarchy.
- **Destination-level passenger demand** stored per airport with different generation rules for regional, capital, and international airports.
- **Route and fleet constraints** including route-slot capacity, runway capacity, route pricing, and aircraft reassignment.
- **Aircraft lifecycle state machine** covering `Idle`, `Boarding`, `Flying`, `Arrived`, `Waiting`, `Turnaround`, and `OutOfService`.
- **Connection-passenger handling** with a manifest that transfers passengers through intermediate airports.
- **Aircraft wear and reliability simulation** with mileage-based degradation, critical-failure probability, repair time, and repair cost.
- **Progression systems** for both airports and aircraft using XP, levels, stat points, and upgrade curves.
- **JSON persistence** for global economy state, purchased airports, routes, and aircraft assignments/stats using a staged reconstruction pass.
- **Management UI orchestration** for airport purchase, route creation, fleet assignment, aircraft inspection, and speed controls.

---

## Core Simulation Loop

```mermaid
flowchart LR
    Acquire["Acquire airport"] --> Route["Create route"]
    Route --> Assign["Assign aircraft"]
    Assign --> Demand["Board passenger demand"]
    Demand --> Flight["Simulate flight"]
    Flight --> Revenue["Apply revenue / fuel / condition"]
    Revenue --> Progress["Airport & aircraft progression"]
    Progress --> Route
```

The project is systems-driven rather than direct-control focused: aircraft execute their own runtime lifecycle while the player manages network and capacity decisions.

---

## Architecture

```mermaid
flowchart TD
    Data["airports.json"] --> Generator["AirportGenerator"]
    Generator --> Airports["Airport / Country runtime objects"]
    UI["Management UI controllers"] --> GM["GameManager"]
    GM --> Airports
    GM --> Routes["Route state & fleet assignment"]
    GM --> Planes["Plane runtime state machines"]
    Airports --> Planes
    Planes --> GM
    Save["SaveManager"] --> GM
    Save --> Airports
    Save --> Planes
```

The original prototype uses `GameManager` as a central coordinator. That made iteration fast, but it also became the largest coupling point in the project. The architectural notes document both the useful boundaries and the places I would split today.

See [`docs/architecture.md`](docs/architecture.md).

---

## Aircraft Runtime

Aircraft progress through explicit operational states:

```text
Idle
  ↓
Boarding ── insufficient demand ──> Waiting
  ↓                                 │
Flying <────────────────────────────┘
  ↓
Arrived
  ↓
Turnaround
  ↓
Boarding

Flying ── critical condition failure ──> OutOfService ──> Boarding / Idle
```

During boarding, direct passengers are loaded first. Remaining capacity can be filled by passengers whose final destination can be reached through the current network. A connection manifest preserves those final destinations until arrival.

During flight, distance contributes to wear and reliability changes. Low reliability can trigger a critical failure, forcing the aircraft into the repair path.

Representative code: [`AircraftStateMachine.sample.cs`](code-samples/AircraftStateMachine.sample.cs)

---

## Airport Demand and Progression

Each airport maintains a destination-demand dictionary:

```csharp
Dictionary<Airport, int> passengersByRoute;
```

Passenger generation is influenced by continent, country, airport, reputation, and airport-type multipliers. Destination selection changes by airport category:

- **Regional** airports favor nearby airports in the same country.
- **Capital** airports favor nearby airports in the same continent.
- **International** airports sample from higher-tier airports across the active network.

Capacity pressure can reduce reputation, while flights award airport XP. Airport progression can increase runway capacity, route slots, passenger capacity, and reputation ceiling.

Representative code: [`AirportDemand.sample.cs`](code-samples/AirportDemand.sample.cs)

---

## Route and Fleet Operations

Route creation is not just a visual line. The runtime checks and updates several pieces of state:

```text
origin / destination validation
→ route price
→ route-slot availability
→ outbound + inbound route references
→ passenger-demand entry
→ route ownership cost
→ aircraft runway-capacity check
→ aircraft destination assignment
```

Historical route metrics are accumulated when aircraft are removed from a route.

Representative code: [`RouteOperations.sample.cs`](code-samples/RouteOperations.sample.cs)

---

## Persistence

The prototype saves to one JSON file under `Application.persistentDataPath`.

The save contains:

- global money, elapsed days, earned/spent totals;
- purchased airport state and upgrade values;
- route references stored as indexes into the generated airport list;
- aircraft stats, progression, condition, origin/destination references, and runtime state.

Loading is staged: first airports are restored, then route references, then aircraft are recreated and reattached to their origin/destination airports.

This is a useful reconstruction strategy for Unity object references, but it is still prototype persistence: there is no schema versioning or migration layer.

Representative code: [`SaveManager.sample.cs`](code-samples/SaveManager.sample.cs)

---

## Data-Driven World Setup

`AirportGenerator` reads `Resources/airports.json`, locates the configured continent object, creates a country object when necessary, spawns airport prefabs, assigns gameplay metadata, and registers the generated airport with `GameManager`.

The archived source snapshot bundled a small sample dataset. The repository therefore presents this as a **data-driven generation system**, not as a benchmarked large-scale world generator.

Representative code: [`AirportGenerator.sample.cs`](code-samples/AirportGenerator.sample.cs)

---

## Screenshots

### Main Menu
![Main menu](screenshots/main-menu.png)

### Runtime Simulation
![Runtime simulation](screenshots/runtime-simulation.png)

### Country / Airport Acquisition
![Country purchase](screenshots/country-purchase.png)

### Route Network
![Route network](screenshots/route-network-closeup.png)

### Route Details
![Route details](screenshots/route-details-panel.png)

### Fleet Assignment
![Fleet assignment](screenshots/fleet-assignment.png)

---

## Selected Code Samples

| Sample | Demonstrates |
|---|---|
| [`AircraftStateMachine.sample.cs`](code-samples/AircraftStateMachine.sample.cs) | State transitions, boarding, connection demand, movement, wear, critical failure, turnaround |
| [`AirportDemand.sample.cs`](code-samples/AirportDemand.sample.cs) | Destination demand, capacity pressure, reputation, airport XP/upgrades |
| [`RouteOperations.sample.cs`](code-samples/RouteOperations.sample.cs) | Route pricing, slot/runway validation, route creation, fleet assignment |
| [`SaveManager.sample.cs`](code-samples/SaveManager.sample.cs) | JSON serialization and staged reference reconstruction |
| [`AirportGenerator.sample.cs`](code-samples/AirportGenerator.sample.cs) | JSON-driven runtime object generation and hierarchy setup |

These are **curated excerpts** from the archived source. Some field names and method boundaries were normalized for readability, but no additional gameplay systems were invented for the portfolio version.

---

## Documentation

- [`docs/architecture.md`](docs/architecture.md) — runtime boundaries and system relationships.
- [`docs/persistence.md`](docs/persistence.md) — save model, reconstruction flow, and limitations.
- [`docs/engineering-notes.md`](docs/engineering-notes.md) — what worked, what aged poorly, and how I would redesign it today.
- [`docs/README.md`](docs/README.md) — documentation index.

Editable Mermaid sources live under [`diagrams/`](diagrams/).

---

## What I Would Change Today

Transit Empire was one of my earlier large Unity/C# systems projects. The prototype works through direct Unity object references and a central manager, but today I would redesign several boundaries:

- split the ~900-line `GameManager` into route, fleet, economy, selection, and simulation services;
- move business rules out of UI controllers;
- replace broad singleton access with explicit dependencies/events;
- separate persistent IDs from scene/runtime object references;
- version the save schema and add migration logic;
- add deterministic simulation tests for demand, route economics, and state transitions;
- replace polling/`InvokeRepeating` coordination with clearer simulation ticks where appropriate;
- isolate catalog/balance data from runtime orchestration.

Those limitations are part of why the project is useful in the portfolio: it shows both the scale of the system I could build and the architectural problems I learned to recognize afterward.

---

## Verified Scope / Removed Experiments

The original project folder also contained an unfinished **Unity ML-Agents experiment**. The agent scripts were not referenced by the archived scene or prefabs, and ML-Agents was not part of the implemented game loop. It is intentionally excluded from this portfolio repository.

Portfolio scope is intentionally limited to behavior verified in the archived source snapshot used for this revision.

---

## Project Status

**Archived prototype / portfolio project.**

Transit Empire is not presented as a finished commercial game. Its value is the interconnected simulation work: demand generation, routes, fleet state, economy, progression, runtime recovery, UI orchestration, and persistence.

---

## Tech Stack

```text
Engine: Unity 6 (6000.4.0f1)
Language: C#
Rendering: Universal Render Pipeline / 2D
UI: Unity UI + TextMeshPro
Input: Unity Input System
Persistence: JsonUtility + local JSON file
Data: JSON Resources
Documentation: Markdown + Mermaid
```

## Author

Developed by **Pval-Dev**.
