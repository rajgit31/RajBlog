#region Using

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

#endregion

namespace BlogEngine.Core.Ping
{
	/// <summary>
	/// Manages to send out trackbacks and then pingbacks if trackbacks aren't supported by the linked site.
	/// </summary>
	public static class Manager
	{
		/// <summary>
		/// Sends the trackback or pingback message.
		/// <remarks>
		/// It will try to send a trackback message first, and if the refered web page
		/// doesn't support trackbacks, a pingback is sent.
		/// </remarks>
		/// </summary>
		public static void Send(IPublishable item, Uri itemUrl)
		{
			foreach (Uri url in GetUrlsFromContent(item.Content))
			{
                bool isTrackbackSent = false;

                if (BlogSettings.Instance.EnableTrackBackSend)
                { 
				    string pageContent = Utils.DownloadWebPage(url);// ReadFromWeb(url);
				    Uri trackbackUrl = GetTrackBackUrlFromPage(pageContent);

				    if (trackbackUrl != null)
				    {
					    TrackbackMessage message = new TrackbackMessage(item, trackbackUrl, itemUrl);
					    isTrackbackSent = Trackback.Send(message);
				    }
                }

                if (!isTrackbackSent && BlogSettings.Instance.EnablePingBackSend)
				{
					Pingback.Send(itemUrl, url);
				}
			}
		}

		#region "RegEx Methods"

		/// <summary>
		/// Regex used to find all hyperlinks.
		/// </summary>
		//private static readonly Regex urlsRegex = new Regex(@"\<a\s+href=""(http://.*?)"".*\>.+\<\/a\>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		//private static readonly Regex urlsRegex = new Regex(@"<a[^(href)]?href=""([^""]+)""[^>]?>([^<]+)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex urlsRegex = new System.Text.RegularExpressions.Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Regex used to find the trackback link on a remote web page.
		/// </summary>
		private static readonly Regex trackbackLinkRegex = new Regex("trackback:ping=\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Gets all the URLs from the specified string.
		/// </summary>
		private static List<Uri> GetUrlsFromContent(string content)
		{
			List<Uri> urlsList = new List<Uri>();
			foreach (Match myMatch in urlsRegex.Matches(content))
			{
                string url = myMatch.Groups["url"].ToString().Trim();
				Uri uri;
				if (Uri.TryCreate(url, UriKind.Absolute, out uri))
					urlsList.Add(uri);
			}

			return urlsList;
		}

		/// <summary>
		/// Examines the web page source code to retrieve the trackback link from the RDF.
		/// </summary>
		private static Uri GetTrackBackUrlFromPage(string input)
		{
			string url = trackbackLinkRegex.Match(input).Groups[1].ToString().Trim();
			Uri uri;

			if (Uri.TryCreate(url, UriKind.Absolute, out uri))
				return uri;
			else
				return null;
		}

		#endregion

		/// <summary>
		/// Returns the HTML code of a given URL.
		/// </summary>
		/// <param name="sourceUrl">The URL you want to extract the html code.</param>
		/// <returns></returns>
		private static string ReadFromWeb(Uri sourceUrl)
		{
			string html;
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
				html = client.DownloadString(sourceUrl);
			}
			return html;
		}
	}
}
