using Azure.Identity;
using BuildInstructorFunction;
using BuildInstructorFunction.Services;
using DataAccessLayer.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace BuildInstructorFunction
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

            builder.Services.AddSingleton<IBuildService, BuildService>();
            builder.Services.AddSingleton<IVideoRepository, VideoRepository>();
            builder.Services.AddSingleton<IBuildRepository, BuildRepository>();
            builder.Services.AddSingleton<IUserLayerRepository, UserLayerRepository>();
            builder.Services.AddSingleton<IFfmpegComplexOperations, FfmpegComplexOperations>();
            builder.Services.AddSingleton<IFfmpegService, FfmpegService>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<IAzureBatchService, AzureBatchService>();
            builder.Services.AddSingleton<IBuilderFunctionSender, BuilderFunctionSender>();
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

