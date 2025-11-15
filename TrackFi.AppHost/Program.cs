using TrackFi.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire auto-discovers HTTPS endpoint from launchSettings.json
var apiService = builder.AddProject<Projects.TrackFi_Api>("trackFiApi")
    .WithExternalHttpEndpoints()
    .WithSwaggerUI()
    .WithScalar()
    .WithRedoc();
    


builder.AddNpmApp("frontend", "../../TrackFi.Frontend", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
