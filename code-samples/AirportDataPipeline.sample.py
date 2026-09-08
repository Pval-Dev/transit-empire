"""Curated excerpt from Transit Empire's airport data pipeline.

The original toolchain consumed a real-world airport CSV, preserved geographic
latitude/longitude, filtered and ranked airports, grouped them by ISO country,
and exported gameplay-ready JSON for Unity.

This sample is intentionally reduced: the production script also contained the
full country configuration, global tier budgets, polygon fallback generation,
and reporting utilities.
"""

import csv
import json
import math
from collections import defaultdict

SUPPORTED_TYPES = {"large_airport", "medium_airport", "small_airport"}


def load_airports(csv_path, supported_countries, excluded_countries=None):
    excluded_countries = excluded_countries or set()
    airports = []

    with open(csv_path, newline="", encoding="utf-8") as source:
        reader = csv.DictReader(source)

        for row in reader:
            airport_type = row.get("type", "").strip()
            if airport_type not in SUPPORTED_TYPES:
                continue

            iso = row.get("iso_country", "").strip().upper()
            if iso in excluded_countries or iso not in supported_countries:
                continue

            iata = row.get("iata_code", "").strip()
            if airport_type == "small_airport" and not iata:
                continue

            try:
                latitude = float(row["latitude_deg"])
                longitude = float(row["longitude_deg"])
            except (KeyError, ValueError):
                continue

            airports.append(
                {
                    "name": row.get("name", "").strip(),
                    "latitude": latitude,
                    "longitude": longitude,
                    "source_type": airport_type,
                    "iso_country": iso,
                    "iata": iata,
                    "scheduled_service": row.get("scheduled_service", "").strip().lower()
                    == "yes",
                }
            )

    return airports


def quality_score(airport):
    type_score = {
        "large_airport": 300,
        "medium_airport": 200,
        "small_airport": 100,
    }

    return (
        type_score.get(airport["source_type"], 0)
        + (50 if airport["scheduled_service"] else 0)
        + (20 if airport["iata"] else 0)
    )


def build_gameplay_records(airports_by_country, country_config, tier_budgets):
    output = []

    for iso, country_airports in airports_by_country.items():
        config = country_config[iso]
        ordered = sorted(country_airports, key=quality_score, reverse=True)

        international_budget = tier_budgets[iso]["international"]
        capital_budget = tier_budgets[iso]["capital"]

        for index, airport in enumerate(ordered):
            if index < international_budget:
                gameplay_type = 2
            elif index < international_budget + capital_budget:
                gameplay_type = 1
            else:
                gameplay_type = 0

            output.append(
                {
                    "airportName": airport["name"],
                    # Transit Empire's 2D map uses geographic longitude/latitude
                    # directly as world-space x/y coordinates.
                    "x": round(airport["longitude"], 4),
                    "y": round(airport["latitude"], 4),
                    "continent": config["continent"],
                    "country": config["unity_name"],
                    "CountryPriceMultiplier": config["price_multiplier"],
                    "PassMultiplier": config["passenger_multiplier"],
                    "airportType": gameplay_type,
                }
            )

    return output


def filter_min_distance(airports, min_degrees=0.3):
    """Keep higher-tier airports when two locations are too close together."""
    filtered = []

    for airport in sorted(airports, key=lambda item: -item["airportType"]):
        too_close = False

        for existing in filtered:
            if airport["country"] != existing["country"]:
                continue

            dx = airport["x"] - existing["x"]
            dy = airport["y"] - existing["y"]

            if math.sqrt(dx * dx + dy * dy) < min_degrees:
                too_close = True
                break

        if not too_close:
            filtered.append(airport)

    return filtered


def export_airports_json(airports, output_path):
    with open(output_path, "w", encoding="utf-8") as target:
        json.dump(airports, target, ensure_ascii=False, separators=(",", ":"))


def group_by_country(airports):
    grouped = defaultdict(list)
    for airport in airports:
        grouped[airport["iso_country"]].append(airport)
    return grouped
