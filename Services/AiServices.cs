using Microsoft.EntityFrameworkCore;
using SmartLogisticsApp.Data;
using SmartLogisticsApp.Models;

namespace SmartLogisticsApp.Services;

public interface IAiEngineService
{
    Task<List<DemandForecast>> GenerateDemandForecastsAsync();
    DateTime CalculatePredictedEta(double currentLat, double currentLng, double destLat, double destLng, double avgSpeedKmh = 50.0);
}

public class AiEngineService : IAiEngineService
{
    private readonly SmartLogisticsContext _db;

    public AiEngineService(SmartLogisticsContext db)
    {
        _db = db;
    }

    public async Task<List<DemandForecast>> GenerateDemandForecastsAsync()
    {
        var products = await _db.Products.ToListAsync();
        var forecasts = new List<DemandForecast>();
        var random = new Random();

        foreach (var prod in products)
        {
            // Linear regression simulation / heuristic forecasting based on reorder thresholds
            decimal baseDemand = prod.ReorderQuantity > 0 ? prod.ReorderQuantity : 100;
            decimal predicted = baseDemand * (decimal)(0.85 + (random.NextDouble() * 0.4));

            var forecast = new DemandForecast
            {
                ProductId = prod.ProductId,
                ForecastDate = DateTime.Today.AddDays(7),
                PredictedDemand = Math.Round(predicted, 2),
                ConfidenceScore = Math.Round((decimal)(0.88 + (random.NextDouble() * 0.1)), 2),
                GeneratedAt = DateTime.UtcNow
            };
            forecasts.Add(forecast);
        }

        _db.DemandForecasts.AddRange(forecasts);
        await _db.SaveChangesAsync();
        return forecasts;
    }

    public DateTime CalculatePredictedEta(double currentLat, double currentLng, double destLat, double destLng, double avgSpeedKmh = 50.0)
    {
        // Haversine distance formula calculation
        double dLat = ToRadians(destLat - currentLat);
        double dLng = ToRadians(destLng - currentLng);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(currentLat)) * Math.Cos(ToRadians(destLat)) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distanceKm = 6371 * c; // Earth radius in km

        double hoursNeeded = distanceKm / avgSpeedKmh;
        return DateTime.Now.AddHours(hoursNeeded);
    }

    private static double ToRadians(double val) => (Math.PI / 180) * val;
}