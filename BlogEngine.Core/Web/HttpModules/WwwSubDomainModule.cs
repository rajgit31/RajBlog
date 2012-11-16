#region Using

using System;
using System.Web;
using BlogEngine.Core;
using System.Text.RegularExpressions;

#endregion

namespace BlogEngine.Core.Web.HttpModules
{
  /// <summary>
  /// Removes or adds the www subdomain from all requests
  /// and makes a permanent redirection to the new location.
  /// </summary>
  public class WwwSubDomainModule : IHttpModule
  {

    #region IHttpModule Members

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"></see>.
		/// </summary>
    public void Dispose()
    {
      // Nothing to dispose
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void Init(HttpApplication context)
    {
      context.BeginRequest += new EventHandler(context_BeginRequest);
    }

    #endregion

    private static Regex _Regex = new Regex("(http|https)://www\\.", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private void context_BeginRequest(object sender, EventArgs e)
    {
      if (BlogSettings.Instance.HandleWwwSubdomain == "ignore" || string.IsNullOrEmpty(BlogSettings.Instance.HandleWwwSubdomain))
        return;

      HttpContext context = (sender as HttpApplication).Context;
      if (context.Request.HttpMethod != "GET" || context.Request.RawUrl.Contains("/admin/") || context.Request.IsLocal)
        return;

      if (context.Request.PhysicalPath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
      {
        string url = context.Request.Url.ToString();

        if (url.Contains("://www.") && BlogSettings.Instance.HandleWwwSubdomain == "remove")
          RemoveWww(context);
        
        if (!url.Contains("://www.") && BlogSettings.Instance.HandleWwwSubdomain == "add")
          AddWww(context);        
      }
    }

    /// <summary>
    /// Adds the www subdomain to the request and redirects.
    /// </summary>
    private static void AddWww(HttpContext context)
    {
      string url = context.Request.Url.ToString().Replace("://", "://www.");
      PermanentRedirect(url, context);
    }

    /// <summary>
    /// Removes the www subdomain from the request and redirects.
    /// </summary>
    private static void RemoveWww(HttpContext context)
    {
      string url = context.Request.Url.ToString();
      if (_Regex.IsMatch(url))
      {
        url = _Regex.Replace(url, "$1://");        
        PermanentRedirect(url, context);
      }
    }

    /// <summary>
    /// Sends permanent redirection headers (301)
    /// </summary>
    private static void PermanentRedirect(string url, HttpContext context)
    {
			if (url.EndsWith("default.aspx", StringComparison.OrdinalIgnoreCase))
				url = url.ToLowerInvariant().Replace("default.aspx", string.Empty);

      context.Response.Clear();
      context.Response.StatusCode = 301;
      context.Response.AppendHeader("location", url);
      context.Response.End();
    } 

  }
}