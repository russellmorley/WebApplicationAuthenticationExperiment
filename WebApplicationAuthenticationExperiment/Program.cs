using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using WebApplicationAuthenticationExperiment.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.Cookies;

List<RefreshToken> refreshTokens = new List<RefreshToken>();


var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

IConfigurationSection jwtSection = config.GetSection("Authentication:Jwt");
var issuer = jwtSection["issuer"] ?? throw new ApplicationException("Authentication.Jwt.issuer not set");
var audience = jwtSection["audience"] ?? throw new ApplicationException("Authentication.Jwt.audience not set");
var secretKey = jwtSection["secretKey"] ?? throw new ApplicationException("Authentication.Jwt.secretKey not set");

IConfigurationSection googleAuthNSection = config.GetSection("Authentication:Google");
var clientId = googleAuthNSection["clientId"];
var clientSecret = googleAuthNSection["clientSecret"];

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // Set your valid issuer and audience here
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        // For signalr, from https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0#built-in-jwt-authentication
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/refresh")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };

})
//AddGoogle doesn't work without adding cookies (or another valid signon scheme)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddGoogle(options =>
{
    IConfigurationSection googleAuthNSection = config.GetSection("Authentication:Google");
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    // when cookies are set, CallbackPath cannot be set.
    // HOWEVER, for this example BOTH of the following must be added toAuthorized redirect URIs in 
    // Google:
    //   https://localhost:7080/api/google-response
    //   https://localhost:7080/signin-google
    // with the latter apparently something hard coded in aspnetcore's google authentication implementation.
    //options.CallbackPath = "/api/google-response";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors("AllowAll");


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// for some reason neither of these now need to be set, presumably they're set by other middleware.
//app.UseAuthentication();
//app.UseAuthorization();

app.MapHub<RefreshHub>("/hubs/refresh");

var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weatherforecast", (HttpContext httpContext) =>
{
    //httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireAuthorization()
;

app.MapGet("/api/send", async ( string? path, IHubContext<RefreshHub> hubContext) =>
{
    // example getting the hubcontext: it's available through di: var hubContext = context.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
    if (!string.IsNullOrEmpty(path))
    {
        await hubContext.Clients.All.SendAsync("RefreshMessage", path);
        return Results.Ok("Message sent.");
    }
    else
    {
        return Results.BadRequest("path query parameter is required.");
    }
})
.WithName("GetSend")
.WithOpenApi()
//.RequireAuthorization()
;





//AUTHENTICATION Endpoints

app.MapGet("/api/google-login", async (HttpContext httpContext) =>
{
    var redirectUrl = "/api/google-response"; // Set your redirect URL
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    await httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
});

app.MapGet("/api/google-response", (HttpContext httpContext) =>
{
    var queryString = "authenticate=true";
    return Results.Redirect($"https://localhost:5174/?{queryString}");
});

app.MapGet("/api/get-tokens-from-google-code", async (HttpContext httpContext) =>
{
    var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    // Extract user information from the authenticated principal
    var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value as string; // Google ID
    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value as string; // User email

    if (string.IsNullOrEmpty(userId))
    {
        return Results.BadRequest("User ID not found.");
    }

    if (string.IsNullOrEmpty(email))
    {
        return Results.BadRequest("Email not found.");
    }

    var jwtSecurityToken = JwtSecurityTokenFactory.Create(userId, email, secretKey, issuer, audience);

    // Generate a refresh token
    var refreshToken = new RefreshToken
    {
        Token = Guid.NewGuid().ToString(),
        Expiration = DateTime.UtcNow.AddDays(7), // Set refresh token expiration (e.g., 7 days)
        UserId = userId,
        Email = email
    };

    // Store the refresh token
    refreshTokens.Add(refreshToken);

    var jwtSecurityTokenString = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken); // Generate the token

    //httpContext.Response.Cookies.Append("jwtSecurityToken", jwtSecurityTokenString, new CookieOptions
    //{
    //    HttpOnly = true,
    //    Secure = true, // Ensure this is true when using HTTPS
    //    MaxAge = TimeSpan.FromHours(1),
    //    Path = "/"
    //});

    return Results.Ok(new
    {
        jwtSecurityTokenString,
        refreshToken
    });
});

app.MapPost("/refresh-token", (string refreshToken) =>
{
    var storedToken = refreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
    if (storedToken == null || storedToken.Expiration < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var newAccessToken = JwtSecurityTokenFactory.Create(storedToken.UserId, storedToken.Email, secretKey, issuer, audience);

    return Results.Ok(new
    {
        jwtSecurityTokenString = new JwtSecurityTokenHandler().WriteToken(newAccessToken)
    });
});

app.Run();

public static class JwtSecurityTokenFactory
{
    public static JwtSecurityToken Create(string userId, string email, string secretKey, string issuer, string audience)
    {
        // Create claims for the JWT
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)); // Your signing key
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1), // Set token expiration
            signingCredentials: creds);
    }
}
public class RefreshToken
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }

}




record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
