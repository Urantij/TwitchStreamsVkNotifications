using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchStreamsVkNotifications.Work.Check;

public interface ITwitchChecker
{
    public event EventHandler<TwitchCheckInfo>? ChannelChecked;
}
