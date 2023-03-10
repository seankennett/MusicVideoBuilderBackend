using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewVideoFunction;
using NewVideoFunction.Interfaces;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace NewVideoFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddOptions<Connections>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });

            builder.Services.AddSingleton<IDatabaseWriter, DatabaseWriter>();
            builder.Services.AddSingleton<IMailer, Mailer>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
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

