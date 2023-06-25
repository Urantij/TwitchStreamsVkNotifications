using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace TwitchStreamsVkNotifications.Routes;

/// <summary>
/// Обработка подключений к /
/// </summary>
public class MainRoute
{
    public static string? LastState { get; private set; } = null;

    public static IResult GetAsync(IOptions<MyOptions> options, ILogger<MainRoute> logger)
    {
        logger.LogInformation("Получили.");

        if (options.Value == null)
            return TypedResults.Ok("Сервер не настроен.");

        if (options.Value.Auth == null)
        {
            LastState = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, int.MaxValue).ToString();

            var param = new Dictionary<string, string?>()
            {
                { "client_id", options.Value.VkClientId.ToString() },
                { "redirect_uri", options.Value.VkRedirectUri.ToString() },
                { "scope", "wall offline" },
                { "response_type", "token" },
                { "state", LastState.ToString() },
            };

            var redirectUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("https://oauth.vk.com/authorize", param);

            return TypedResults.Redirect(redirectUrl);
        }

        string expireText;
        if (options.Value.Auth.ExpiresIn.TotalSeconds == 0)
        {
            expireText = "Время неограничено.";
        }
        else
        {
            DateTime endOfTheLife = DateTime.UtcNow + options.Value.Auth.ExpiresIn;

            expireText = $"Всё закончится {endOfTheLife:dd:MM:yyyy HH:mm:ss} (UTC)";
        }

        return TypedResults.Content(
            $$"""
            <!DOCTYPE html>
            <html>

            <head>
                <title>я юра</title>
                <script>
                    function reset()
                    {
                        fetch("{{new Uri(options.Value.ServerUrl, "setup")}}",
                            {
                                method: "DELETE"
                            })
                            .then(response => {
                                window.location.href = "{{options.Value.ServerUrl}}";
                            })
                            .catch(reason => {
                                console.log(reason);
                            });
                    }
                </script>
            </head>

            <body>
                Всё есть как есть. {{expireText}}<br/>
                <button onclick="reset()">Сбросить</button>
            </body>

            </html>
            """, contentType: "text/html", Encoding.UTF8);
    }
}
