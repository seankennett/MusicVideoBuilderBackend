using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using UploadLayerFunction;
using UploadLayerFunction.Interfaces;

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
            builder.Services.AddOptions<Connections>().Configure<IConfiguration>(
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

            builder.Services.AddSingleton<IDatabaseWriter, DatabaseWriter>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var configuration = builder.ConfigurationBuilder.Build();
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = configuration["ManagedIdentityClientId"]
            };

            var keyVaultEndpoint = configuration["AzureKeyVaultEndpoint"];

            builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential(defaultAzureCredentialOptions))
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

        }
    }
}

