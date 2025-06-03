using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebApplicationAuthenticationExperiment.Subscriber
{
    public class SubscriberAuthenticationHandler : ISubscriberAuthenticationHandler
    {
        public Task<AuthenticateResult> HandleAuthenticateAsync(AuthenticateResult result)
        {
            if (!result.Succeeded)
            {
                return Task.FromResult(result); // Return if authentication failed
            }

            // Get the user information from the result
            var user = result.Principal;

            // Here you can extract the email or other identifiers
            var email = user.FindFirst(ClaimTypes.Email)?.Value;

            //NameIdentifier = (was "id", now) "sub" (sub is for the latest version of oauth, I believe) (https://github.com/dotnet/aspnetcore/blob/aa0ae536e89b8b8e2c2ecb1cb336451cf45387e5/src/Security/Authentication/Google/src/GoogleOptions.cs#L30)
            // "sub" is the unique identifier for the user. (https://www.cerberauth.com/blog/oauth2-access-token-claims/)
            // In terms of this application, the NameIdentifier claim is therefore the 'external unique id' for the user, as follows:
            var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            // Check the user in your database
            //var dbUser = await CheckUserInDatabase(email); // Implement this method

            //if (dbUser != null)
            //{
            // If the user exists in your database, return success
            return Task.FromResult(result); // You can modify the claims if needed
            //}

            //// If the user is not found in the database, fail the authentication
            //return AuthenticateResult.Fail("User not found in the database.");

        }
    }
}
