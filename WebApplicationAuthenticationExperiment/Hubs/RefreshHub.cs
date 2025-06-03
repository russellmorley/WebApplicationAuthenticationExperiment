using Microsoft.AspNetCore.SignalR;
using WebApplicationAuthenticationExperiment.Subscriber;

namespace WebApplicationAuthenticationExperiment.Hubs
{
    public class RefreshHub : SubscriberHub
    {
        public async Task RefreshAllConnectedClients(string path)//string user, string message)
        {
            await Clients.All.SendAsync("RefreshMessage", path);//user, message);
        }
    }
}
