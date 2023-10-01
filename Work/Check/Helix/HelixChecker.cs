using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwitchLib.Api;

namespace TwitchStreamsVkNotifications.Work.Check.Helix;

public class HelixChecker : BackgroundService, ITwitchChecker
{
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<HelixChecker> logger;
    private readonly HelixConfig config;
    private readonly string channelId;

    readonly TwitchAPI api;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public HelixChecker(IOptions<MyOptions> myOptions, IOptions<HelixConfig> options, IHostApplicationLifetime lifetime, ILogger<HelixChecker> logger)
    {
        this.lifetime = lifetime;
        this.logger = logger;
        this.config = options.Value;
        this.channelId = myOptions.Value.TwitchChannelId;

        api = new TwitchAPI();
        api.Settings.ClientId = config.ClientId;
        api.Settings.Secret = config.Secret;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Начинаем.");

        await Task.Run(CheckLoopAsync);
    }

    async Task CheckLoopAsync()
    {
        while (!lifetime.ApplicationStopping.IsCancellationRequested)
        {
            TwitchCheckInfo? checkInfo = await CheckChannelAsync();

            // Если ошибка, стоит подождать чуть больше обычного.
            if (checkInfo == null)
            {
                await Task.Delay(config.HelixCheckDelay.Multiply(1.5));
                continue;
            }

            try
            {
                ChannelChecked?.Invoke(this, checkInfo);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{nameof(CheckLoopAsync)}");
            }

            await Task.Delay(config.HelixCheckDelay);
        }
    }

    /// <returns>null, если ошибка внеплановая</returns>
    private async Task<TwitchCheckInfo?> CheckChannelAsync()
    {
        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream;

        try
        {
            var response = await api.Helix.Streams.GetStreamsAsync(userIds: new List<string>() { channelId }, first: 1);

            if (response.Streams.Length == 0)
            {
                return new TwitchCheckInfo(false, DateTime.UtcNow);
            }

            stream = response.Streams[0];

            if (!stream.Type.Equals("live", StringComparison.OrdinalIgnoreCase))
                return new TwitchCheckInfo(false, DateTime.UtcNow);
        }
        catch (TwitchLib.Api.Core.Exceptions.BadScopeException)
        {
            logger.LogWarning($"CheckChannel exception опять BadScopeException");

            return null;
        }
        catch (TwitchLib.Api.Core.Exceptions.InternalServerErrorException)
        {
            logger.LogWarning($"CheckChannel exception опять InternalServerErrorException");

            return null;
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning("CheckChannel HttpRequestException: \"{Message}\"", e.Message);

            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "CheckChannel");

            return null;
        }

        return new TwitchCheckInfo(true, DateTime.UtcNow);
    }
}
