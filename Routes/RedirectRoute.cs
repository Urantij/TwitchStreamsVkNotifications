using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace TwitchStreamsVkNotifications.Routes;

/// <summary>
/// Обработка подключений к /redirect
/// </summary>
public class RedirectRoute
{
    public static IResult GetAsync(IOptions<MyOptions> options)
    {
        return TypedResults.Content(
            $$"""
            <!DOCTYPE html>
            <html>

            <head>
                <title>я юра</title>
                <script>
                    var hash = window.location.hash.substring(1);

                    fetch("{{new Uri(options.Value.ServerUrl, "redirect")}}",
                        {
                            method: "POST",
                            body: hash
                        })
                        .then(response => {
                            window.location.href = "{{options.Value.ServerUrl}}";
                        })
                        .catch(reason => {
                            console.log(reason);
                        });
                </script>
            </head>

            <body>
                ща-ща...
            </body>

            </html>
            """, contentType: "text/html", Encoding.UTF8);
    }

    public static async Task<IResult> PostAsync(HttpContext httpContext, IOptions<MyOptions> options, ILogger<RedirectRoute> logger)
    {
        using MemoryStream ms = new();

        await httpContext.Request.Body.CopyToAsync(ms);

        var contentString = Encoding.UTF8.GetString(ms.ToArray());

        var content = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(contentString);

        if (content.TryGetValue("error", out StringValues errorValues))
        {
            var description = content.GetValueOrDefault("error_description").FirstOrDefault();

            logger.LogError("Получили ошибку в посте {error} ({description})", errorValues.First(), description);

            return TypedResults.BadRequest($"Ошибка... Вот сообщение: {errorValues.First()}\n{description}");
        }

        var accessToken = content["access_token"].First()!;
        var expires_in = content["expires_in"].First()!;
        var state = content["state"].First()!;

        if (MainRoute.LastState != state)
        {
            return TypedResults.BadRequest("Случилось что-то невероятно плохое.");
        }

        options.Value.Auth = new AuthInfo()
        {
            AccessToken = accessToken,
            Date = DateTime.UtcNow,
            ExpiresIn = TimeSpan.FromSeconds(int.Parse(expires_in))
        };

        await File.WriteAllTextAsync("options.json", JsonSerializer.Serialize(options.Value, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));

        logger.LogInformation("Получили пост.");

        return TypedResults.Ok();
    }
}
