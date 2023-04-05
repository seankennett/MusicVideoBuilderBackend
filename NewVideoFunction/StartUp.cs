using Azure.Identity;
using BuildDataAccess.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewVideoFunction;
using NewVideoFunction.Interfaces;
using Stripe;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace NewVideoFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;
            var managedClientId = configuration["ManagedIdentityClientId"];
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedClientId
            };

            builder.Services.AddLogging();
            
            builder.Services.AddLogging();
            builder.Services.AddOptions<NewVideoConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
            builder.Services.AddOptions<SqlConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(new DefaultAzureCredential(defaultAzureCredentialOptions));
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PrivateBlobStorageUrl"]));
            });
            builder.Services.AddSingleton(new ChainedTokenCredential(
                new ManagedIdentityCredential(managedClientId),
                new EnvironmentCredential()));

            builder.Services.AddSingleton<IBuildRepository, BuildRepository>();
            builder.Services.AddSingleton<IUserLayerRepository, UserLayerRepository>();
            builder.Services.AddSingleton<IMailer, Mailer>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<IChargeService, ChargeService>();            
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var configuration = builder.ConfigurationBuilder.Build();
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = configuration["ManagedIdentityClientId"]
            };

            var keyVaultEndpoint = configuration["AzureKeyVaultEndpoint"];

            configuration = builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential(defaultAzureCredentialOptions))
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

            StripeConfiguration.ApiKey = configuration["StripeSecretKey"];
        }
    }
}

