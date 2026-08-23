"""
FR-08: Delivery ETA Prediction Engine
Calculates great-circle (Haversine) distance between two coordinates and
applies a traffic/speed adjustment factor to produce a realistic ETA.
"""

import math
from datetime import datetime, timedelta
from dataclasses import dataclass
from typing import Optional


EARTH_RADIUS_KM = 6371.0088


class InvalidCoordinateError(ValueError):
    """Raised when origin/destination coordinates are missing or out of range."""


@dataclass
class EtaResult:
    distance_km: float
    predicted_eta: datetime
    avg_speed_kmh: float
    calculated_at: datetime


def _validate_coords(lat: float, lng: float, label: str) -> None:
    if lat is None or lng is None:
        raise InvalidCoordinateError(f"{label} coordinates are missing.")
    if not (-90.0 <= lat <= 90.0):
        raise InvalidCoordinateError(f"{label} latitude {lat} out of range.")
    if not (-180.0 <= lng <= 180.0):
        raise InvalidCoordinateError(f"{label} longitude {lng} out of range.")


def haversine_distance_km(lat1: float, lng1: float, lat2: float, lng2: float) -> float:
    """Great-circle distance between two lat/lng points, in kilometers."""
    _validate_coords(lat1, lng1, "Origin")
    _validate_coords(lat2, lng2, "Destination")

    phi1, phi2 = math.radians(lat1), math.radians(lat2)
    d_phi = math.radians(lat2 - lat1)
    d_lambda = math.radians(lng2 - lng1)

    a = (
        math.sin(d_phi / 2) ** 2
        + math.cos(phi1) * math.cos(phi2) * math.sin(d_lambda / 2) ** 2
    )
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return EARTH_RADIUS_KM * c


def traffic_adjustment_factor(distance_km: float, hour_of_day: Optional[int] = None) -> float:
    """
    Returns an average speed (km/h) to use for the ETA, adjusted for a simple
    time-of-day traffic heuristic. Swap this out for a real traffic API later
    without changing the rest of the pipeline (see Assumptions in the
    requirement doc: real GPS/traffic data may be simulated).
    """
    base_speed_kmh = 45.0  # baseline urban delivery speed

    if hour_of_day is None:
        hour_of_day = datetime.now().hour

    # crude peak-hour penalty
    is_peak = hour_of_day in range(8, 10) or hour_of_day in range(17, 20)
    peak_penalty = 0.6 if is_peak else 1.0

    # longer hauls are more likely to include highway segments -> faster average
    distance_bonus = 1.15 if distance_km > 30 else 1.0

    return base_speed_kmh * peak_penalty * distance_bonus


def predict_eta(
    origin_lat: float,
    origin_lng: float,
    dest_lat: float,
    dest_lng: float,
    departure_time: Optional[datetime] = None,
) -> EtaResult:
    """
    FR-08: Given origin and destination coordinates, returns predicted ETA,
    distance used, and the average speed basis (for explainability).
    """
    now = departure_time or datetime.utcnow()

    distance_km = haversine_distance_km(origin_lat, origin_lng, dest_lat, dest_lng)
    avg_speed_kmh = traffic_adjustment_factor(distance_km, hour_of_day=now.hour)

    travel_hours = distance_km / avg_speed_kmh
    predicted_eta = now + timedelta(hours=travel_hours)

    return EtaResult(
        distance_km=round(distance_km, 3),
        predicted_eta=predicted_eta,
        avg_speed_kmh=round(avg_speed_kmh, 1),
        calculated_at=now,
    )
