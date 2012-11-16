#region Using

using System;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpModules
{
	/// <summary>
	/// Handles pretty URL's and redirects them to the permalinks.
	/// </summary>
	public class UrlRewrite : IHttpModule
	{
		#region IHttpModule Members

		/// <summary>
		/// 
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

		/// <summary>
		/// Handles the BeginRequest event of the context control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void context_BeginRequest(object sender, EventArgs e)
		{
			HttpContext context = ((HttpApplication)sender).Context;
			string path = context.Request.Path.ToUpperInvariant();
			string url = context.Request.RawUrl.ToUpperInvariant();

            path = path.Replace(".ASPX.CS", "");
            url = url.Replace(".ASPX.CS", "");

			if (url.Contains(BlogSettings.Instance.FileExtension.ToUpperInvariant()) && !url.Contains("ERROR404.ASPX"))
			{
				if (path == Utils.RelativeWebRoot.ToUpperInvariant() + "DEFAULT.ASPX" && context.Request.QueryString.Count == 0)
				{
					Page front = Page.GetFrontPage();
					if (front != null)
						url = front.RelativeLink.ToUpperInvariant();
				}

				if (url.Contains("/POST/"))
				{
					RewritePost(context, url);
				}
				else if (url.Contains("/CATEGORY/"))
				{
					RewriteCategory(context, url);
				}
				else if (url.Contains("/PAGE/"))
				{
					RewritePage(context, url);
				}
				else if (url.Contains("/CALENDAR/"))
				{
					context.RewritePath(Utils.RelativeWebRoot + "default.aspx?calendar=show", false);
				}
				else if (url.Contains("/DEFAULT" + BlogSettings.Instance.FileExtension.ToUpperInvariant()))
				{
					RewriteDefault(context);
				}
				else if (url.Contains("/AUTHOR/"))
				{
					string author = ExtractTitle(context, url);
					context.RewritePath(Utils.RelativeWebRoot + "default" + BlogSettings.Instance.FileExtension + "?name=" + author + GetQueryString(context), false);
				}
				else if (path.Contains("/BLOG.ASPX"))
				{
					context.RewritePath(Utils.RelativeWebRoot + "default.aspx?blog=true" + GetQueryString(context));
				}
			}
		}

		private static void RewritePost(HttpContext context, string url)
		{
			DateTime date = ExtractDate(context);
			string slug = ExtractTitle(context, url);
			Post post = Post.Posts.Find(delegate(Post p)
			{
                if (date != DateTime.MinValue &&
                    (p.DateCreated.Year != date.Year || p.DateCreated.Month != date.Month || p.DateCreated.Day != date.Day))
                {
                    return false;
                }
				return slug.Equals(Utils.RemoveIllegalCharacters(p.Slug), StringComparison.OrdinalIgnoreCase);
			});

			if (post != null)
			{
				if (url.Contains("/FEED/"))
				{
					context.RewritePath("syndication.axd?post=" + post.Id.ToString() + GetQueryString(context), false);
				}
				else
				{
					context.RewritePath(Utils.RelativeWebRoot + "post.aspx?id=" + post.Id.ToString() + GetQueryString(context), false);
				}
			}
		}

		private static void RewriteCategory(HttpContext context, string url)
		{
			string title = ExtractTitle(context, url);
			foreach (Category cat in Category.Categories)
			{
				string legalTitle = Utils.RemoveIllegalCharacters(cat.Title).ToLowerInvariant();
				if (title.Equals(legalTitle, StringComparison.OrdinalIgnoreCase))
				{
					if (url.Contains("/FEED/"))
					{
						context.RewritePath("syndication.axd?category=" + cat.Id.ToString() + GetQueryString(context), false);
					}
					else
					{
						context.RewritePath(Utils.RelativeWebRoot + "default.aspx?id=" + cat.Id.ToString() + GetQueryString(context), false);
						break;
					}
				}
			}
		}

		private static void RewritePage(HttpContext context, string url)
		{
			string slug = ExtractTitle(context, url);
			Page page = Page.Pages.Find(p => slug.Equals(Utils.RemoveIllegalCharacters(p.Slug), StringComparison.OrdinalIgnoreCase));

			if (page != null)
			{
				context.RewritePath(Utils.RelativeWebRoot + "page.aspx?id=" + page.Id + GetQueryString(context), false);
			}
		}

		private static readonly Regex YEAR = new Regex("/([0-9][0-9][0-9][0-9])/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex YEAR_MONTH = new Regex("/([0-9][0-9][0-9][0-9])/([0-1][0-9])/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex YEAR_MONTH_DAY = new Regex("/([0-9][0-9][0-9][0-9])/([0-1][0-9])/([0-3][0-9])/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static void RewriteDefault(HttpContext context)
		{
			string url = context.Request.RawUrl;
			string page = "&page=" + context.Request.QueryString["page"];
			if (string.IsNullOrEmpty(context.Request.QueryString["page"]))
				page = null;

            if (YEAR_MONTH_DAY.IsMatch(url)) {
                Match match = YEAR_MONTH_DAY.Match(url);
                string year = match.Groups[1].Value;
                string month = match.Groups[2].Value;
                string day = match.Groups[3].Value;
                string date = year + "-" + month + "-" + day;
                context.RewritePath(Utils.RelativeWebRoot + "default.aspx?date=" + date + page, false);
            } else if (YEAR_MONTH.IsMatch(url)) {
                Match match = YEAR_MONTH.Match(url);
                string year = match.Groups[1].Value;
                string month = match.Groups[2].Value;
                string path = string.Format("default.aspx?year={0}&month={1}", year, month);
                context.RewritePath(Utils.RelativeWebRoot + path + page, false);
            } else if (YEAR.IsMatch(url)) {
                Match match = YEAR.Match(url);
                string year = match.Groups[1].Value;
                string path = string.Format("default.aspx?year={0}", year);
                context.RewritePath(Utils.RelativeWebRoot + path + page, false);
            } else {
                context.RewritePath(url.Replace("Default.aspx", "default.aspx")); // fixes a casing oddity on Mono
            }
              
		}

		/// <summary>
		/// Extracts the title from the requested URL.
		/// </summary>
		private static string ExtractTitle(HttpContext context, string url)
		{
			url = url.ToLowerInvariant().Replace("---", "-");
			if (url.Contains(BlogSettings.Instance.FileExtension) && url.EndsWith("/"))
			{
				url = url.Substring(0, url.Length - 1);
				context.Response.AppendHeader("location", url);
				context.Response.StatusCode = 301;
			}

			url = url.Substring(0, url.IndexOf(BlogSettings.Instance.FileExtension));
			int index = url.LastIndexOf("/") + 1;
			string title = url.Substring(index);
			return context.Server.UrlEncode(title);
		}

		/// <summary>
		/// Extracts the year and month from the requested URL and returns that as a DateTime.
		/// </summary>
		private static DateTime ExtractDate(HttpContext context)
		{
			if (!BlogSettings.Instance.TimeStampPostLinks)
				return DateTime.MinValue;

			Match match = YEAR_MONTH_DAY.Match(context.Request.RawUrl);
			if (match.Success)
			{
				int year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				int month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				int day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
				return new DateTime(year, month, day);
			}

			match = YEAR_MONTH.Match(context.Request.RawUrl);
			if (match.Success)
			{
				int year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				int month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				return new DateTime(year, month, 1);
			}

			return DateTime.MinValue;
		}

		/// <summary>
		/// Gets the query string from the requested URL.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		private static string GetQueryString(HttpContext context)
		{
			string query = context.Request.QueryString.ToString();
			if (!string.IsNullOrEmpty(query))
				return "&" + query;

			return string.Empty;
		}
	}
}