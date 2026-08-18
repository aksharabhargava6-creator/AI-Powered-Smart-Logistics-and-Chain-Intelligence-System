using Microsoft.AspNetCore.SignalR;

namespace SmartLogisticsApp.Hubs;

public class TrackingHub : Hub
{
    public async Task SendLocationUpdate(long deliveryId, double lat, double lng, double speed)
    {
        await Clients.All.SendAsync("ReceiveLocationUpdate", deliveryId, lat, lng, speed, DateTime.Now.ToString("T"));
    }
}