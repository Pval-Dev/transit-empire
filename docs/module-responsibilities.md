# Module Responsibilities

This document maps the main Transit Empire components by category, responsibility, and technical value.

The project was built as a functional Unity/C# simulation prototype. Some responsibilities are centralized, especially inside `GameManager`, but the system still has clear runtime modules and domain boundaries.

## Responsibility Map

| Category | Component | Responsibility | Technical Value |
|---|---|---|---|
| Core | `GameManager` | Coordinates simulation time, money, airport ownership, fleet actions, routes, reputation, UI state flags, and global metrics | Central runtime orchestrator |
| Persistence | `SaveManager` | Saves and restores the full simulation state through JSON files | Multi-pass persistence and runtime reconstruction |
| Persistence UI | `SaveSlots` | Provides save/load slot selection and slot previews | User-facing persistence workflow |
| Finance | `LoanManager` | Creates loan offers, investor offers, active debts, repayment flags, and investor state | Financial product manager |
| Finance | `MantenimientoManager` | Displays airports with maintenance debt and triggers maintenance payments | Recurring cost management |
| World Builder | `AirportGenerator` | Builds countries, airports, sprites, colliders, and purchase buttons from `airports.json` | Data-driven runtime object generation |
| World Builder | `CountryBorderGenerator` | Builds country meshes, borders, labels, colliders, and click handlers from `countries.json` | Geospatial runtime visualization |
| World Builder | `CountryBorderGeneratorTutorial` | Builds a reduced country map for tutorial mode | Scoped onboarding world generation |
| Data Processing | `generate_airports.py` | Converts external airport data into a Unity-ready airport dataset | Offline data preprocessing |
| Data Processing | Country triangulation script | Converts GeoJSON polygons into triangulated country mesh data | Offline geometry preprocessing |
| Domain | `Country` | Tracks country price, ownership state, development progress, airport reveal thresholds, and aggregate metrics | Territory progression model |
| Domain | `Airport` | Generates passengers, stores route demand, manages capacity, reputation, routes, XP, upgrades, and maintenance | Network node simulation |
| Domain | `AirportGarage` | Stores aircraft inventory and equipped aircraft for an airport | Local asset ownership container |
| Domain | `Plane` | Executes boarding, flight, arrival, revenue, wear, repair, XP, and route metrics through a state machine | Autonomous operational entity |
| Routes | `RouteManager` | Controls route UI flow, route list creation, route selection, and route metrics display | Route interaction controller |
| Routes | `RtBuyUi` | Displays selected route destination, distance, and price before purchase | Route transaction confirmation |
| Fleet UI | `GarageManager` | Builds aircraft lists for garage, equipped, route assignment, and active route contexts | Contextual fleet browser |
| Fleet UI | `PlaneShopPanel` | Builds aircraft purchase catalog from the selected aircraft type | Asset acquisition interface |
| Airport UI | `AirportInformationUiManager` | Displays airport data and routes the user to shop, garage, route, and equip panels | Airport operations interface |
| Aircraft UI | `InformationManager` | Displays aircraft stats and performs buy, equip, delete, route assignment, maintenance, and upgrade actions | Contextual aircraft action controller |
| Country UI | `CountryUIManager` | Displays country purchase data, country status, airport ownership progress, and country preview render | Territory interface |
| Main UI | `InformationMain` | Displays global simulation metrics such as money, date, reputation, routes, passengers, autonomy, and speed | Operational dashboard |
| Interaction | `CountryClickHandler` | Detects country double-clicks and opens country UI | Map interaction observer |
| Interaction | `AirportActivator` | Opens airport purchase UI for visible unowned airports | Purchase entry point |
| Interaction | `BuyAirportButtomshop` | Confirms airport purchase and activates airport simulation | Airport purchase transaction |
| Interaction | `AirPortBuyPlanes` | Routes airport clicks to either airport interface or route destination selection | Airport interaction router |
| Interaction | `PlaneUi` | Opens aircraft information when clicking an aircraft in the map | Runtime asset inspection |
| Input | `CameraInputHandler` | Reads mouse, touch, pan, scroll, and pinch inputs | Input abstraction |
| Input | `CameraController` | Applies map panning, smooth zooming, and bounds clamping | Map navigation controller |
| Services | `NotificatorManager` | Creates categorized notifications with icons, colors, and sound | Global feedback service |
| Services | `AudioManager` | Manages music, SFX, volume settings, and playback categories | Audio service |
| Services | `Globalsettings` | Stores autosave, notifications, language, and user settings through `PlayerPrefs` | Settings persistence |
| Services | `Textexporter` | Applies scene-specific EN/ES localization dictionaries to UI text | Localization service |
| Services | `PlaneSpritesManager` | Resolves aircraft sprites and skins by aircraft type | Visual asset lookup service |
| Services | `GlobalButtonSound` | Registers click sound listeners on active UI buttons | Global UI audio helper |
| Services | `SafeAreaFit` | Adjusts UI anchors for devices with safe areas or notches | Mobile layout support |
| Helpers | `JsonHelper` | Wraps JSON arrays so Unity `JsonUtility` can parse them | Serialization helper |
| Tutorial | `tutorial` | Guides the first route creation flow through step validation and arrow hints | Onboarding state machine |

