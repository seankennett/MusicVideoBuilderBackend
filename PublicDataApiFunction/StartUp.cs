using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using PublicDataApiFunction;
using PublicDataApiFunction.Repositories;
using Stripe;

[assembly: FunctionsStartup(typeof(Startup))]
namespace PublicDataApiFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<SqlConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });

            builder.Services.AddSingleton<ICollectionRepository, CollectionRepository>();
            builder.Services.AddSingleton<IDirectionRepository, DirectionRepository>();

            builder.Services.AddMemoryCache();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var configuration = builder.ConfigurationBuilder.Build();

            var keyVaultEndpoint = configuration["AzureKeyVaultEndpoint"];

            configuration = builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential())
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

            StripeConfiguration.ApiKey = configuration["StripeSecretKey"];
        }
    }
}

