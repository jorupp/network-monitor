using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class Settings
    {
        public static readonly string Section = nameof(Settings);
        public TimeSpan Interval { get; set; }
        public TimeSpan TimeAllowedEarly { get; set; } = TimeSpan.FromMilliseconds(50);
        public Dictionary<string, string> Http { get; set; }
        public Dictionary<string, string> Ping { get; set; }
    }
}
