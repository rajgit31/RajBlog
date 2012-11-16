using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
//using App_Code.Controls;
using Imaginary;

public partial class widgets_TwitterFeed_edit : WidgetEditBase
{
    public widgets_TwitterFeed_edit()
    {
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!this.IsPostBack)
        {
            // intializing TwitterFeedSettings object so that we have access to default values.
            TwitterFeedSettings settings = this.GetFeedSettings();
            this.itemCount.Text = settings.ItemCount.ToString();
            this.pollingInterval.Text = settings.PollingInterval.ToString();
            this.dateFormat.Text = settings.DateFormat;
            this.twitterAccountName.Text = settings.TwitterAccountName;
            this.showRelativeDate.Checked = settings.ShowRelativeDate;
            this.followMeText.Text = settings.FollowMeText;
        }
    }

    public override void Save()
    {
        TwitterFeedSettings settings = this.GetFeedSettings();
        settings.DateFormat = this.dateFormat.Text;
        settings.FollowMeText = this.followMeText.Text;
        settings.ItemCount = int.Parse(this.itemCount.Text);
        settings.PollingInterval = int.Parse(this.pollingInterval.Text);
        settings.TwitterAccountName = this.twitterAccountName.Text;
        settings.ShowRelativeDate = this.showRelativeDate.Checked;
        settings.Save();
        this.SaveSettings(settings.Dictionary);
        if (File.Exists(settings.DataFileName))
        {
            File.Delete(settings.DataFileName);
        }
    }

    private TwitterFeedSettings GetFeedSettings()
    {
        TwitterFeedSettings settings = HttpRuntime.Cache[TwitterFeedSettings.TWITTERFEED_SETTINGS_CACHE_KEY] as TwitterFeedSettings;
        if (settings == null)
        {
            settings = new TwitterFeedSettings(this.GetSettings());
            HttpRuntime.Cache[TwitterFeedSettings.TWITTERFEED_SETTINGS_CACHE_KEY] = settings;
        }
        return settings;
    }
}