using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using TwitchStreamsVkNotifications;
using TwitchStreamsVkNotifications.Work;

var builder = WebApplication.CreateBuilder(args);

{
    string? certBasePath = builder.Configuration.GetConnectionString("CertPath");

    if (certBasePath != null)
    {
        string certPath = Path.Combine(certBasePath, "fullchain.pem");
        string keyPath = Path.Combine(certBasePath, "privkey.pem");

        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            builder.Services.Configure<KestrelServerOptions>(serverOptions =>
                {
                    serverOptions.ConfigureHttpsDefaults(options =>
                    {
                        options.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                    });
                });
        }
        else
        {
            System.Console.WriteLine("Сертификат не найден.");
        }
    }
    else
    {
        System.Console.WriteLine("Сертификат не используется.");
    }
}

var configBuilder = new ConfigurationBuilder();
var config = configBuilder.AddJsonFile("options.json", optional: false, reloadOnChange: true).Build();

builder.Services.AddOptions<MyOptions>()
.Bind(config)
.ValidateDataAnnotations()
.ValidateOnStart();

builder.Services.AddScoped<VkPoster>();
builder.Services.AddSingleton<TwitchChecker>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(c =>
{
    c.TimestampFormat = "[HH:mm:ss] ";
});

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

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
