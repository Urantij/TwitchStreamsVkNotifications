using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchStreamsVkNotifications.Work.Check.Helix;

public class HelixConfig
{
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string Secret { get; set; }

    public TimeSpan HelixCheckDelay { get; set; } = TimeSpan.FromMinutes(1);
}
