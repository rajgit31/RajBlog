// written by Donovan Olivier, http://planetdonovan.com
// loosely based on BlogEngine.NET 1.6 Twitter widget.
// respect:
//     Peter Bromberg - http://www.eggheadcafe.com/tutorials/aspnet/9fea2d46-f10a-4152-92e0-4d6f4f5f7063/silverlight-2-beta-2---d.aspx
//     Scott Guthrie - http://weblogs.asp.net/scottgu/archive/2010/03/18/building-a-windows-phone-7-twitter-application-using-silverlight.aspx
// changelog: 
//     May 5, 2010 - initial release.
//     May 10, 2010 - added relative time option, this gets around the time zone issue.
//     May 11, 2010 - removed language setting as it limits search results.
//                    fixed bug with data not being refreshed after settings changed.
// known issues:
//     1. sometimes the data doesn't bind, it's the Repeater.ItemDataBound event not firing and i don't know why.
//     2. i know i should have used divs instead of a table, but i couldn't get the right content div to dynamically
//        size if the widget width is different in the theme (if you know how, please tell me).

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;
using BlogEngine.Core;
//using App_Code.Controls;
using Imaginary;

public partial class widgets_TwitterSearch_widget : WidgetBase
{
    // if this changes, our widget will break. we could move this stuff into configuration but it might be too advanced for some users.
    private string _searchUrl = "http://search.twitter.com/search.atom?q={0}&rpp={1}";
    private string _twitterUrl = "http://twitter.com/{0}";
    private TwitterSearchSettings _settings;

    public widgets_TwitterSearch_widget()
    {
    }

    public override string Name
    {
        get { return "TwitterSearch"; }
    }

    public override bool IsEditable
    {
        get { return true; }
    }

    public string SearchUrl
    {
        get { return _searchUrl; }
        set { _searchUrl = value; }
    }

    public string TwitterUrl
    {
        get { return _twitterUrl; }
        set { _twitterUrl = value; }
    }

    private TwitterSearchSettings Settings
    {
        get { return _settings; }
        set { _settings = value; }
    }

    public override void LoadWidget()
    {
        this.Settings = this.GetSearchSettings();
        this.twitterLink.Text = this.Settings.FollowMeText;
        this.twitterLink.NavigateUrl = string.Format(this.TwitterUrl, this.Settings.TwitterAccountName);
        this.LoadData();
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
                    this.BindData(feed.Items);
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
        WebRequest request = HttpWebRequest.Create(string.Format(this.SearchUrl, this.Settings.SearchTerm, this.Settings.ItemCount));
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

    protected void ItemDataBoundHandler(object sender, RepeaterItemEventArgs e)
    {
        SyndicationItem item = (SyndicationItem)e.Item.DataItem;
        foreach (SyndicationLink link in item.Links)
        {
            if (link.RelationshipType == "image")
            {
                ((Image)e.Item.FindControl("itemImage")).ImageUrl = link.Uri.AbsoluteUri;
                break;
            }
        }
        string accountName = item.Authors[0].Uri.Substring(item.Authors[0].Uri.LastIndexOf('/') + 1);
        ((Label)e.Item.FindControl("itemText")).Text = ((TextSyndicationContent)item.Content).Text.Replace("<b>", "").Replace("</b>", ""); // search term comes up bold.
        ((Label)e.Item.FindControl("itemUser")).Text = string.Format("<a href=\"{0}\">{1}</a>", item.Authors[0].Uri, accountName);
        ((Label)e.Item.FindControl("itemDate")).Text = this.GetFormattedDate(item.PublishDate);
    }

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
