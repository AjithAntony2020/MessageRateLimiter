using Microsoft.AspNetCore.SignalR;

namespace MessageRateLimiter.Models
{
    public class LiveUpdateHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
