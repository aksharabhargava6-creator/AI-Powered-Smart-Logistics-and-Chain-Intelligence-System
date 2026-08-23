"""
FR-07: AI Demand Forecasting Engine
Trains a lightweight model per product from historical order data and
produces a demand forecast + replenishment flag.

Swap MIN_DATA_POINTS / model choice as real historical data volume becomes
known. Kept dependency-light (sklearn) with an XGBoost path noted for later.
"""

from dataclasses import dataclass
from datetime import date, timedelta
from typing import List, Optional

import numpy as np
import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_absolute_error

MIN_DATA_POINTS = 10  # minimum historical periods required to forecast


class InsufficientDataError(ValueError):
    """Raised when a product does not have enough historical data to forecast."""


@dataclass
class ForecastResult:
    product_id: str
    horizon_days: int
    predicted_demand: float
    confidence_mae: Optional[float]
    generated_at: date


@dataclass
class ReplenishmentFlag:
    product_id: str
    current_stock: float
    predicted_demand: float
    threshold_breached: bool


def _build_features(history: pd.DataFrame) -> pd.DataFrame:
    """
    history: DataFrame with columns ['order_date', 'quantity'], one row per
    historical order period (e.g. daily or weekly aggregated demand).
    """
    df = history.sort_values("order_date").reset_index(drop=True)
    df["t"] = np.arange(len(df))  # simple time index as the model feature
    df["rolling_avg_3"] = df["quantity"].rolling(window=3, min_periods=1).mean()
    return df


def forecast_demand(
    product_id: str,
    history: pd.DataFrame,
    horizon_days: int = 7,
) -> ForecastResult:
    """
    FR-07: Predicts demand for `product_id` over the next `horizon_days`,
    using historical order quantities. Returns an "insufficient data" error
    rather than a low-confidence guess when history is too short.
    """
    if history is None or len(history) < MIN_DATA_POINTS:
        raise InsufficientDataError(
            f"Product {product_id} has fewer than {MIN_DATA_POINTS} historical "
            f"data points; cannot generate a reliable forecast."
        )

    df = _build_features(history)

    X = df[["t", "rolling_avg_3"]]
    y = df["quantity"]

    # holdout split for a basic confidence/error indicator
    split = max(1, int(len(df) * 0.8))
    X_train, X_test = X.iloc[:split], X.iloc[split:]
    y_train, y_test = y.iloc[:split], y.iloc[split:]

    model = LinearRegression()  # baseline; swap for XGBoost as data volume grows
    model.fit(X_train, y_train)

    confidence_mae = None
    if len(X_test) > 0:
        preds = model.predict(X_test)
        confidence_mae = float(mean_absolute_error(y_test, preds))

    # refit on full history, then project forward `horizon_days`
    model.fit(X, y)
    future_t = np.arange(len(df), len(df) + horizon_days)
    future_rolling = np.full(horizon_days, df["rolling_avg_3"].iloc[-1])
    future_X = pd.DataFrame({"t": future_t, "rolling_avg_3": future_rolling})
    predicted_demand = float(np.clip(model.predict(future_X).sum(), 0, None))

    return ForecastResult(
        product_id=product_id,
        horizon_days=horizon_days,
        predicted_demand=round(predicted_demand, 2),
        confidence_mae=round(confidence_mae, 2) if confidence_mae is not None else None,
        generated_at=date.today(),
    )


def check_replenishment(
    product_id: str,
    current_stock: float,
    predicted_demand: float,
    safety_stock_threshold: float,
) -> ReplenishmentFlag:
    """
    FR-07: Flags a product for replenishment if predicted demand would push
    stock below the configured safety threshold.
    """
    projected_remaining = current_stock - predicted_demand
    breached = projected_remaining < safety_stock_threshold

    return ReplenishmentFlag(
        product_id=product_id,
        current_stock=current_stock,
        predicted_demand=predicted_demand,
        threshold_breached=breached,
    )
