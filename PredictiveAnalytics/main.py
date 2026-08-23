"""
Predictive Analytics Service (FR-07 & FR-08)
Internal FastAPI service consumed by the ASP.NET Core backend.
Run with: uvicorn main:app --reload --port 8001
"""

from datetime import datetime
from typing import Optional

import pandas as pd
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from eta import predict_eta, InvalidCoordinateError
from forecasting import forecast_demand, check_replenishment, InsufficientDataError

app = FastAPI(title="Predictive Analytics Service", version="0.1.0")


# ---------- FR-07: Demand Forecasting ----------

class OrderHistoryPoint(BaseModel):
    order_date: str  # ISO date, e.g. "2026-07-01"
    quantity: float


class ForecastRequest(BaseModel):
    product_id: str
    horizon_days: int = Field(default=7, gt=0, le=90)
    history: list[OrderHistoryPoint]


class ForecastResponse(BaseModel):
    product_id: str
    horizon_days: int
    predicted_demand: float
    confidence_mae: Optional[float]
    generated_at: str


@app.post("/api/forecast/demand", response_model=ForecastResponse)
def get_demand_forecast(req: ForecastRequest):
    history_df = pd.DataFrame([h.dict() for h in req.history])
    if not history_df.empty:
        history_df["order_date"] = pd.to_datetime(history_df["order_date"])

    try:
        result = forecast_demand(req.product_id, history_df, req.horizon_days)
    except InsufficientDataError as e:
        raise HTTPException(status_code=422, detail=str(e))

    return ForecastResponse(
        product_id=result.product_id,
        horizon_days=result.horizon_days,
        predicted_demand=result.predicted_demand,
        confidence_mae=result.confidence_mae,
        generated_at=result.generated_at.isoformat(),
    )


class ReplenishmentRequest(BaseModel):
    product_id: str
    current_stock: float
    predicted_demand: float
    safety_stock_threshold: float


class ReplenishmentResponse(BaseModel):
    product_id: str
    current_stock: float
    predicted_demand: float
    threshold_breached: bool


@app.post("/api/forecast/replenishment-flags", response_model=ReplenishmentResponse)
def get_replenishment_flag(req: ReplenishmentRequest):
    flag = check_replenishment(
        req.product_id, req.current_stock, req.predicted_demand, req.safety_stock_threshold
    )
    return ReplenishmentResponse(
        product_id=flag.product_id,
        current_stock=flag.current_stock,
        predicted_demand=flag.predicted_demand,
        threshold_breached=flag.threshold_breached,
    )


# ---------- FR-08: ETA Prediction ----------

class EtaRequest(BaseModel):
    delivery_id: Optional[str] = None
    origin_lat: float
    origin_lng: float
    dest_lat: float
    dest_lng: float
    departure_time: Optional[str] = None  # ISO datetime, defaults to now


class EtaResponse(BaseModel):
    delivery_id: Optional[str]
    distance_km: float
    predicted_eta: str
    avg_speed_kmh: float
    calculated_at: str


@app.post("/api/eta/predict", response_model=EtaResponse)
def get_eta(req: EtaRequest):
    departure = (
        datetime.fromisoformat(req.departure_time) if req.departure_time else None
    )

    try:
        result = predict_eta(
            req.origin_lat, req.origin_lng, req.dest_lat, req.dest_lng, departure
        )
    except InvalidCoordinateError as e:
        raise HTTPException(status_code=422, detail=str(e))

    return EtaResponse(
        delivery_id=req.delivery_id,
        distance_km=result.distance_km,
        predicted_eta=result.predicted_eta.isoformat(),
        avg_speed_kmh=result.avg_speed_kmh,
        calculated_at=result.calculated_at.isoformat(),
    )


@app.get("/health")
def health_check():
    return {"status": "ok"}
