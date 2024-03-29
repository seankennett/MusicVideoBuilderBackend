using Azure.Identity;
using Azure.Storage.Queues;
using BuildDataAccess.Repositories;
using CollectionDataAccess.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;
using SpaWebApi;
using SpaWebApi.Services;
using UserSubscriptionAccess.Repositories;
using VideoDataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

if (!builder.Environment.IsDevelopment())
{
    configuration.AddAzureKeyVault(new Uri(configuration["AzureKeyVaultEndpoint"]), new DefaultAzureCredential());
}

// Add services to the container.

// Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(options =>
        {
            configuration.Bind("AzureAdB2C", options);

            options.TokenValidationParameters.NameClaimType = "name";
        },
options => { configuration.Bind("AzureAdB2C", options); });

builder.Services.AddAzureClients(clientBuilder =>
{
    
    clientBuilder.UseCredential(new DefaultAzureCredential());
    clientBuilder.AddBlobServiceClient(new Uri(configuration["PrivateBlobStorageUrl"]));
    clientBuilder.AddQueueServiceClient(new Uri(configuration["PrivateQueueStorageUrl"])).ConfigureOptions(queueClientOptions =>
    {
        queueClientOptions.MessageEncoding = QueueMessageEncoding.Base64;
        
    });
});

builder.Services.AddOptions<SqlConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
builder.Services.AddOptions<SpaWebApiConfig>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
builder.Services.AddHttpClient("PublicApi", client =>
{
    client.BaseAddress = new Uri(configuration["PublicApi"]);
});
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICollectionService, CollectionService>();
builder.Services.AddSingleton<IUserCollectionRepository, UserCollectionRepository>();
builder.Services.AddSingleton<IClipRepository, ClipRepository>();
builder.Services.AddSingleton<IVideoRepository, VideoRepository>();
builder.Services.AddSingleton<IBuildService, BuildService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IClipService, ClipService>();
builder.Services.AddSingleton<IVideoService, VideoService>();
builder.Services.AddSingleton<IBuildRepository, BuildRepository>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();
builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddSingleton<ISubscriptionProductService, SubscriptionProductService>();

builder.Services.AddControllers(options =>
{
    options.OutputFormatters.RemoveType<StringOutputFormatter>();
});

var app = builder.Build();

Stripe.StripeConfiguration.ApiKey = builder.Configuration["StripeSecretKey"];

if (app.Environment.IsDevelopment())
{
    // Configure the HTTP request pipeline.
    app.UseCors(cors =>
    {
        cors.AllowAnyOrigin();
        cors.AllowAnyHeader();
        cors.AllowAnyMethod();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
