using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchStreamsVkNotifications;

public class AuthInfo
{
    public required string AccessToken { get; set; }
    public required DateTime Date { get; set; }
    public required TimeSpan ExpiresIn { get; set; }
}
