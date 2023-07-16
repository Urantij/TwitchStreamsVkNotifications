using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwitchSimpleLib.Pubsub;
using TwitchSimpleLib.Pubsub.Payloads.Playback;

namespace TwitchStreamsVkNotifications.Work;

/// <summary>
/// Создаёт клиент пабсаба, который и даёт нам информацию о том, что стрим запустился.
/// Тут же и отправляется информация в вк.
/// </summary>
public class TwitchChecker : IDisposable
{
    readonly IOptions<MyOptions> options;
    readonly IServiceScopeFactory serviceScopeFactory;
    readonly ILogger logger;

    bool started = false;

    readonly TwitchPubsubClient pubsubClient;

    bool lastOnline = false;
    DateTime? lastUpdate = null;

    public TwitchChecker(IOptions<MyOptions> options, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        this.options = options;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = loggerFactory.CreateLogger(this.GetType());

        pubsubClient = new TwitchPubsubClient(new TwitchPubsubClientOpts()
        {
        }, loggerFactory);
        pubsubClient.Connected += ClientConnected;
        pubsubClient.ConnectionClosed += ClientConnectionClosed;

        var topic = pubsubClient.AddPlaybackTopic(options.Value.TwitchChannelId);
        topic.DataReceived = PlaybackReceived;
    }

    public void Init()
    {
        if (started)
            return;

        started = true;

        pubsubClient.ConnectAsync();
    }

    private async void PlaybackReceived(PlaybackData data)
    {
        try
        {
            if (data.Type == "stream-up")
            {
                logger.LogInformation("Стрим поднялся.");

                if (!lastOnline && (lastUpdate == null || DateTime.UtcNow - lastUpdate.Value > options.Value.ReplayCooldown))
                {
                    using var scope = serviceScopeFactory.CreateScope();

                    var poster = scope.ServiceProvider.GetRequiredService<VkPoster>();
                    await poster.PostAsync();
                }

                lastUpdate = DateTime.UtcNow;
                lastOnline = true;
            }
            else if (data.Type == "stream-down")
            {
                logger.LogInformation("Стрим опустился.");

                lastUpdate = DateTime.UtcNow;
                lastOnline = false;
            }
            else return;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при обработке информации.");
        }
    }

    private void ClientConnected()
    {
        logger.LogInformation("Клиент присоединился.");
    }

    private void ClientConnectionClosed(Exception? exception)
    {
        logger.LogInformation("Клиент потерял соединение. {message}", exception?.Message);
    }

    public void Dispose()
    {
        pubsubClient.Close();
    }
}
