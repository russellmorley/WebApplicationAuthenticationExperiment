using Microsoft.AspNetCore.Authentication;

namespace WebApplicationAuthenticationExperiment.Subscriber
{
    public interface ISubscriberAuthenticationHandler
    {
        Task<AuthenticateResult> HandleAuthenticateAsync(AuthenticateResult result);
    }
}
