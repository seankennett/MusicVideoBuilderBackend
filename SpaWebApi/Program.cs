using Azure.Identity;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Identity.Web;
using SpaWebApi.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

if (!builder.Environment.IsDevelopment())
{
    configuration.AddAzureKeyVault(new Uri(configuration["AzureKeyVaultEndpoint"]), new DefaultAzureCredential());
}
else
{
    // silly kestral limit - azure will use iis and therefore webconfig
    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = int.MaxValue;
    });
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

// Creating policies that wraps the authorization requirements
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AuthorRolePolicy", policy =>
    {
        policy.RequireClaim("extension_IsLayerAuthor", "true");
    });
});
builder.Services.AddMemoryCache();

builder.Services.AddOptions<Connections>().Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                });
builder.Services.AddSingleton<ITagRepository, TagRepository>();
builder.Services.AddSingleton<ILayerRepository, LayerRepository>();
builder.Services.AddSingleton<IUserLayerRepository, UserLayerRepository>();
builder.Services.AddSingleton<IClipRepository, ClipRepository>();
builder.Services.AddSingleton<IVideoRepository, VideoRepository>();
builder.Services.AddSingleton<IVideoAssetService, VideoAssetService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IClipService, ClipService>();
builder.Services.AddSingleton<IVideoService, VideoService>();
builder.Services.AddSingleton<ILayerUploadService, LayerUploadService>();
builder.Services.AddSingleton<IBuildRepository, BuildRepository>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();

builder.Services.AddControllers();

var app = builder.Build();

StripeConfiguration.ApiKey = builder.Configuration["StripeSecretKey"];

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
