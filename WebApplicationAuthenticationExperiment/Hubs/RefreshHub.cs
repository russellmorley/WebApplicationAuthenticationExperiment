using Microsoft.AspNetCore.SignalR;

namespace WebApplicationAuthenticationExperiment.Hubs
{
    public class RefreshHub : Hub
    {
        public async Task RefreshAllConnectedClients(string path)//string user, string message)
        {
            await Clients.All.SendAsync("RefreshMessage", path);//user, message);
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
