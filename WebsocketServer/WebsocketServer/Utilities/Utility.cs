using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    static class Utility
    {
        public static DateTime GetCurrentUTC()
        {
            return DateTime.UtcNow;
        }

        public static long UTCNowMilliseconds()
        {
            TimeSpan timeSpan = new TimeSpan(System.DateTime.UtcNow.Ticks);
            return (long)timeSpan.TotalMilliseconds;
        }

        public static long UTCNowSeconds()
        {
            TimeSpan timeSpan = new TimeSpan(System.DateTime.UtcNow.Ticks);
            return (long)timeSpan.TotalSeconds;
        }

        /// <summary>
        /// </summary>
        /// <param name="utcTimeString">like: 03:40:20</param>
        /// <param name="toLocalTime"></param>
        /// <returns></returns>
        public static DateTime? ParseUTCTimeString(string utcTimeString, DateTimeKind dateTimeKind = DateTimeKind.Local)
        {
            if (string.IsNullOrEmpty(utcTimeString))
            {
                return null;
            }

            string[] segments = utcTimeString.Split(':');
            if (segments == null || segments.Length != 3)
            {
                return null;
            }

            int hour, mins, secs;
            if (!int.TryParse(segments[0], out hour) ||
                !int.TryParse(segments[1], out mins) ||
                !int.TryParse(segments[2], out secs) ||
                hour < 0 || hour > 23 ||
                mins < 0 || mins > 59 ||
                secs < 0 || secs > 59)
            {
                return null;
            }

            // date is meaningless
            return new DateTime(2000, 1, 1, hour, mins, secs, dateTimeKind);
        }
    }
}
