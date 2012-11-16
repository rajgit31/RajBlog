#region Using

using System;
using System.Xml;
using System.IO;
using System.Web;
using System.Collections;
using System.Globalization;
using BlogEngine.Core;
using System.Net;
using System.Threading;

#endregion

namespace BlogEngine.Core.Web.HttpModules
{
  /// <summary>
  /// Summary description for ReferrerModule
  /// </summary>
  public class ReferrerModule : IHttpModule
  {

    #region IHttpModule Members

    /// <summary>
    /// Disposes of the resources (other than memory) used by the 
    /// module that implements <see cref="T:System.Web.IHttpModule"></see>.
    /// </summary>
    public void Dispose()
    {
      // Nothing to dispose.
    }

    /// <summary>
    /// Initializes a module and prepares it to handle requests.
    /// </summary>
    /// <param name="context">An <see cref="T:System.Web.HttpApplication"></see> that 
    /// provides access to the methods, properties, and events common to all application 
    /// objects within an ASP.NET application
    /// </param>
    public void Init(HttpApplication context)
    {
      if (BlogSettings.Instance.EnableReferrerTracking)
        context.EndRequest += new EventHandler(context_BeginRequest);
    }

    #endregion

    /// <summary>
    /// Handles the BeginRequest event of the context control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void context_BeginRequest(object sender, EventArgs e)
    {
      HttpContext context = ((HttpApplication)sender).Context;
      if (!context.Request.Path.ToUpperInvariant().Contains(".ASPX"))
          return;

      if (context.Request.UrlReferrer != null)
      {
        Uri referrer = context.Request.UrlReferrer;
        if (!referrer.Host.Equals(Utils.AbsoluteWebRoot.Host, StringComparison.OrdinalIgnoreCase) && !IsSearchEngine(referrer.ToString()))
        {
            ThreadPool.QueueUserWorkItem(BeginRegisterClick, new DictionaryEntry(referrer, context.Request.Url));
        }
      }
    }

    #region Private fields

    /// <summary>
    /// Used to thread safe the file operations
    /// </summary>
    private static object _SyncRoot = new object();

    /// <summary>
    /// The relative path of the XML file.
    /// </summary>
    //private static string _Folder = HttpContext.Current.Server.MapPath("~/App_Data/log/");
      private static string _Folder = HttpContext.Current.Server.MapPath(BlogSettings.Instance.StorageLocation + "log/");

      

    #endregion

    private static bool IsSearchEngine(string referrer)
    {
      string lower = referrer.ToUpperInvariant();
      if (lower.Contains("YAHOO") && lower.Contains("P="))
        return true;

      return lower.Contains("?Q=") || lower.Contains("&Q=");
    }

    /// <summary>
    /// Determines whether the specified referrer is spam.
    /// </summary>
    /// <param name="referrer">The referrer.</param>
    /// <param name="url">The URL.</param>
    /// <returns>
    /// 	<c>true</c> if the specified referrer is spam; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsSpam(Uri referrer, Uri url)
    {
      try
      {
        using (WebClient client = new WebClient())
        {
          string html = client.DownloadString(referrer).ToUpperInvariant();
          string subdomain = Utils.GetSubDomain(url);
          string host = url.Host.ToUpperInvariant();
          
          if (subdomain != null)
            host = host.Replace(subdomain.ToUpperInvariant() + ".", string.Empty);

          return !html.Contains(host);
        }
      }
      catch
      {
        return true;
      }
    }

    private static void BeginRegisterClick(object stateInfo)
    {
			try
			{
				DictionaryEntry entry = (DictionaryEntry)stateInfo;
				Uri referrer = (Uri)entry.Key;
				Uri url = (Uri)entry.Value;

				RegisterClick(url, referrer);
				stateInfo = null;
				OnReferrerRegistered(referrer);
			}
			catch (Exception)
			{
				// Could write to the file.
			}
    }

    private static void RegisterClick(Uri url, Uri referrer)
    {
        Referrer refer = null;
        if (Referrer.Referrers != null && Referrer.Referrers.Count > 0)
        {
            refer = Referrer.Referrers.Find(r => r.ReferrerUrl.Equals(referrer) && r.Url.Equals(url) && r.Day == DateTime.Today);
        }

        if (refer == null)
        {
            refer = new Referrer()
            {
                Day = DateTime.Today,
                ReferrerUrl = referrer,
                Url = url,
                PossibleSpam = IsSpam(referrer, url)
            };
        }
        refer.Count += 1;

        refer.Save();
    }

    #region Events

    /// <summary>
    /// Occurs when a visitor enters the website and the referrer is logged.
    /// </summary>
    public static event EventHandler<EventArgs> ReferrerRegistered;
    /// <summary>
    /// Raises the event in a safe way
    /// </summary>
    private static void OnReferrerRegistered(Uri referrer)
    {
      if (ReferrerRegistered != null)
      {
        ReferrerRegistered(referrer, EventArgs.Empty);
      }
    }

    #endregion

  }
}