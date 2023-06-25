using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using TwitchStreamsVkNotifications;
using TwitchStreamsVkNotifications.Routes;
using TwitchStreamsVkNotifications.Work;

var builder = WebApplication.CreateBuilder(args);

{
    builder.Services.Configure<KestrelServerOptions>(serverOptions =>
    {
        serverOptions.ConfigureHttpsDefaults(options =>
        {
            string? certBasePath = builder.Configuration.GetConnectionString("CertPath") ?? throw new Exception("Нет пути до сертификатов.");

            string certPath = Path.Combine(certBasePath, "fullchain.pem");
            string keyPath = Path.Combine(certBasePath, "privkey.pem");

            if (!File.Exists(certPath) || !File.Exists(keyPath))
                throw new Exception("Нет сертификатов.");

            options.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        });
    });
}

var configBuilder = new ConfigurationBuilder();
var config = configBuilder.AddJsonFile("options.json", optional: false, reloadOnChange: true).Build();

builder.Services.AddOptions<MyOptions>()
.Bind(config)
.ValidateDataAnnotations()
.ValidateOnStart();

builder.Services.AddScoped<VkPoster>();
builder.Services.AddSingleton<TwitchChecker>();

var app = builder.Build();

app.MapGet("/", MainRoute.GetAsync);
app.MapGet("/redirect", RedirectRoute.GetAsync);
app.MapPost("/redirect", RedirectRoute.PostAsync);

using (var scope = app.Services.CreateScope())
{
    var checker = scope.ServiceProvider.GetRequiredService<TwitchChecker>();
    checker.Init();

    if (args.Contains("--test"))
    {
        var vk = scope.ServiceProvider.GetRequiredService<VkPoster>();
        await vk.PostAsync();
    }
}

app.Run();
