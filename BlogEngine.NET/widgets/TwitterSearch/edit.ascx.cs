using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
//using App_Code.Controls;
using Imaginary;

public partial class widgets_TwitterSearch_edit : WidgetEditBase
{
    public widgets_TwitterSearch_edit()
    {
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!this.IsPostBack)
        {
            // intializing TwitterSearchSettings object so that we have access to default values.
            TwitterSearchSettings settings = this.GetSearchSettings();
            this.searchTerm.Text = settings.SearchTerm;
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
        TwitterSearchSettings settings = this.GetSearchSettings();
        settings.DateFormat = this.dateFormat.Text;
        settings.FollowMeText = this.followMeText.Text;
        settings.ItemCount = int.Parse(this.itemCount.Text);
        settings.PollingInterval = int.Parse(this.pollingInterval.Text);
        settings.SearchTerm = this.searchTerm.Text;
        settings.TwitterAccountName = this.twitterAccountName.Text;
        settings.ShowRelativeDate = this.showRelativeDate.Checked;
        settings.Save();
        this.SaveSettings(settings.Dictionary);
        if (File.Exists(settings.DataFileName))
        {
            File.Delete(settings.DataFileName);
        }
    }

    private TwitterSearchSettings GetSearchSettings()
    {
        TwitterSearchSettings settings = HttpRuntime.Cache[TwitterSearchSettings.TWITTERSEARCH_SETTINGS_CACHE_KEY] as TwitterSearchSettings;
        if (settings == null)
        {
            settings = new TwitterSearchSettings(this.GetSettings());
            HttpRuntime.Cache[TwitterSearchSettings.TWITTERSEARCH_SETTINGS_CACHE_KEY] = settings;
        }
        return settings;
    }
}