using System;
using AzureLab3;
using AzureLab3.Data;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace AzureLab3;

public class Startup : FunctionsStartup
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(Environment.CurrentDirectory)
        .AddJsonFile("appsettings.json", true)
        .AddEnvironmentVariables()
        .Build();

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddScoped<MongoDBContext>(provider =>
            new(Configuration.GetConnectionString("MongoDB"), "Cookbook","Recipes"));
    }
}