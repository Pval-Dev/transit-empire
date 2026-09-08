# Engineering Notes

Transit Empire is useful in this portfolio because it captures a real transition point: the project grew beyond isolated gameplay scripts into a network of simulation systems with shared state, persistence, progression, and UI orchestration.

The source also makes the costs of that growth visible.

## What Worked Well

### Explicit state for aircraft behavior

The aircraft lifecycle is modeled with named states rather than a collection of unrelated booleans. That gives boarding, flight, arrival, waiting, turnaround, and repair clear execution boundaries.

### Destination-level demand

Passengers are represented by destination rather than only as one airport total. That makes direct vs. connecting passengers and network effects possible.

### Data-driven airport creation

Airports are generated from JSON instead of being fully hardcoded into scene objects. The generator also creates country containers on demand and attaches gameplay metadata to the spawned airports.

### Reference reconstruction in persistence

The save system recognizes that Unity object references cannot simply be serialized to JSON. Saving route/airport indexes and reconnecting them after the world exists is a sound prototype-level solution.

### Capacity as a cross-system constraint

The simulation tracks multiple kinds of capacity: passenger capacity, route slots, runway usage, and aircraft capacity/range. Those constraints connect airport progression to fleet and route decisions.

## Where the Prototype Shows Its Age

### `GameManager` accumulated too many responsibilities

The class grew to roughly 900 lines in the archived source and handles money, time speed, airport purchase, aircraft catalog/purchase, selected objects, garage modes, route creation, route deletion, fleet assignment, aggregate metrics, country purchase, and debug/save flags.

That is the clearest refactor target.

### UI state and domain state are mixed

Several UI controllers make direct calls into `GameManager` and encode mode transitions in booleans. The result works for a prototype but makes the allowed transitions harder to reason about.

### Runtime objects double as domain models

`Airport` and `Plane` are `MonoBehaviour` classes containing both simulation rules and scene/presentation concerns. This makes pure unit testing difficult.

### Persistence relies on runtime ordering

Indexes are practical for a prototype but brittle when content order changes. Stable IDs would be safer.

### Some calculations are embedded in long runtime methods

Trip revenue, passenger transfer, wear, notifications, XP, and route statistics are combined in arrival handling. Breaking those calculations into pure functions would improve testability and make balancing safer.

## ML-Agents Experiment

The original folder contained two scripts that referenced `Unity.MLAgents`, an ML-Agents config/timer folder, and a local-file package dependency.

The archived scene/prefab YAML did not reference either agent script. Because the experiment never became part of the implemented game architecture, it was excluded from the portfolio and from the cleaned source archive prepared during this review.

This is intentionally not listed as a Transit Empire feature.

## What I Would Build Differently Today

A modern architecture could keep Unity as the view/runtime host while moving the simulation into plain C#:

```text
Unity Views / Input
        ↓
Application Services
  RouteService
  FleetService
  AirportService
  EconomyService
        ↓
Domain Models
  AirportState
  AircraftState
  Route
  DemandMatrix
        ↓
Persistence Adapter
```

The main benefits would be:

- deterministic unit tests without loading a Unity scene;
- clearer ownership of mutations;
- easier save-version migration;
- fewer singleton dependencies;
- reusable simulation code;
- simpler UI controllers;
- explicit events when money, routes, airport levels, or aircraft state changes.

Transit Empire does not pretend to already have that architecture. The point of preserving it is to show the systems that were built, and the engineering judgment developed by seeing where the prototype architecture eventually became expensive.
