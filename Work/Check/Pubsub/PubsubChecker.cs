using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwitchSimpleLib.Pubsub;
using TwitchSimpleLib.Pubsub.Payloads.Playback;

namespace TwitchStreamsVkNotifications.Work.Check.Pubsub;

public class PubsubChecker : ITwitchChecker, IHostedService, IDisposable
{
    private readonly ILogger<PubsubChecker> logger;

    private readonly TwitchPubsubClient client;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public PubsubChecker(IOptions<MyOptions> options, ILogger<PubsubChecker> logger, ILoggerFactory loggerFactory)
    {
        this.logger = logger;

        client = new TwitchPubsubClient(new TwitchPubsubClientOpts()
        {
        }, loggerFactory);
        client.Connected += ClientConnected;
        client.ConnectionClosed += ClientConnectionClosed;

        var topic = client.AddPlaybackTopic(options.Value.TwitchChannelId);
        topic.DataReceived = PlaybackReceived;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Начинаем.");

        await client.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Close();
        Dispose();

        return Task.CompletedTask;
    }

    private void PlaybackReceived(PlaybackData data)
    {
        bool up;
        if (data.Type == "stream-up")
        {
            up = true;
        }
        else if (data.Type == "stream-down")
        {
            up = false;
        }
        else return;

        TwitchCheckInfo checkInfo = new(up, DateTime.UtcNow);

        try
        {
            ChannelChecked?.Invoke(this, checkInfo);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{nameof(PlaybackReceived)}");
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
        client.Close();
    }
}
