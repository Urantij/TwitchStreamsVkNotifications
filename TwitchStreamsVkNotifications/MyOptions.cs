using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchStreamsVkNotifications;

public class MyOptions
{
    public required string TwitchChannelId { get; set; }

    public required ulong VkClientId { get; set; }
    public required long VkOwnerId { get; set; }
    public required string VkPostMessage { get; set; }

    /// <summary>
    /// Если случится переподруб в это временное окно, то нового анонса не будет.
    /// </summary>
    public TimeSpan ReplayCooldown { get; set; } = TimeSpan.FromMinutes(5);

    public AuthInfo? Auth { get; set; }
}
