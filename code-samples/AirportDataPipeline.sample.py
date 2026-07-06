"""
Representative sample extracted from Transit Empire's airport data pipeline.

This script converts real-world airport CSV data and country GeoJSON polygons
into a Unity-ready `airports.json` dataset.

The production version contains a much larger COUNTRY_CONFIG table.
This sample keeps a small subset to show the architecture without dumping
the full project dataset.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import random
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass
from pathlib import Path


# ---------------------------------------------------------------------
# Global distribution targets
# ---------------------------------------------------------------------

TARGET_TOTAL = 10_000
TARGET_T2 = round(TARGET_TOTAL * 0.08)   # International hubs
TARGET_T1 = round(TARGET_TOTAL * 0.17)   # Capital / major airports
TARGET_T0 = TARGET_TOTAL - TARGET_T2 - TARGET_T1  # Regional airports

PROFILE_MINIMUMS = {
    "giant":  (5, 15, 30),
    "large":  (2, 5, 10),
    "medium": (1, 3, 6),
    "small":  (1, 2, 3),
}


@dataclass(frozen=True)
class CountryConfig:
    unity_name: str
    continent: str
    country_price_multiplier: float
    passenger_multiplier: float
    tier2_weight: int
    profile: str


# Production version contains the full supported country table.
COUNTRY_CONFIG: dict[str, CountryConfig] = {
    "US": CountryConfig("UnitedStates", "NA", 3.2, 1.8, 50, "giant"),
    "CA": CountryConfig("Canada", "NA", 3.0, 1.6, 15, "giant"),
    "MX": CountryConfig("Mexico", "NA", 2.0, 1.3, 10, "large"),

    "FR": CountryConfig("France", "EP", 2.8, 1.5, 12, "large"),
    "DE": CountryConfig("Germany", "EP", 2.9, 1.5, 13, "large"),
    "ES": CountryConfig("Spain", "EP", 2.5, 1.4, 9, "large"),
    "GB": CountryConfig("UK", "EP", 3.0, 1.6, 13, "large"),

    "BR": CountryConfig("Brazil", "SA", 2.2, 1.4, 18, "giant"),
    "AR": CountryConfig("Argentina", "SA", 1.9, 1.2, 7, "large"),

    "CN": CountryConfig("China", "A", 2.5, 1.6, 40, "giant"),
    "JP": CountryConfig("Japan", "A", 3.2, 1.7, 18, "large"),
    "IN": CountryConfig("India", "A", 2.0, 1.5, 20, "giant"),

    "ZA": CountryConfig("SouthAfrica", "AFR", 2.2, 1.3, 7, "large"),
    "NG": CountryConfig("Nigeria", "AFR", 1.8, 1.3, 6, "large"),

    "AU": CountryConfig("Australia", "O", 2.8, 1.5, 12, "giant"),
    "NZ": CountryConfig("NewZealand", "O", 2.6, 1.4, 3, "medium"),
}

EXCLUDED_ISOS = {
    "MT", "CY", "GI", "IM", "JE", "GG", "AX", "FO",
    "PR", "BB", "LC", "VC", "GD", "AG", "DM", "KN",
    "MV", "MO", "MU", "SC", "CV", "ST", "KM",
}


@dataclass
class SourceAirport:
    name: str
    latitude: float
    longitude: float
    csv_type: str
    iso_country: str
    iata_code: str
    scheduled_service: bool
    synthetic: bool = False


@dataclass
class UnityAirport:
    airportName: str
    x: float
    y: float
    continent: str
    country: str
    CountryPriceMultiplier: float
    PassMultiplier: float
    airportType: int


# ---------------------------------------------------------------------
# Budgeting
# ---------------------------------------------------------------------

def compute_global_budgets() -> tuple[dict[str, int], dict[str, int]]:
    active_countries = {
        iso: config
        for iso, config in COUNTRY_CONFIG.items()
        if iso not in EXCLUDED_ISOS
    }

    total_weight = sum(config.tier2_weight for config in active_countries.values())
    tier1_to_tier2_ratio = TARGET_T1 / TARGET_T2

    tier2_budget: dict[str, int] = {}

    for iso, config in active_countries.items():
        minimum_t2 = PROFILE_MINIMUMS[config.profile][0]
        proportional_share = TARGET_T2 * config.tier2_weight / total_weight
        tier2_budget[iso] = max(minimum_t2, round(proportional_share))

    adjust_budget_to_target(
        tier2_budget,
        TARGET_T2,
        {
            iso: PROFILE_MINIMUMS[config.profile][0]
            for iso, config in active_countries.items()
        }
    )

    tier1_budget: dict[str, int] = {}

    for iso, config in active_countries.items():
        minimum_t1 = PROFILE_MINIMUMS[config.profile][1]
        tier1_budget[iso] = max(
            minimum_t1,
            round(tier2_budget[iso] * tier1_to_tier2_ratio)
        )

    adjust_budget_to_target(
        tier1_budget,
        TARGET_T1,
        {
            iso: PROFILE_MINIMUMS[config.profile][1]
            for iso, config in active_countries.items()
        }
    )

    return tier2_budget, tier1_budget


def adjust_budget_to_target(
    budget: dict[str, int],
    target: int,
    minimums: dict[str, int]
) -> None:
    diff = target - sum(budget.values())

    if diff == 0:
        return

    ordered_isos = sorted(
        budget.keys(),
        key=lambda iso: budget[iso],
        reverse=diff < 0
    )

    step = 1 if diff > 0 else -1
    cursor = 0

    while diff != 0:
        iso = ordered_isos[cursor % len(ordered_isos)]

        if step < 0 and budget[iso] <= minimums.get(iso, 1):
            cursor += 1
            continue

        budget[iso] += step
        diff -= step
        cursor += 1

        if cursor > len(ordered_isos) * 500:
            break


# ---------------------------------------------------------------------
# Input loading
# ---------------------------------------------------------------------

def load_airports_csv(path: Path) -> list[SourceAirport]:
    airports: list[SourceAirport] = []

    with path.open(newline="", encoding="utf-8") as file:
        reader = csv.DictReader(file)

        for row in reader:
            airport_type = row.get("type", "").strip()

            if airport_type not in ("large_airport", "medium_airport", "small_airport"):
                continue

            iso = row.get("iso_country", "").strip().upper()

            if iso in EXCLUDED_ISOS or iso not in COUNTRY_CONFIG:
                continue

            iata_code = row.get("iata_code", "").strip()
            scheduled = row.get("scheduled_service", "").strip().lower() == "yes"

            # Small airports without IATA codes create too much noise for gameplay.
            if airport_type == "small_airport" and not iata_code:
                continue

            try:
                latitude = float(row["latitude_deg"])
                longitude = float(row["longitude_deg"])
            except (KeyError, ValueError):
                continue

            airports.append(
                SourceAirport(
                    name=row.get("name", "").strip(),
                    latitude=latitude,
                    longitude=longitude,
                    csv_type=airport_type,
                    iso_country=iso,
                    iata_code=iata_code,
                    scheduled_service=scheduled
                )
            )

    return airports


def load_country_polygons(path: Path) -> dict[str, list[tuple[float, float]]]:
    with path.open(encoding="utf-8") as file:
        geojson = json.load(file)

    polygons: dict[str, list[tuple[float, float]]] = {}

    for feature in geojson.get("features", []):
        properties = feature.get("properties", {})
        iso = (
            properties.get("ISO_A2") or
            properties.get("iso_a2") or
            ""
        ).strip().upper()

        if iso not in COUNTRY_CONFIG:
            continue

        geometry = feature.get("geometry")
        if not geometry:
            continue

        geometry_type = geometry.get("type")

        if geometry_type == "Polygon":
            ring = geometry.get("coordinates", [[]])[0]
        elif geometry_type == "MultiPolygon":
            multipolygon = geometry.get("coordinates", [])
            ring = max(multipolygon, key=lambda polygon: len(polygon[0]))[0]
        else:
            continue

        points = [
            (coordinate[0], coordinate[1])
            for coordinate in ring
            if len(coordinate) >= 2
        ]

        if points:
            unity_country_name = COUNTRY_CONFIG[iso].unity_name
            polygons[unity_country_name] = points

    return polygons


# ---------------------------------------------------------------------
# Synthetic fallback generation
# ---------------------------------------------------------------------

def point_in_polygon(
    x: float,
    y: float,
    polygon: list[tuple[float, float]]
) -> bool:
    inside = False
    previous_index = len(polygon) - 1

    for index in range(len(polygon)):
        xi, yi = polygon[index]
        xj, yj = polygon[previous_index]

        intersects = (yi > y) != (yj > y)

        if intersects:
            boundary_x = (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi

            if x < boundary_x:
                inside = not inside

        previous_index = index

    return inside


def random_point_in_polygon(
    polygon: list[tuple[float, float]],
    max_attempts: int = 300
) -> tuple[float, float]:
    longitudes = [point[0] for point in polygon]
    latitudes = [point[1] for point in polygon]

    min_lon, max_lon = min(longitudes), max(longitudes)
    min_lat, max_lat = min(latitudes), max(latitudes)

    for _ in range(max_attempts):
        lon = random.uniform(min_lon, max_lon)
        lat = random.uniform(min_lat, max_lat)

        if point_in_polygon(lon, lat, polygon):
            return round(lon, 4), round(lat, 4)

    return (
        round((min_lon + max_lon) / 2, 4),
        round((min_lat + max_lat) / 2, 4)
    )


def generate_synthetic_airports(
    iso: str,
    count: int,
    polygons: dict[str, list[tuple[float, float]]]
) -> list[SourceAirport]:
    country_name = COUNTRY_CONFIG[iso].unity_name
    polygon = polygons.get(country_name)

    if not polygon:
        return []

    generated: list[SourceAirport] = []

    for index in range(count):
        longitude, latitude = random_point_in_polygon(polygon)

        generated.append(
            SourceAirport(
                name=f"{country_name} Regional Airport {index + 1}",
                latitude=latitude,
                longitude=longitude,
                csv_type="small_airport",
                iso_country=iso,
                iata_code="",
                scheduled_service=False,
                synthetic=True
            )
        )

    return generated


# ---------------------------------------------------------------------
# Ranking and output conversion
# ---------------------------------------------------------------------

def quality_score(airport: SourceAirport) -> int:
    type_score = {
        "large_airport": 300,
        "medium_airport": 200,
        "small_airport": 100,
    }

    return (
        type_score.get(airport.csv_type, 0) +
        (50 if airport.scheduled_service else 0) +
        (20 if airport.iata_code else 0) +
        (0 if airport.synthetic else 5)
    )


def build_unity_airports(
    airports_by_country: dict[str, list[SourceAirport]],
    polygons: dict[str, list[tuple[float, float]]],
    tier2_budget: dict[str, int],
    tier1_budget: dict[str, int]
) -> list[UnityAirport]:
    unity_airports: list[UnityAirport] = []

    for iso, source_airports in sorted(airports_by_country.items()):
        if iso not in COUNTRY_CONFIG:
            continue

        config = COUNTRY_CONFIG[iso]
        minimum_t2, minimum_t1, minimum_t0 = PROFILE_MINIMUMS[config.profile]

        tier2_count = tier2_budget.get(iso, minimum_t2)
        tier1_count = tier1_budget.get(iso, minimum_t1)

        minimum_total = tier2_count + tier1_count + minimum_t0

        ranked_airports = sorted(
            source_airports,
            key=quality_score,
            reverse=True
        )

        if len(ranked_airports) < minimum_total:
            deficit = minimum_total - len(ranked_airports)
            ranked_airports.extend(
                generate_synthetic_airports(iso, deficit, polygons)
            )

        for index, source in enumerate(ranked_airports):
            if index < tier2_count:
                airport_type = 2
            elif index < tier2_count + tier1_count:
                airport_type = 1
            else:
                airport_type = 0

            unity_airports.append(
                UnityAirport(
                    airportName=source.name,
                    x=round(source.longitude, 4),
                    y=round(source.latitude, 4),
                    continent=config.continent,
                    country=config.unity_name,
                    CountryPriceMultiplier=config.country_price_multiplier,
                    PassMultiplier=config.passenger_multiplier,
                    airportType=airport_type
                )
            )

    return unity_airports


def scale_regionals_to_target(
    airports: list[UnityAirport],
    target_total: int
) -> list[UnityAirport]:
    protected_airports = [
        airport
        for airport in airports
        if airport.airportType != 0
    ]

    regional_airports = [
        airport
        for airport in airports
        if airport.airportType == 0
    ]

    regional_slots = target_total - len(protected_airports)

    if regional_slots <= 0:
        return protected_airports

    if len(regional_airports) <= regional_slots:
        return airports

    keep_ratio = regional_slots / len(regional_airports)

    regionals_by_country: dict[str, list[UnityAirport]] = defaultdict(list)

    for airport in regional_airports:
        regionals_by_country[airport.country].append(airport)

    quota_by_country = {
        country: max(1, round(len(country_airports) * keep_ratio))
        for country, country_airports in regionals_by_country.items()
    }

    adjust_budget_to_target(
        quota_by_country,
        regional_slots,
        {country: 1 for country in quota_by_country}
    )

    used_by_country: dict[str, int] = defaultdict(int)
    kept_regionals: list[UnityAirport] = []

    for airport in regional_airports:
        quota = quota_by_country.get(airport.country, 1)

        if used_by_country[airport.country] >= quota:
            continue

        kept_regionals.append(airport)
        used_by_country[airport.country] += 1

    return protected_airports + kept_regionals


def filter_min_distance(
    airports: list[UnityAirport],
    min_degrees: float
) -> list[UnityAirport]:
    filtered: list[UnityAirport] = []

    # Higher tier airports are processed first so hubs survive the filter.
    for airport in sorted(airports, key=lambda item: -item.airportType):
        too_close = False

        for kept in filtered:
            if airport.country != kept.country:
                continue

            dx = airport.x - kept.x
            dy = airport.y - kept.y

            if math.sqrt(dx * dx + dy * dy) < min_degrees:
                too_close = True
                break

        if not too_close:
            filtered.append(airport)

    return filtered


# ---------------------------------------------------------------------
# CLI pipeline
# ---------------------------------------------------------------------

def write_airports_json(
    airports: list[UnityAirport],
    output_path: Path,
    pretty: bool
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    payload = [asdict(airport) for airport in airports]

    with output_path.open("w", encoding="utf-8") as file:
        json.dump(
            payload,
            file,
            ensure_ascii=False,
            indent=2 if pretty else None,
            separators=None if pretty else (",", ":")
        )


def print_report(airports: list[UnityAirport]) -> None:
    tier_counts = Counter(airport.airportType for airport in airports)
    continent_counts = Counter(airport.continent for airport in airports)

    print("Airport dataset generated")
    print("-------------------------")
    print(f"Total airports       : {len(airports)}")
    print(f"International / T2   : {tier_counts[2]}")
    print(f"Capital / T1         : {tier_counts[1]}")
    print(f"Regional / T0        : {tier_counts[0]}")
    print(f"By continent         : {dict(sorted(continent_counts.items()))}")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Generate Unity-ready airports.json from airport CSV and country GeoJSON."
    )

    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--geojson", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)

    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--min-distance", type=float, default=0.3)
    parser.add_argument("--pretty", action="store_true")

    args = parser.parse_args()

    random.seed(args.seed)

    tier2_budget, tier1_budget = compute_global_budgets()
    country_polygons = load_country_polygons(args.geojson)
    source_airports = load_airports_csv(args.input)

    airports_by_country: dict[str, list[SourceAirport]] = defaultdict(list)

    for airport in source_airports:
        airports_by_country[airport.iso_country].append(airport)

    for iso in COUNTRY_CONFIG:
        if iso not in EXCLUDED_ISOS:
            airports_by_country.setdefault(iso, [])

    unity_airports = build_unity_airports(
        airports_by_country,
        country_polygons,
        tier2_budget,
        tier1_budget
    )

    unity_airports = scale_regionals_to_target(
        unity_airports,
        TARGET_TOTAL
    )

    unity_airports = filter_min_distance(
        unity_airports,
        min_degrees=args.min_distance
    )

    write_airports_json(
        unity_airports,
        args.output,
        pretty=args.pretty
    )

    print_report(unity_airports)


if __name__ == "__main__":
    main()
