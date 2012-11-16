#region Using

using System;
using System.IO;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.ComponentModel;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
	/// <summary>
	/// Recieves pingbacks from other blogs and websites, and 
	/// registers them as a comment.
	/// </summary>
	public class PingbackHandler : IHttpHandler
	{

		#region Private fields

		private static readonly Regex _Regex = new Regex(@"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex REGEX_HTML = new Regex(@"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", RegexOptions.Singleline | RegexOptions.Compiled);
		private const string SUCCESS = "<methodResponse><params><param><value><string>Thanks!</string></value></param></params></methodResponse>";

		private string _Title;
		private bool _SourceHasLink;
		private bool _ContainsHtml;

		#endregion

		/// <summary>
		/// Enables processing of HTTP Web requests by a custom HttpHandler that 
		/// implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpContext"></see> 
		/// object that provides references to the intrinsic server objects 
		/// (for example, Request, Response, Session, and Server) used to service HTTP requests.
		/// </param>
		public void ProcessRequest(HttpContext context)
		{
			if (!BlogSettings.Instance.IsCommentsEnabled || !BlogSettings.Instance.EnablePingBackReceive)
			{
				context.Response.StatusCode = 404;
				context.Response.End();
			}

			CancelEventArgs e = new CancelEventArgs();
			OnReceived(e);
			if (e.Cancel)
				return;

			XmlDocument doc = RetrieveXmlDocument(context);
			XmlNodeList list = doc.SelectNodes("methodCall/params/param/value/string");
			if (list == null)
				list = doc.SelectNodes("methodCall/params/param/value");

			string sourceUrl = list[0].InnerText.Trim();
			string targetUrl = list[1].InnerText.Trim();

			ExamineSourcePage(sourceUrl, targetUrl);
			context.Response.ContentType = "text/xml";

			Post post = GetPostByUrl(targetUrl);
			if (post != null)
			{
				if (IsFirstPingBack(post, sourceUrl))
				{
					if (_SourceHasLink && !_ContainsHtml)
					{
						AddComment(sourceUrl, post);
						OnAccepted(sourceUrl);
						context.Response.Write(SUCCESS);
					}
					else if (!_SourceHasLink)
					{
						SendError(context, 17, "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
					}
					else
					{
						OnSpammed(sourceUrl);
						// Don't let spammers know we exist.
						context.Response.StatusCode = 404;
					}
				}
				else
				{
					OnRejected(sourceUrl);
					SendError(context, 48, "The pingback has already been registered.");
				}
			}
			else
			{
				SendError(context, 32, "The specified target URI does not exist.");
			}
		}

		private static XmlDocument RetrieveXmlDocument(HttpContext context)
		{
			string html = string.Empty;
			string xml = ParseRequest(context);
			html = xml;
			if (!xml.Contains("<methodName>pingback.ping</methodName>"))
			{
				context.Response.StatusCode = 404;
				context.Response.End();
			}

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc;
		}

		private static void SendError(HttpContext context, int code, string message)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("<?xml version=\"1.0\"?>");
			sb.Append("<methodResponse>");
			sb.Append("<fault>");
			sb.Append("<value>");
			sb.Append("<struct>");
			sb.Append("<member>");
			sb.Append("<name>faultCode</name>");
			sb.AppendFormat("<value><int>{0}</int></value>", code);
			sb.Append("</member>");
			sb.Append("<member>");
			sb.Append("<name>faultString</name>");
			sb.AppendFormat("<value><string>{0}</string></value>", message);
			sb.Append("</member>");
			sb.Append("</struct>");
			sb.Append("</value>");
			sb.Append("</fault>");
			sb.Append("</methodResponse>");

			context.Response.Write(sb.ToString());
		}

		/// <summary>
		/// Insert the pingback as a comment on the post.
		/// </summary>
		private void AddComment(string sourceUrl, Post post)
		{
			Comment comment = new Comment();
			comment.Id = Guid.NewGuid();
			comment.Author = GetDomain(sourceUrl);
			comment.Website = new Uri(sourceUrl);
			comment.Content = "Pingback from " + comment.Author + Environment.NewLine + Environment.NewLine + _Title;
			comment.DateCreated = DateTime.Now;
			comment.Email = "pingback";
			comment.IP = HttpContext.Current.Request.UserHostAddress;
			comment.Parent = post;
			comment.IsApproved = true; //NOTE: Pingback comments are approved by default.
			post.AddComment(comment);
		}

		/// <summary>
		/// Retrieves the content of the input stream
		/// and return it as plain text.
		/// </summary>
		private static string ParseRequest(HttpContext context)
		{
			byte[] buffer = new byte[context.Request.InputStream.Length];
			context.Request.InputStream.Read(buffer, 0, buffer.Length);

			return System.Text.Encoding.Default.GetString(buffer);
		}

		/// <summary>
		/// Parse the source URL to get the domain.
		/// It is used to fill the Author property of the comment.
		/// </summary>
		private static string GetDomain(string sourceUrl)
		{
			int start = sourceUrl.IndexOf("://") + 3;
			int stop = sourceUrl.IndexOf("/", start);
			return sourceUrl.Substring(start, stop - start).Replace("www.", string.Empty);
		}

		/// <summary>
		/// Checks to see if the source has already pinged the target.
		/// If it has, there is no reason to add it again.
		/// </summary>
		private static bool IsFirstPingBack(Post post, string sourceUrl)
		{
			foreach (Comment comment in post.Comments)
			{
				if (comment.Website != null && comment.Website.ToString().Equals(sourceUrl, StringComparison.OrdinalIgnoreCase))
					return false;

				if (comment.IP != null && comment.IP == HttpContext.Current.Request.UserHostAddress)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Parse the HTML of the source page.
		/// </summary>
		private void ExamineSourcePage(string sourceUrl, string targetUrl)
		{
			try
			{
				using (WebClient client = new WebClient())
				{
					client.Credentials = CredentialCache.DefaultNetworkCredentials;
					string html = client.DownloadString(sourceUrl);
					_Title = _Regex.Match(html).Value.Trim();
					_ContainsHtml = REGEX_HTML.IsMatch(_Title);
					_SourceHasLink = html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());
				}
			}
			catch (WebException)
			{
				_SourceHasLink = false;
			}
		}

		/// <summary>
		/// Retrieve the post that belongs to the target URL.
		/// </summary>
		private static Post GetPostByUrl(string url)
		{
			int start = url.LastIndexOf("/") + 1;
			int stop = url.ToUpperInvariant().IndexOf(".ASPX");
			string name = url.Substring(start, stop - start).ToLowerInvariant();

			foreach (Post post in Post.Posts)
			{
				string legalTitle = Utils.RemoveIllegalCharacters(post.Title).ToLowerInvariant();
				if (name == legalTitle)
				{
					return post;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
		/// </summary>
		/// <value></value>
		/// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
		public bool IsReusable
		{
			get { return true; }
		}

		#region Events

		/// <summary>
		/// Occurs when a hit is made to the trackback.axd handler.
		/// </summary>
		public static event EventHandler<CancelEventArgs> Received;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnReceived(CancelEventArgs e)
		{
			if (Received != null)
			{
				Received(null, e);
			}
		}

		/// <summary>
		/// Occurs when a pingback is accepted as valid and added as a comment.
		/// </summary>
		public static event EventHandler<EventArgs> Accepted;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnAccepted(string url)
		{
			if (Accepted != null)
			{
				Accepted(url, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when a pingback request is rejected because the sending
		/// website already made a trackback or pingback to the specific page.
		/// </summary>
		public static event EventHandler<EventArgs> Rejected;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnRejected(string url)
		{
			if (Rejected != null)
			{
				Rejected(url, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when the request comes from a spammer.
		/// </summary>
		public static event EventHandler<EventArgs> Spammed;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		public static void OnSpammed(string url)
		{
			if (Spammed != null)
			{
				Spammed(url, EventArgs.Empty);
			}
		}

		#endregion

	}
}