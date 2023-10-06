using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using BuilderFunction;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;

[assembly: FunctionsStartup(typeof(Startup))]
namespace BuilderFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            builder.Services.AddLogging();
            builder.Services.AddOptions<BuilderConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                    settings.MaxConcurrentActivityFunctions = int.Parse(configuration["AzureFunctionsJobHost:extensions:durableTask:maxConcurrentActivityFunctions"]);
                    settings.FunctionTimeOut = TimeSpan.Parse(configuration["AzureFunctionsJobHost:functionTimeout"]);
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
            builder.Services.AddHttpClient(BuilderConstants.WatermarkFileName, httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://cdn.musicvideobuilder.com/custom-pages/");
            });
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

