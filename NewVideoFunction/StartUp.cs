using Azure.Identity;
using DataAccessLayer.Repositories;
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
            builder.Services.AddLogging();
            
            builder.Services.AddLogging();
            builder.Services.AddOptions<Connections>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(new DefaultAzureCredential());
                clientBuilder.AddBlobServiceClient(new Uri(configuration["PrivateBlobStorageUrl"]));
            });

            builder.Services.AddSingleton<IBuildRepository, BuildRepository>();
            builder.Services.AddSingleton<IMailer, Mailer>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<IChargeService, ChargeService>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var config = builder.ConfigurationBuilder.Build();
            var keyVaultEndpoint = config["AzureKeyVaultEndpoint"];

            config = builder.ConfigurationBuilder
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential())
                        .AddJsonFile("local.settings.json", true)
                        .AddEnvironmentVariables()
                    .Build();

            StripeConfiguration.ApiKey = config["StripeSecretKey"];
        }
    }
}

