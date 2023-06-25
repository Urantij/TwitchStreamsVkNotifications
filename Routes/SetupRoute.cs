using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace TwitchStreamsVkNotifications.Routes;

public class SetupRoute
{
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

    public static async Task<IResult> DeleteAsync(IOptions<MyOptions> options, ILogger<RedirectRoute> logger)
    {
        if (options.Value.Auth == null)
        {
            logger.LogWarning("Попытка удалить ауф, когда его уже нет.");
            return TypedResults.BadRequest();
        }

        options.Value.Auth = null;

        await File.WriteAllTextAsync("options.json", JsonSerializer.Serialize(options.Value, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));

        logger.LogInformation("Получили делит.");

        return TypedResults.Ok();
    }
}
