namespace Imaginary
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Web;
    using BlogEngine.Core;

    public static class DefaultValues
    {
        public const string DateFormat = "{0:dddd h:mmtt}";
        public const string FollowMeText = "Follow me on Twitter";
        public const int PollingInterval = 15;
    }

    public static class DictionaryHelper
    {
        // bummer, would have made these extension methods in 3.5+.
        public static int GetInt32(StringDictionary dictionary, string key, int defaultValue)
        {
            int result;
            if (int.TryParse(GetString(dictionary, key, string.Empty), out result))
            {
                return result;
            }
            return defaultValue;
        }

        public static string GetString(StringDictionary dictionary, string key, string defaultValue)
        {
            if (dictionary.ContainsKey(key) && !string.IsNullOrEmpty(dictionary[key]))
            {
                return dictionary[key];
            }
            return defaultValue;
        }

        public static bool GetBoolean(StringDictionary dictionary, string key, bool defaultValue)
        {
            if (dictionary.ContainsKey(key) && !string.IsNullOrEmpty(dictionary[key]))
            {
                return bool.Parse(dictionary[key]);
            }
            return defaultValue;
        }
    }

    public static class BlogHelper
    {
        public static DateTimeOffset ToLocalTime(DateTimeOffset date)
        {
            TimeSpan offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            return date.Add(offset).AddHours(BlogSettings.Instance.Timezone);
        }

        // ripped with mods from stackoverflow: respect, jeff atwood.
        public static string ToRelativeTime(DateTimeOffset date, string formatString)
        {
            /* not using the date format at this point, but might...  */
            TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - date.Ticks);
            double delta = ts.TotalSeconds;

            if (delta < 45)
            {
                return "a few seconds ago";
            }
            if (delta < 60)
            {
                return "less than a minute ago";
            }
            if (delta < 120)
            {
                return "a minute ago";
            }
            if (delta < 2700) // 45 * 60
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 4500) // 75 * 60
            {
                return "about an hour ago";
            }
            if (delta < 7200)
            {
                return "an hour ago";
            }
            if (delta < 86400) // 24 * 60 * 60
            {
                return ts.Hours + " hours ago";
            }
            if (delta < 172800) // 48 * 60 * 60
            {
                return "1 day ago";
            }
            if (delta < 2592000) // 30 * 24 * 60 * 60
            {
                return ts.Days + " days ago";
            }
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }


}
