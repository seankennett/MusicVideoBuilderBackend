using Azure.Identity;
using Azure.Storage.Queues;
using BuildInstructorFunction;
using BuildInstructorFunction.Services;
using DataAccessLayer.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
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
            var configuration = builder.GetContext().Configuration;

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
                clientBuilder.AddQueueServiceClient(new Uri(configuration["PrivateQueueStorageUrl"])).ConfigureOptions(queueClientOptions =>
                {
                    queueClientOptions.MessageEncoding = QueueMessageEncoding.Base64;

                });
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

