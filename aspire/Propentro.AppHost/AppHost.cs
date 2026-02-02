var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume();

var database = postgres.AddDatabase("propentro");

// Redis cache
var redis = builder.AddRedis("redis")
    .WithRedisCommander()
    .WithDataVolume();

// API project
var api = builder.AddProject<Projects.Propentro_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
