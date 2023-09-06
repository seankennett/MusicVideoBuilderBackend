using Azure.Identity;
using LayerDataAccess.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using UploadLayerFunction;

[assembly: FunctionsStartup(typeof(Startup))]
namespace UploadLayerFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = configuration["ManagedIdentityClientId"]
            };

            builder.Services.AddLogging();
            builder.Services.AddOptions<SqlConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(new DefaultAzureCredential(defaultAzureCredentialOptions));
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PrivateBlobStorageUrl"])).WithName("PrivateBlobServiceClient");
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PublicBlobStorageUrl"])).WithName("PublicBlobServiceClient");
            });

            builder.Services.AddSingleton<ILayerRepository, CollectionRepository>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

        }
    }
}

