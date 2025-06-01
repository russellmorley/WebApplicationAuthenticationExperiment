var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.WebApplicationAuthenticationExperiment>("webapplicationauthenticationexperiment");

builder.AddNpmApp("react", "../reactui", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("BROWSER", "none")
    // the following line change is needed to fix the access port.
    .WithHttpsEndpoint(port: 5174, targetPort:5173)//env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
