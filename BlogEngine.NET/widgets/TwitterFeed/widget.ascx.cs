// written by Donovan Olivier, http://planetdonovan.com
// modified version BlogEngine.NET 1.6 Twitter widget including enhancements by AL Bsharah.
// respect:
//     AL Bsharah - http://al.bsharah.com/post/2010/02/02/A-Better-Twitter-Widget-for-BlogEngine-NET-1-6.aspx
//     Scott Guthrie - http://weblogs.asp.net/scottgu/archive/2010/03/18/building-a-windows-phone-7-twitter-application-using-silverlight.aspx
// changelog: 
//     May 9, 2010 - initial release.
//     May 10, 2010 - added relative time option, this gets around the time zone issue.
//     May 11, 2010 - fixed bug with data not being refreshed after settings changed.
// known issues:
//     1. sometimes doesn't show data on first page load.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;
using BlogEngine.Core;
//using App_Code.Controls;
using Imaginary;

public partial class widgets_TwitterFeed_widget : WidgetBase
{
    private const string TWITTERFEED_DATA_FILENAME = "twitterfeed-data.xml";

    // if this changes, our widget will break. we could move this stuff into configuration but it might be too advanced for some users.
    private string _feedUrl = "http://api.twitter.com/1/statuses/user_timeline.atom?screen_name={0}";
    private string _twitterUrl = "http://twitter.com/{0}";
    private TwitterFeedSettings _settings;

    public widgets_TwitterFeed_widget()
    {
    }

    public override string Name
    {
        get { return "TwitterFeed"; }
    }

    public override bool IsEditable
    {
        get { return true; }
    }

    public string FeedUrl
    {
        get { return _feedUrl; }
        set { _feedUrl = value; }
    }

    public string TwitterUrl
    {
        get { return _twitterUrl; }
        set { _twitterUrl = value; }
    }

    private TwitterFeedSettings Settings
    {
        get { return _settings; }
        set { _settings = value; }
    }

    public override void LoadWidget()
    {
        this.Settings = this.GetFeedSettings();
        this.twitterLink.Text = this.Settings.FollowMeText;
        this.twitterLink.NavigateUrl = string.Format(this.TwitterUrl, this.Settings.TwitterAccountName);
        this.LoadData();
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

    private void LoadData()
    {
        if (File.Exists(this.Settings.DataFileName))
        {
            FileInfo info = new FileInfo(this.Settings.DataFileName);
            if (info.LastWriteTime.AddMinutes(this.Settings.PollingInterval) < DateTime.Now.AddSeconds(-30)) // little trick there in case pollingInverval = 0.
            {
                this.BeginExecuteRequest();
            }
            else
            {
                XmlTextReader reader = new XmlTextReader(this.Settings.DataFileName);
                try
                {
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    List<SyndicationItem> items = new List<SyndicationItem>(feed.Items);
                    if (items.Count > this.Settings.ItemCount)
                    {
                        this.BindData(items.GetRange(0, this.Settings.ItemCount));
                    }
                    else
                    {
                        this.BindData(feed.Items);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
        }
        else
        {
            this.BeginExecuteRequest();
        }
    }

    private void BeginExecuteRequest()
    {
        WebRequest request = HttpWebRequest.Create(string.Format(this.FeedUrl, this.Settings.TwitterAccountName));
        request.BeginGetResponse(new AsyncCallback(this.EndExecuteRequest), request);
    }

    private void EndExecuteRequest(IAsyncResult result)
    {
        WebRequest request = (WebRequest)result.AsyncState;
        WebResponse response = request.EndGetResponse(result);
        XmlDocument document = new XmlDocument();
        document.Load(response.GetResponseStream());
        document.Save(this.Settings.DataFileName);
        this.LoadData();
    }

    private void BindData(IEnumerable<SyndicationItem> data)
    {
        this.twitterItems.DataSource = data;
        this.twitterItems.DataBind();
    }

    private string GetFormattedContent(string raw)
    {
        // first, strip off twitter account name.
        if (raw.StartsWith(this.Settings.TwitterAccountName + ": "))
        {
            raw = raw.Substring(this.Settings.TwitterAccountName.Length + 2);
        }
        // resolve links, user names and hash tags.
        raw = regex.Replace(raw, new MatchEvaluator(this.LinkEvaluator));
        raw = regex2.Replace(raw, new MatchEvaluator(this.UserEvaluator));
        raw = regex3.Replace(raw, new MatchEvaluator(this.HashEvaluator));
        return raw;
    }

    protected void ItemDataBoundHandler(object sender, RepeaterItemEventArgs e)
    {
        string twitterIcon = "<img runat=\"server\" style=\"margin-bottom:-5px\" src=\"{0}widgets/TwitterFeed/twitter-icon.png\" alt=\"\" />";
        SyndicationItem item = (SyndicationItem)e.Item.DataItem;
        ((Label)e.Item.FindControl("itemText")).Text = string.Format(twitterIcon, Utils.RelativeWebRoot) + this.GetFormattedContent(((TextSyndicationContent)item.Content).Text);
        ((Label)e.Item.FindControl("itemDate")).Text = this.GetFormattedDate(item.PublishDate);
        foreach (SyndicationElementExtension ext in item.ElementExtensions)
        {
            if (ext.OuterName == "source")
            {
			    string via = Server.HtmlDecode(ext.GetReader().ReadInnerXml().Replace("\r\n", "").Trim()).Replace("<a ","<a target=\"_blank\" rel=\"nofollow\" ");
                ((Label)e.Item.FindControl("itemVia")).Text = "via " +  via;
                break;
            }
        }
    }

    #region straight from al bsharah
    //URL
    private static readonly Regex regex = new Regex("((http://|https://|www\\.)([A-Z0-9.\\-]{1,})\\.[0-9A-Z?;~&\\(\\)#,=\\-_\\./\\+]{2,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const string link = "<a href=\"{0}{1}\" rel=\"nofollow\">{1}</a>";
    //@User
    private static readonly Regex regex2 = new Regex("@[a-zA-Z0-9_]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const string link2 = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";
    //#Hash
    private static readonly Regex regex3 = new Regex("#[a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const string link3 = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";

    private string LinkEvaluator(Match match)
    {
        CultureInfo info = CultureInfo.InvariantCulture;
        if (!match.Value.Contains("://"))
        {
            return string.Format(info, link, "http://", match.Value);
        }
        else
        {
            return string.Format(info, link, string.Empty, match.Value);
        }
    }

    private string UserEvaluator(Match match)
    {
        CultureInfo info = CultureInfo.InvariantCulture;
        String user = Regex.Replace(match.Value, "@", "");
        return string.Format(info, link2, "http://twitter.com/", user, match.Value);
    }

    private string HashEvaluator(Match match)
    {
        CultureInfo info = CultureInfo.InvariantCulture;
        String linktag = Regex.Replace(match.Value, "#", "%23");
        return string.Format(info, link3, "http://search.twitter.com/search?q=", linktag, match.Value);
    }
    #endregion

    private string GetFormattedDate(DateTimeOffset date)
    {
        if (this.Settings.ShowRelativeDate)
        {
            return BlogHelper.ToRelativeTime(date, this.Settings.DateFormat);
        }
        else
        {
            return string.Format(this.Settings.DateFormat, BlogHelper.ToLocalTime(date));
        }
    }
}
