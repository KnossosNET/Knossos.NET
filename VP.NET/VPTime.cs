using System;

namespace VP.NET
{
    /// <summary>
    /// Helper class to handle the date timestamp in vps
    /// </summary>
    public static class VPTime
    {
        /// <summary>
        /// Gets the current timestamp
        /// </summary>
        /// <returns> unix timestamp </returns>
        public static int GetCurrentTime()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Gets the actual datetime from the unix timestamp used in vps
        /// Returns null if the passed timestamp is 0
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns> datetime or null </returns>
        public static DateTime? GetDateFromUnixTimeStamp(int unixTimeStamp)
        {
            if(unixTimeStamp == 0)
            {
                return null;
            }
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