## Core Runtime Group

The core runtime group contains the systems that keep the simulation alive.

```text
GameManager
├── Simulation time
├── Money state
├── Airport ownership
├── Aircraft purchase/equip/delete
├── Route creation/deletion
├── Reputation
├── Financial penalties
├── Autosave triggers
└── UI mode flags
```

`GameManager` is the highest-level coordinator. It owns many global flags and delegates or interacts with other managers.

## Domain Entity Group

The main domain entities are `Country`, `Airport`, `AirportGarage`, and `Plane`.

```text
Country
└── Territory progression

Airport
└── Passenger demand and infrastructure

AirportGarage
└── Local aircraft inventory

Plane
└── Autonomous route execution
```

These components represent the simulated airline world.

## Financial Group

The financial system is split between `LoanManager`, `GameManager`, and `MantenimientoManager`.

```text
LoanManager
├── Loan offer creation
├── Investor offer creation
└── Active debt storage

GameManager
├── Daily payment execution
├── Penalty evaluation
└── Purchase restriction logic

MantenimientoManager
└── Maintenance debt payment UI
```

This split makes `LoanManager` the source of financial products, while `GameManager` executes the financial consequences during simulation time.

## Route And Fleet Group

Routes and fleet operations involve several modules.

```text
RouteManager
├── Route UI
└── Route metrics display

RtBuyUi
└── Route purchase confirmation

GarageManager
└── Aircraft selection by context

GameManager
├── Route creation
├── Aircraft assignment
└── Route deletion

Plane
└── Route execution
```

The route system is a collaboration between UI controllers, airport data, and autonomous aircraft behavior.

## World Generation Group

Transit Empire uses data-driven runtime generation.

```text
Python preprocessing
→ JSON datasets
→ AirportGenerator
→ CountryBorderGenerator
→ Runtime world objects
```

This group transforms external airport and geography data into usable Unity simulation objects.

## UI Group

The UI layer is composed of contextual controllers rather than one monolithic interface.

| Context | Controller |
|---|---|
| Global HUD | `InformationMain` |
| Airport | `AirportInformationUiManager` |
| Aircraft | `InformationManager` |
| Country | `CountryUIManager` |
| Garage/Fleet | `GarageManager` |
| Aircraft Shop | `PlaneShopPanel` |
| Route Purchase | `RtBuyUi` |
| Maintenance | `MantenimientoManager` |
| Save/Load | `SaveSlots` |

## Service Group

Service-style managers provide cross-cutting behavior:

```text
NotificatorManager = user feedback
AudioManager = sound and music
Globalsettings = persistent settings
Textexporter = localization
PlaneSpritesManager = aircraft visuals
GlobalButtonSound = button SFX
SafeAreaFit = mobile layout
```

These services are used by multiple unrelated systems.

## Architectural Note

Transit Empire is not a strict clean architecture project. It is a functional simulation prototype with centralized orchestration and direct Unity references.

However, the project contains identifiable architectural patterns:

- Singleton-style managers
- Runtime builders
- Contextual UI controllers
- Autonomous entity state machines
- DTO-based persistence
- Data-driven generation
- Service-style cross-cutting systems
- Observer-like interaction handlers

## Summary

The project can be understood through this simplified map:

```text
Data Processing
→ World Generation
→ Runtime Domain Entities
→ Core Simulation Orchestration
→ Economy / Routes / Fleet / UI
→ Persistence and Services
```

This module structure is the foundation for presenting Transit Empire as a software engineering project instead of only a Unity prototype.
