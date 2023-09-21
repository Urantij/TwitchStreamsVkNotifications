using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwitchSimpleLib.Pubsub;
using TwitchSimpleLib.Pubsub.Payloads.Playback;
using TwitchStreamsVkNotifications.Work.Check;
using TwitchStreamsVkNotifications.Work.Check.Helix;
using TwitchStreamsVkNotifications.Work.Check.Pubsub;

namespace TwitchStreamsVkNotifications.Work;

/// <summary>
/// Следит за появлением стримов.
/// Тут же и отправляется информация в вк.
/// </summary>
public class TwitchChecker : IHostedService
{
    readonly IOptions<MyOptions> options;
    readonly IServiceScopeFactory serviceScopeFactory;
    readonly ILogger logger;

    bool lastOnline = false;
    DateTime? lastUpdate = null;

    public TwitchChecker(IEnumerable<ITwitchChecker> checkers, IOptions<MyOptions> options, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        this.options = options;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = loggerFactory.CreateLogger(this.GetType());

        foreach (var checker in checkers)
        {
            checker.ChannelChecked += ChannelChecked;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async void ChannelChecked(object? sender, TwitchCheckInfo info)
    {
        try
        {
            if (info.online)
            {
                logger.LogInformation("Стрим поднялся. {name}", sender?.GetType().Name);

                if (!lastOnline && (lastUpdate == null || DateTime.UtcNow - lastUpdate.Value > options.Value.ReplayCooldown))
                {
                    // Если придёт несколько ивентов, ожидание поста может задержать обновление ластапдейта.
                    lastUpdate = DateTime.UtcNow;
                    lastOnline = true;

                    using var scope = serviceScopeFactory.CreateScope();

                    var poster = scope.ServiceProvider.GetRequiredService<VkPoster>();
                    await poster.PostAsync();
                }
                else
                {
                    lastUpdate = DateTime.UtcNow;
                    lastOnline = true;
                }
            }
            else
            {
                logger.LogInformation("Стрим опустился. {name}", sender?.GetType().Name);

                lastUpdate = DateTime.UtcNow;
                lastOnline = false;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при обработке информации. {name}", sender?.GetType().Name);
        }
    }
}
