
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using WebApplicationAuthenticationExperiment.Subscriber;

namespace WebApplicationAuthenticationExperiment.Google
{
    public class SubscriberGoogleAuthenticationHandler : GoogleHandler
    {
        private readonly ISubscriberAuthenticationHandler _subscriberAuthenticationHandler;

        public SubscriberGoogleAuthenticationHandler(
            ISubscriberAuthenticationHandler subscriberAuthenticationHandler,
            IOptionsMonitor<GoogleOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder) 
            : base(options, logger, encoder)
        {
            _subscriberAuthenticationHandler = subscriberAuthenticationHandler;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return await _subscriberAuthenticationHandler.HandleAuthenticateAsync(await base.HandleAuthenticateAsync());
        }
    }

    public static class SubscriberGoogleExtensions
    {
        public static AuthenticationBuilder AddSubscriberGoogle(this AuthenticationBuilder builder, Action<GoogleOptions> configureOptions)
            => builder.AddOAuth<GoogleOptions, SubscriberGoogleAuthenticationHandler>(GoogleDefaults.AuthenticationScheme, GoogleDefaults.DisplayName, configureOptions);
    }
}
