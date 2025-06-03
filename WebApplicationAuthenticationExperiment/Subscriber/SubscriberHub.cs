using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebApplicationAuthenticationExperiment.Subscriber
{
    public class SubscriberHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated ?? false)
            {
                var user = Context!.User;
                var email = user.FindFirst(ClaimTypes.Email)?.Value;

                //NameIdentifier = (was "id", now) "sub" (sub is for the latest version of oauth, I believe) (https://github.com/dotnet/aspnetcore/blob/aa0ae536e89b8b8e2c2ecb1cb336451cf45387e5/src/Security/Authentication/Google/src/GoogleOptions.cs#L30)
                // "sub" is the unique identifier for the user. (https://www.cerberauth.com/blog/oauth2-access-token-claims/)
                // In terms of this application, the NameIdentifier claim is therefore the 'external unique id' for the user, as follows:
                var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            }
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User?.Identity?.IsAuthenticated ?? false)
            {
                var user = Context!.User;
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SignalR Users");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
