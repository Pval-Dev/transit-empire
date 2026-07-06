# Portfolio Positioning

This document explains how Transit Empire should be presented as a software engineering project.

Transit Empire should not be positioned only as a Unity game prototype. Its strongest value is that it behaves like a data-driven simulation system with runtime generation, autonomous entities, financial modeling, persistence, and contextual UI controllers.

## Recommended Project Description

Short version:

```text
Transit Empire is a data-driven airline management simulation built in Unity/C#.
```

Professional version:

```text
Transit Empire is a Unity/C# airline management simulation prototype featuring runtime world generation, autonomous aircraft operations, route network modeling, financial systems, and multi-pass JSON persistence.
```

Recruiter-friendly version:

```text
A simulation-focused Unity project that models airline operations through generated airports, aircraft state machines, route economics, financial constraints, and persistent progression.
```

## What The Project Demonstrates

Transit Empire demonstrates more than scene design or UI assembly.

It demonstrates:

- Data-driven runtime world generation
- External dataset preprocessing
- Autonomous entity behavior
- State machine-driven aircraft operations
- Route network simulation
- Passenger demand modeling
- Financial simulation
- Debt and investor systems
- Maintenance pressure
- Reputation and autonomy systems
- Multi-pass save/load reconstruction
- Contextual UI controllers
- Localization support
- Desktop and mobile map navigation

## Strongest Engineering Points

| Area | Why It Matters |
|---|---|
| World generation | Countries and airports are generated from structured data instead of being manually placed |
| Aircraft state machine | Aircraft operate autonomously through boarding, flying, arrival, turnaround, and repair states |
| Save/load system | The project persists and reconstructs a complex graph of airports, routes, aircraft, passengers, and finance data |
| Economy system | Loans, investors, maintenance, reputation, autonomy, and purchase restrictions interact as a financial simulation |
| Route network | Airports are connected dynamically and route performance is tracked over time |
| Data pipeline | Python scripts preprocess airport and country data into Unity-ready JSON resources |
| UI architecture | Interfaces are contextual and control different simulation workflows |

## How To Describe The Architecture

Good description:

```text
The project uses a centralized orchestration model, with GameManager coordinating simulation state while domain entities such as Airport, Plane, and Country own runtime behavior. Specialized managers handle persistence, finance, routes, UI, services, localization, and world generation.
```

Avoid saying only:

```text
It is a game made in Unity.
```

Better:

```text
It is a Unity-based simulation system with generated world data, operational entities, financial constraints, and persistent runtime state.
```

## How To Explain GameManager

`GameManager` should be described carefully.

Recommended wording:

```text
GameManager acts as the central runtime orchestrator for the prototype. It coordinates simulation time, money, route creation, airport ownership, fleet actions, reputation, autosave triggers, and UI mode state.
```

Avoid presenting it as perfect separation of concerns.

Better phrasing:

```text
Because the project was built as a functional prototype, several responsibilities are centralized in GameManager. The documentation identifies these boundaries and explains how the systems interact.
```

## How To Explain The Prototype Nature

Honest positioning is stronger than pretending the architecture is perfect.

Recommended wording:

```text
Transit Empire was built as a functional simulation prototype. Some systems are tightly coupled through Unity references and singleton-style managers, but the project demonstrates complete end-to-end systems: world generation, route operations, aircraft state machines, financial simulation, and persistence.
```

This makes the project credible.

## Resume Bullet Ideas

Use bullets like these:

```text
- Built a Unity/C# airline management simulation with data-driven airport and country generation from JSON datasets.
- Implemented autonomous aircraft state machines for boarding, route execution, arrival processing, revenue calculation, wear, and repair.
- Designed a route network system connecting airports through dynamic demand, aircraft assignment, route metrics, and operational constraints.
- Created a JSON save/load system capable of reconstructing countries, airports, routes, passenger maps, aircraft state, loans, investors, and global progression.
- Developed financial systems including loans, investors, maintenance debt, reputation, autonomy, and purchase restrictions.
- Built Python preprocessing scripts to convert airport CSV and country GeoJSON data into Unity-ready simulation resources.
```

## GitHub Repository Summary

Recommended repository summary:

```text
Technical documentation for Transit Empire, a data-driven airline management simulation built in Unity/C#.
```

Alternative:

```text
Architecture and systems documentation for a Unity/C# airline management simulation with runtime world generation, autonomous aircraft, financial systems, and JSON persistence.
```

## Project Highlights For README

Recommended highlights:

```text
- Runtime world generation from preprocessed airport and country datasets
- Autonomous aircraft operations using a state machine
- Dynamic route network with passenger demand and route metrics
- Financial simulation with loans, investors, reputation, autonomy, and maintenance
- Multi-pass JSON save/load reconstruction
- Contextual UI controllers for airports, aircraft, routes, countries, finance, and settings
```

## Interview Talking Points

If asked about the project, focus on these points:

| Topic | Talking Point |
|---|---|
| World generation | External datasets are converted into runtime Unity objects |
| Persistence | Save/load reconstructs object references by stable airport names |
| Simulation | Aircraft operate autonomously and feed the economy |
| Economy | Financial systems create constraints instead of just increasing money |
| Architecture | The prototype uses centralized orchestration with clear module boundaries |
| Tradeoffs | Some systems are coupled, but the project reached end-to-end functionality |

## Suggested Technical Narrative

A strong explanation:

```text
Transit Empire started as a Unity prototype, but it evolved into a simulation system. The most important part is not the visual layer, but the interaction between generated world data, airport demand, aircraft state machines, route economics, and persistent progression. The project uses GameManager as a central orchestrator, while Airport, Plane, and Country represent the main runtime entities.
```

## What Not To Overclaim

Do not claim:

```text
Clean architecture
Enterprise-grade architecture
Fully decoupled systems
Production-ready backend
Machine learning AI system
```

Better claims:

```text
Functional simulation prototype
Data-driven runtime generation
Centralized orchestration model
Autonomous entity state machine
Multi-pass JSON persistence
Financial simulation layer
```

## Final Positioning

Transit Empire is strongest when presented as:

```text
A technically ambitious simulation prototype that connects generated world data, autonomous aircraft behavior, route networks, financial systems, and persistent progression inside Unity.
```

This framing is honest, professional, and technically meaningful.
