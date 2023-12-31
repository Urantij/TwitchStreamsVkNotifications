using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VkNet;
using VkNet.Model;

namespace TwitchStreamsVkNotifications.Work;

/// <summary>
/// Создаёт пост в вк.
/// </summary>
public class VkPoster
{
    private readonly IOptions<MyOptions> options;
    private readonly ILogger<VkPoster> logger;

    private bool ignoreNextOnline = false;

    public VkPoster(IOptions<MyOptions> options, ILogger<VkPoster> logger)
    {
        this.options = options;
        this.logger = logger;
    }

    public void DoIgnoreNextOnline()
    {
        ignoreNextOnline = true;
    }

    public async Task PostAsync()
    {
        if (options.Value.Auth == null)
        {
            logger.LogError("Ауф не сделан.");
            return;
        }

        logger.LogInformation("Постим.");

        using VkApi api = new();

        await api.AuthorizeAsync(new ApiAuthParams
        {
            AccessToken = options.Value.Auth.AccessToken,
            Settings = VkNet.Enums.Filters.Settings.Wall
        });

        if (!ignoreNextOnline)
        {
            await api.Wall.PostAsync(new WallPostParams()
            {
                Guid = Guid.NewGuid().ToString(),

                OwnerId = options.Value.VkOwnerId,
                FromGroup = options.Value.VkOwnerId < 0 ? true : null,
                Signed = options.Value.VkOwnerId < 0 ? false : null,
                Message = options.Value.VkPostMessage,
            });
        }
        else
        {
            ignoreNextOnline = false;
        }

        logger.LogInformation("Запостили.");
    }
}
