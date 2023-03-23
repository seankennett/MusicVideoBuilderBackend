using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using BuilderFunction;
using Microsoft.Extensions.Azure;

[assembly: FunctionsStartup(typeof(Startup))]
namespace BuilderFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            builder.Services.AddLogging();
            builder.Services.AddOptions<Connections>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                    settings.MaxConcurrentActivityFunctions = int.Parse(configuration["AzureFunctionsJobHost:extensions:durableTask:maxConcurrentActivityFunctions"]);
                    settings.FunctionTimeOut = TimeSpan.Parse(configuration["AzureFunctionsJobHost:functionTimeout"]);
                });
            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(new DefaultAzureCredential());
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PrivateBlobStorageUrl"])).WithName("PrivateBlobServiceClient");
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PublicBlobStorageUrl"])).WithName("PublicBlobServiceClient");
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var config = builder.ConfigurationBuilder.Build();
            var keyVaultEndpoint = config["AzureKeyVaultEndpoint"];

            builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential())
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

        }
    }
}

