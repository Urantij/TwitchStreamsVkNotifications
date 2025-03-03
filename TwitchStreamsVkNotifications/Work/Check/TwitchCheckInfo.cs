using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchStreamsVkNotifications.Work.Check;

public class TwitchCheckInfo
{
    public readonly bool online;
    /// <summary>
    /// UTC
    /// </summary>
    public readonly DateTime checkTime;

    public TwitchCheckInfo(bool online, DateTime checkTime)
    {
        this.online = online;
        this.checkTime = checkTime;
    }
}
