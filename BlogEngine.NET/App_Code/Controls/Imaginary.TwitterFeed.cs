namespace Imaginary
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Web;
    using System.Web.Hosting;
    using BlogEngine.Core;

    public class TwitterFeedSettings
    {
        public const string TWITTERFEED_SETTINGS_CACHE_KEY = "twitterfeed-settings";
        private const string TWITTERFEED_DATA_FILENAME = "twitterfeed-data.xml";

        private string _twitterAccountName;
        private string _followMeText;
        private int _itemCount;
        private int _pollingInterval;
        private string _dateFormat;
        private bool _showRelativeDate;
        private StringDictionary _dictionary;
        private string _dataFileName = HostingEnvironment.MapPath(Path.Combine(BlogSettings.Instance.StorageLocation, TWITTERFEED_DATA_FILENAME));

        public TwitterFeedSettings(StringDictionary dictionary)
        {
            _dictionary = dictionary;
            this.DateFormat = DictionaryHelper.GetString(dictionary, "dateFormat", DefaultValues.DateFormat);
            this.FollowMeText = DictionaryHelper.GetString(dictionary, "followMeText", DefaultValues.FollowMeText);
            this.ItemCount = DictionaryHelper.GetInt32(dictionary, "itemCount", 5);
            this.PollingInterval = DictionaryHelper.GetInt32(dictionary, "pollingInterval", DefaultValues.PollingInterval);
            this.TwitterAccountName = DictionaryHelper.GetString(dictionary, "twitterAccountName", null);
            this.ShowRelativeDate = DictionaryHelper.GetBoolean(dictionary, "showRelativeDate", false);
        }

        public string TwitterAccountName
        {
            get { return _twitterAccountName; }
            set { _twitterAccountName = value; }
        }

        public string FollowMeText
        {
            get { return _followMeText; }
            set { _followMeText = value; }
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set { _itemCount = value; }
        }

        public int PollingInterval
        {
            get { return _pollingInterval; }
            set { _pollingInterval = value; }
        }

        public string DateFormat
        {
            get { return _dateFormat; }
            set { _dateFormat = value; }
        }

        public bool ShowRelativeDate
        {
            get { return _showRelativeDate; }
            set { _showRelativeDate = value; }
        }

        public StringDictionary Dictionary
        {
            get { return _dictionary; }
            private set { _dictionary = value; }
        }

        public string DataFileName
        {
            get { return _dataFileName; }
            set { _dataFileName = value; }
        }

        public void Save()
        {
            this.Dictionary["dateFormat"] = this.DateFormat;
            this.Dictionary["followMeText"] = this.FollowMeText;
            this.Dictionary["itemCount"] = this.ItemCount.ToString();
            this.Dictionary["pollingInterval"] = this.PollingInterval.ToString();
            this.Dictionary["twitterAccountName"] = this.TwitterAccountName;
            this.Dictionary["showRelativeDate"] = this.ShowRelativeDate.ToString();
        }
    }
}
