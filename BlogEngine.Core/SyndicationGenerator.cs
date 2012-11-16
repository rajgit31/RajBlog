/****************************************************************************
Modification History:
*****************************************************************************
Date		Author		Description
*****************************************************************************
08/29/2007	brian.kuhn	Created SyndicationGenerator Class
02/12/2008  rtur.net    
****************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Web;

namespace BlogEngine.Core
{
	/// <summary>
	/// Generates syndication feeds for blog entities.
	/// </summary>
	public class SyndicationGenerator
	{
		//============================================================
		//	PUBLIC/PRIVATE/PROTECTED MEMBERS
		//============================================================
		#region PRIVATE/PROTECTED/PUBLIC MEMBERS
		/// <summary>
		/// Private member to hold the name of the syndication generation utility.
		/// </summary>
		private const string GENERATOR_NAME = "BlogEngine.Net Syndication Generator";
		/// <summary>
		/// Private member to hold the URI of the syndication generation utility.
		/// </summary>
		private static readonly Uri GENERATOR_URI = new Uri("http://dotnetblogengine.net/");
		/// <summary>
		/// Private member to hold the version of the syndication generation utility.
		/// </summary>
		private static readonly Version GENERATOR_VERSION = new Version("1.0.0.0");
		/// <summary>
		/// Private member to hold the <see cref="BlogSettings"/> to use when generating syndication results.
		/// </summary>
		private BlogSettings blogSettings;
		/// <summary>
		/// Private member to hold a collection of <see cref="Category"/> objects used to categorize the web log content.
		/// </summary>
		private List<Category> blogCategories;
		/// <summary>
		/// Private member to hold a collection of the XML namespaces that define supported syndication extensions.
		/// </summary>
		private static Dictionary<string, string> xmlNamespaces;
		#endregion

		//============================================================
		//	CONSTRUCTORS
		//============================================================
		#region SyndicationGenerator(BlogSettings settings, List<Category> categories)
		/// <summary>
		/// Initializes a new instance of the <see cref="SyndicationGenerator"/> class using the supplied <see cref="BlogSettings"/> and collection of <see cref="Category"/> objects.
		/// </summary>
		/// <param name="settings">The <see cref="BlogSettings"/> to use when generating syndication results.</param>
		/// <param name="categories">A collection of <see cref="Category"/> objects used to categorize the web log content.</param>
		public SyndicationGenerator(BlogSettings settings, List<Category> categories)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			if (categories == null)
			{
				throw new ArgumentNullException("categories");
			}

			//------------------------------------------------------------
			//	Initialize class state
			//------------------------------------------------------------
			this.Settings = settings;

			if (categories.Count > 0)
			{
				Category[] values = new Category[categories.Count];
				categories.CopyTo(values);

				foreach (Category category in values)
				{
					this.Categories.Add(category);
				}
			}
		}
		#endregion

		//============================================================
		//	PUBLIC PROPERTIES
		//============================================================
		#region Categories
		/// <summary>
		/// Gets a collection of <see cref="Category"/> objects used to categorize the web log content.
		/// </summary>
		/// <value>A collection of <see cref="Category"/> objects used to categorize the web log content.</value>
		public List<Category> Categories
		{
			get
			{
				if (blogCategories == null)
				{
					blogCategories = new List<Category>();
				}

				return blogCategories;
			}
		}
		#endregion

		#region SupportedNamespaces
		/// <summary>
		/// Gets a collection of the XML namespaces used to provide support for syndication extensions.
		/// </summary>
		/// <value>The collection of the XML namespaces, keyed by namespace prefix, that are used to provide support for syndication extensions.</value>
		public static Dictionary<string, string> SupportedNamespaces
		{
			get
			{
				if (xmlNamespaces == null)
				{
					xmlNamespaces = new Dictionary<string, string>();

					xmlNamespaces.Add("blogChannel", "http://backend.userland.com/blogChannelModule");
					xmlNamespaces.Add("dc", "http://purl.org/dc/elements/1.1/");

					xmlNamespaces.Add("pingback", "http://madskills.com/public/xml/rss/module/pingback/");
					xmlNamespaces.Add("trackback", "http://madskills.com/public/xml/rss/module/trackback/");
					xmlNamespaces.Add("wfw", "http://wellformedweb.org/CommentAPI/");
					xmlNamespaces.Add("slash", "http://purl.org/rss/1.0/modules/slash/");
					xmlNamespaces.Add("geo", "http://www.w3.org/2003/01/geo/wgs84_pos#");
					//xmlNamespaces.Add("atom", "http://www.w3.org/2005/Atom");
				}

				return xmlNamespaces;
			}
		}
		#endregion

		#region Settings
		/// <summary>
		/// Gets or sets the <see cref="BlogSettings"/> used when generating syndication results.
		/// </summary>
		/// <value>The <see cref="BlogSettings"/> used when generating syndication results.</value>
		/// <exception cref="ArgumentNullException">The <paramref name="value"/> is a null reference (Nothing in Visual Basic).</exception>
		public BlogSettings Settings
		{
			get
			{
				return blogSettings;
			}

			protected set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				else
				{
					blogSettings = value;
				}
			}
		}
		#endregion

		//============================================================
		//	STATIC UTILITY METHODS
		//============================================================
		#region FormatW3cOffset(TimeSpan offset, string separator)
		/// <summary>
		/// Converts the value of the specified <see cref="TimeSpan"/> to its equivalent string representation.
		/// </summary>
		/// <param name="offset">The <see cref="TimeSpan"/> to convert.</param>
		/// <param name="separator">Separator used to deliminate hours and minutes.</param>
		/// <returns>A string representation of the TimeSpan.</returns>
		private static string FormatW3cOffset(TimeSpan offset, string separator)
		{
			string formattedOffset = String.Empty;

			if (offset >= TimeSpan.Zero)
			{
				formattedOffset = "+";
			}

			return String.Concat(formattedOffset, offset.Hours.ToString("00", CultureInfo.InvariantCulture), separator, offset.Minutes.ToString("00", CultureInfo.InvariantCulture));
		}
		#endregion

		#region GetPermaLink(IPublishable publishable)
		/// <summary>
		/// Creates a <see cref="Uri"/> that represents the peramlink for the supplied <see cref="IPublishable"/>.
		/// </summary>
		/// <param name="publishable">The <see cref="IPublishable"/> used to generate the permalink for.</param>
		/// <returns>A <see cref="Uri"/> that represents the peramlink for the supplied <see cref="IPublishable"/>.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="publishable"/> is a null reference (Nothing in Visual Basic).</exception>
		private static Uri GetPermaLink(IPublishable publishable)
		{
			Post post = publishable as Post;
			if (post != null)
			{
				return post.PermaLink;
			}

			return publishable.AbsoluteLink;
		}
		#endregion

		#region ToRfc822DateTime(DateTime dateTime)
		/// <summary>
		/// Converts the supplied <see cref="DateTime"/> to its equivalent <a href="http://asg.web.cmu.edu/rfc/rfc822.html">RFC-822 DateTime</a> string representation.
		/// </summary>
		/// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
		/// <returns>The equivalent <a href="http://asg.web.cmu.edu/rfc/rfc822.html">RFC-822 DateTime</a> string representation.</returns>
		public static string ToRfc822DateTime(DateTime dateTime)
		{
			int offset = (int)(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours + BlogSettings.Instance.Timezone);
			string timeZone = "+" + offset.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0');

			//------------------------------------------------------------
			//	Adjust time zone based on offset
			//------------------------------------------------------------
			if (offset < 0)
			{
				int i = offset * -1;
				timeZone = "-" + i.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0');

			}

			return dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'), DateTimeFormatInfo.InvariantInfo);
		}
		#endregion

		#region ToW3CDateTime(DateTime utcDateTime)
		/// <summary>
		/// Converts the supplied <see cref="DateTime"/> to its equivalent <a href="http://www.w3.org/TR/NOTE-datetime">W3C DateTime</a> string representation.
		/// </summary>
		/// <param name="utcDateTime">The Coordinated Universal Time (UTC) <see cref="DateTime"/> to convert.</param>
		/// <returns>The equivalent <a href="http://www.w3.org/TR/NOTE-datetime">W3C DateTime</a> string representation.</returns>
		private static string ToW3CDateTime(DateTime utcDateTime)
		{
			TimeSpan utcOffset = TimeSpan.Zero;
			return (utcDateTime + utcOffset).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + SyndicationGenerator.FormatW3cOffset(utcOffset, ":");
		}
		#endregion

		#region ConvertPathsToAbsolute(string content)
		/// <summary>
		/// Converts all relative paths in the spcified content to absolute.
		/// </summary>
		private static string ConvertPathsToAbsolute(string content)
		{
			content = content.Replace("\"" + Utils.AbsoluteWebRoot.AbsolutePath + "image.axd", "\"" + Utils.AbsoluteWebRoot + "image.axd");
			content = content.Replace("\"" + Utils.AbsoluteWebRoot.AbsolutePath + "file.axd", "\"" + Utils.AbsoluteWebRoot + "file.axd");
			content = content.Replace("href=\"" + Utils.RelativeWebRoot + "", "href=\"" + Utils.AbsoluteWebRoot);
			
			if (HttpContext.Current != null)
			{
				Uri url = HttpContext.Current.Request.Url;
				content = content.Replace("href=\"/", "href=\"" + url.Scheme + "://" + url.Authority + "/");
			}

			return content;
		}
		#endregion

		//============================================================
		//	PUBLIC METHODS
		//============================================================
		#region WriteFeed(SyndicationFormat format, Stream stream, List<IPublishable> publishables)
		/// <summary>
		/// Writes a generated syndication feed that conforms to the supplied <see cref="SyndicationFormat"/> using the supplied <see cref="Stream"/> and collection.
		/// </summary>
		/// <param name="format">A <see cref="SyndicationFormat"/> enumeration value indicating the syndication format to generate.</param>
		/// <param name="stream">The <see cref="Stream"/> to which you want to write the syndication feed.</param>
		/// <param name="publishables">The collection of <see cref="IPublishable"/> objects used to generate the syndication feed content.</param>
		/// <param name="title">The title of the RSS channel</param>
		public void WriteFeed(SyndicationFormat format, Stream stream, List<IPublishable> publishables, string title)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (publishables == null)
			{
				throw new ArgumentNullException("publishables");
			}
			if (!stream.CanWrite)
			{
				throw new ArgumentException(String.Format(null, "Unable to generate {0} syndication feed. The provided stream does not support writing.", format), "stream");
			}

			//------------------------------------------------------------
			//	Write syndication feed based on specified format
			//------------------------------------------------------------
			switch (format)
			{
				case SyndicationFormat.Atom:
					this.WriteAtomFeed(stream, publishables, title);
					break;

				case SyndicationFormat.Rss:
					this.WriteRssFeed(stream, publishables, title);
					break;
			}
		}
		#endregion

		//============================================================
		//	SYNDICATION WRITE METHODS
		//============================================================
		#region WriteAtomFeed(Stream stream, List<IPublishable> publishables)
		/// <summary>
		/// Writes a generated Atom syndication feed to the specified <see cref="Stream"/> using the supplied collection.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to which you want to write the syndication feed.</param>
		/// <param name="publishables">The collection of <see cref="IPublishable"/> objects used to generate the syndication feed content.</param>
		/// <param name="title">The title of the ATOM content.</param>
		private void WriteAtomFeed(Stream stream, List<IPublishable> publishables, string title)
		{
			XmlWriterSettings writerSettings = new XmlWriterSettings();
			writerSettings.Encoding = System.Text.Encoding.UTF8;
			writerSettings.Indent = true;

			//------------------------------------------------------------
			//	Create writer against stream using defined settings
			//------------------------------------------------------------
			using (XmlWriter writer = XmlWriter.Create(stream, writerSettings))
			{
				writer.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
				//writer.WriteAttributeString("version", "1.0");

				//------------------------------------------------------------
				//	Write XML namespaces used to support syndication extensions
				//------------------------------------------------------------
				foreach (string prefix in SyndicationGenerator.SupportedNamespaces.Keys)
				{
					writer.WriteAttributeString("xmlns", prefix, null, SyndicationGenerator.SupportedNamespaces[prefix]);
				}

				//------------------------------------------------------------
				//	Write feed content
				//------------------------------------------------------------
				this.WriteAtomContent(writer, publishables, title);

				writer.WriteFullEndElement();
			}
		}
		#endregion

		#region WriteRssFeed(Stream stream, List<IPublishable> publishables)
		/// <summary>
		/// Writes a generated RSS syndication feed to the specified <see cref="Stream"/> using the supplied collection.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to which you want to write the syndication feed.</param>
		/// <param name="publishables">The collection of <see cref="IPublishable"/> objects used to generate the syndication feed content.</param>
		/// <param name="title">The title of the RSS channel.</param>
		private void WriteRssFeed(Stream stream, List<IPublishable> publishables, string title)
		{
			XmlWriterSettings writerSettings = new XmlWriterSettings();
			writerSettings.Encoding = System.Text.Encoding.UTF8;
			writerSettings.Indent = true;

			//------------------------------------------------------------
			//	Create writer against stream using defined settings
			//------------------------------------------------------------
			using (XmlWriter writer = XmlWriter.Create(stream, writerSettings))
			{
				writer.WriteStartElement("rss");
				writer.WriteAttributeString("version", "2.0");

				//------------------------------------------------------------
				//	Write XML namespaces used to support syndication extensions
				//------------------------------------------------------------
				foreach (string prefix in SyndicationGenerator.SupportedNamespaces.Keys)
				{
					writer.WriteAttributeString("xmlns", prefix, null, SyndicationGenerator.SupportedNamespaces[prefix]);
				}

				//------------------------------------------------------------
				//	Write <channel> element
				//------------------------------------------------------------
				this.WriteRssChannel(writer, publishables, title);

				writer.WriteFullEndElement();
			}
		}
		#endregion

		//============================================================
		//	PRIVATE RSS METHODS
		//============================================================
		#region WriteRssChannel(XmlWriter writer, List<IPublishable> publishables)
		/// <summary>
		/// Writes the RSS channel element information to the specified <see cref="XmlWriter"/> using the supplied collection.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write channel element information to.</param>
		/// <param name="publishables">The collection of <see cref="IPublishable"/> objects used to generate syndication content.</param>
		/// <param name="title">The title of the RSS channel.</param>
		private void WriteRssChannel(XmlWriter writer, List<IPublishable> publishables, string title)
		{
			writer.WriteStartElement("channel");

			writer.WriteElementString("title", title);
			writer.WriteElementString("description", this.Settings.Description);
			writer.WriteElementString("link", Utils.AbsoluteWebRoot.ToString());

			//------------------------------------------------------------
			//	Write common/shared channel elements
			//------------------------------------------------------------
			this.WriteRssChannelCommonElements(writer);

			foreach (IPublishable publishable in publishables)
			{
				if (publishable.IsVisible)
				{
					WriteRssItem(writer, publishable);
				}
			}

			writer.WriteEndElement();
		}
		#endregion

		#region WriteRssChannelCommonElements(XmlWriter writer)
		/// <summary>
		/// Writes the common/shared RSS channel element information to the specified <see cref="XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write channel element information to.</param>
		private void WriteRssChannelCommonElements(XmlWriter writer)
		{
			//------------------------------------------------------------
			//	Write optional channel elements
			//------------------------------------------------------------
			string url = Utils.FeedUrl;
			if (System.Web.HttpContext.Current != null)
				url = System.Web.HttpContext.Current.Request.Url.ToString();

			writer.WriteElementString("docs", "http://www.rssboard.org/rss-specification");
			writer.WriteElementString("generator", "BlogEngine.NET " + BlogSettings.Instance.Version());
			//writer.WriteRaw("\n<atom:link href=\"" + url + "\" rel=\"self\" type=\"application/rss+xml\" />");
			if (!String.IsNullOrEmpty(this.Settings.Language))
			{
				writer.WriteElementString("language", this.Settings.Language);
			}

			//------------------------------------------------------------
			//	Write blogChannel syndication extension elements
			//------------------------------------------------------------
			Uri blogRoll;
			if (Uri.TryCreate(String.Concat(Utils.AbsoluteWebRoot.ToString().TrimEnd('/'), "/opml.axd"), UriKind.RelativeOrAbsolute, out blogRoll))
			{
				writer.WriteElementString("blogChannel", "blogRoll", "http://backend.userland.com/blogChannelModule", blogRoll.ToString());
			}

			if (!String.IsNullOrEmpty(this.Settings.Endorsement))
			{
				Uri blink;
				if (Uri.TryCreate(this.Settings.Endorsement, UriKind.RelativeOrAbsolute, out blink))
				{
					writer.WriteElementString("blogChannel", "blink", "http://backend.userland.com/blogChannelModule", blink.ToString());
				}
			}

			//------------------------------------------------------------
			//	Write Dublin Core syndication extension elements
			//------------------------------------------------------------
			if (!String.IsNullOrEmpty(this.Settings.AuthorName))
			{
				writer.WriteElementString("dc", "creator", "http://purl.org/dc/elements/1.1/", this.Settings.AuthorName);
			}
			//if (!String.IsNullOrEmpty(this.Settings.Description))
			//{
			//  writer.WriteElementString("dc", "description", "http://purl.org/dc/elements/1.1/", this.Settings.Description);
			//}
			if (!String.IsNullOrEmpty(this.Settings.Name))
			{
				writer.WriteElementString("dc", "title", "http://purl.org/dc/elements/1.1/", this.Settings.Name);
			}

			//------------------------------------------------------------
			//	Write basic geo-coding syndication extension elements
			//------------------------------------------------------------
			NumberFormatInfo decimalFormatInfo = new NumberFormatInfo();
			decimalFormatInfo.NumberDecimalDigits = 6;

			if (this.Settings.GeocodingLatitude != Single.MinValue)
			{
				writer.WriteElementString("geo", "lat", "http://www.w3.org/2003/01/geo/wgs84_pos#", this.Settings.GeocodingLatitude.ToString("N", decimalFormatInfo));
			}
			if (this.Settings.GeocodingLongitude != Single.MinValue)
			{
				writer.WriteElementString("geo", "long", "http://www.w3.org/2003/01/geo/wgs84_pos#", this.Settings.GeocodingLongitude.ToString("N", decimalFormatInfo));
			}
		}
		#endregion

		#region WriteRssItem(XmlWriter writer, IPublishable publishable)
		/// <summary>
		/// Writes the RSS channel item element information to the specified <see cref="XmlWriter"/> using the supplied <see cref="Page"/>.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write channel item element information to.</param>
		/// <param name="publishable">The <see cref="IPublishable"/> used to generate channel item content.</param>
		private static void WriteRssItem(XmlWriter writer, IPublishable publishable)
		{
			//------------------------------------------------------------
			//	Cast IPublishable as Post to support comments/trackback
			//------------------------------------------------------------
			Post post = publishable as Post;
			Comment comment = publishable as Comment;

			//------------------------------------------------------------
			//	Raise serving event
			//------------------------------------------------------------                
			ServingEventArgs arg = new ServingEventArgs(publishable.Content, ServingLocation.Feed);
			publishable.OnServing(arg);
			if (arg.Cancel)
			{
				return;
			}

			//------------------------------------------------------------
			//	Modify post content to make references absolute
			//------------------------------------------------------------    
			string content = ConvertPathsToAbsolute(arg.Body);

			if (comment != null)
				content = content.Replace(Environment.NewLine, "<br />");

			writer.WriteStartElement("item");
			//------------------------------------------------------------
			//	Write required channel item elements
			//------------------------------------------------------------
			writer.WriteElementString("title", publishable.Title);
			writer.WriteElementString("description", content);
			writer.WriteElementString("link", Utils.ConvertToAbsolute(publishable.RelativeLink).ToString());

      //------------------------------------------------------------
      //	Write enclosure tag for podcasting support
      //------------------------------------------------------------
      if (BlogSettings.Instance.EnableEnclosures)
      {
        string encloser = GetEnclosure(content);
        if (!string.IsNullOrEmpty(encloser))
          writer.WriteRaw(encloser);
      }

			//------------------------------------------------------------
			//	Write optional channel item elements
			//------------------------------------------------------------
			writer.WriteElementString("author", publishable.Author);
			if (post != null)
			{
				writer.WriteElementString("comments", String.Concat(Utils.ConvertToAbsolute(publishable.RelativeLink).ToString(), "#comment"));
			}
			writer.WriteElementString("guid", SyndicationGenerator.GetPermaLink(publishable).ToString());
			writer.WriteElementString("pubDate", SyndicationGenerator.ToRfc822DateTime(publishable.DateCreated));

			//------------------------------------------------------------
			//	Write channel item category elements
			//------------------------------------------------------------
			if (publishable.Categories != null)
			{
				foreach (Category category in publishable.Categories)
				{
					writer.WriteElementString("category", category.Title);
				}
			}

			//------------------------------------------------------------
			//	Write Dublin Core syndication extension elements
			//------------------------------------------------------------
			if (!String.IsNullOrEmpty(publishable.Author))
			{
				writer.WriteElementString("dc", "publisher", "http://purl.org/dc/elements/1.1/", publishable.Author);
			}
			//if (!String.IsNullOrEmpty(publishable.Description))
			//{
			//	writer.WriteElementString("dc", "description", "http://purl.org/dc/elements/1.1/", publishable.Description);
			//}

			//------------------------------------------------------------
			//	Write pingback syndication extension elements
			//------------------------------------------------------------
			Uri pingbackServer;
			if (Uri.TryCreate(String.Concat(Utils.AbsoluteWebRoot.ToString().TrimEnd('/'), "/pingback.axd"), UriKind.RelativeOrAbsolute, out pingbackServer))
			{
				writer.WriteElementString("pingback", "server", "http://madskills.com/public/xml/rss/module/pingback/", pingbackServer.ToString());
				writer.WriteElementString("pingback", "target", "http://madskills.com/public/xml/rss/module/pingback/", SyndicationGenerator.GetPermaLink(publishable).ToString());
			}

			//------------------------------------------------------------
			//	Write slash syndication extension elements
			//------------------------------------------------------------
			if (post != null && post.Comments != null)
			{
				writer.WriteElementString("slash", "comments", "http://purl.org/rss/1.0/modules/slash/", post.Comments.Count.ToString(CultureInfo.InvariantCulture));
			}

			//------------------------------------------------------------
			//	Write trackback syndication extension elements
			//------------------------------------------------------------
			if (post != null && post.TrackbackLink != null)
			{
				writer.WriteElementString("trackback", "ping", "http://madskills.com/public/xml/rss/module/trackback/", post.TrackbackLink.ToString());
			}

			//------------------------------------------------------------
			//	Write well-formed web syndication extension elements
			//------------------------------------------------------------
			writer.WriteElementString("wfw", "comment", "http://wellformedweb.org/CommentAPI/", String.Concat(Utils.ConvertToAbsolute(publishable.RelativeLink).ToString(), "#comment"));
			writer.WriteElementString("wfw", "commentRss", "http://wellformedweb.org/CommentAPI/", Utils.AbsoluteWebRoot.ToString().TrimEnd('/') + "/syndication.axd?post=" + publishable.Id.ToString());

			//------------------------------------------------------------
			//	Write </item> element
			//------------------------------------------------------------
			writer.WriteEndElement();
		}
		#endregion

		//============================================================
		//	PRIVATE ATOM METHODS
		//============================================================
		#region WriteAtomContent(XmlWriter writer, List<IPublishable> publishables)
		/// <summary>
		/// Writes the Atom feed element information to the specified <see cref="XmlWriter"/> using the supplied collection.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write channel element information to.</param>
		/// <param name="publishables">The collection of <see cref="IPublishable"/> objects used to generate syndication content.</param>
		/// <param name="title">The title of the ATOM content.</param>
		private void WriteAtomContent(XmlWriter writer, List<IPublishable> publishables, string title)
		{
			//------------------------------------------------------------
			//	Write required feed elements
			//------------------------------------------------------------
			writer.WriteElementString("id", Utils.AbsoluteWebRoot.ToString());
			writer.WriteElementString("title", title);
			writer.WriteElementString("updated", (publishables.Count > 0) ? SyndicationGenerator.ToW3CDateTime(publishables[0].DateModified.ToUniversalTime()) : SyndicationGenerator.ToW3CDateTime(DateTime.UtcNow));

			//------------------------------------------------------------
			//	Write recommended feed elements
			//------------------------------------------------------------
			writer.WriteStartElement("link");
			writer.WriteAttributeString("href", Utils.AbsoluteWebRoot.ToString());
			writer.WriteEndElement();

			writer.WriteStartElement("link");
			writer.WriteAttributeString("rel", "self");
			writer.WriteAttributeString("href", Utils.AbsoluteWebRoot + "syndication.axd?format=atom");
			writer.WriteEndElement();

			//writer.WriteStartElement("link");
			//writer.WriteAttributeString("rel", "alternate");
			//writer.WriteAttributeString("href", Utils.FeedUrl.ToString());
			//writer.WriteEndElement();

			//------------------------------------------------------------
			//	Write optional feed elements
			//------------------------------------------------------------
			writer.WriteElementString("subtitle", this.Settings.Description);

			//------------------------------------------------------------
			//	Write common/shared feed elements
			//------------------------------------------------------------
			this.WriteAtomContentCommonElements(writer);

			foreach (IPublishable publishable in publishables)
			{
				//------------------------------------------------------------
				//	Skip publishable content if it is not visible
				//------------------------------------------------------------
				if (!publishable.IsVisible)
				{
					continue;
				}

				//------------------------------------------------------------
				//	Write <entry> element for publishable content
				//------------------------------------------------------------
				WriteAtomEntry(writer, publishable);
			}
		}
		#endregion

		#region WriteAtomContentCommonElements(XmlWriter writer)
		/// <summary>
		/// Writes the common/shared Atom feed element information to the specified <see cref="XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write channel element information to.</param>
		private void WriteAtomContentCommonElements(XmlWriter writer)
		{
			//------------------------------------------------------------
			//	Write optional feed elements
			//------------------------------------------------------------
			writer.WriteStartElement("author");
			writer.WriteElementString("name", this.Settings.AuthorName);
			writer.WriteEndElement();

			writer.WriteStartElement("generator");
			writer.WriteAttributeString("uri", GENERATOR_URI.ToString());
			writer.WriteAttributeString("version", GENERATOR_VERSION.ToString());
			writer.WriteString(GENERATOR_NAME);
			writer.WriteEndElement();

			//------------------------------------------------------------
			//	Write blogChannel syndication extension elements
			//------------------------------------------------------------
			Uri blogRoll;
			if (Uri.TryCreate(String.Concat(Utils.AbsoluteWebRoot.ToString().TrimEnd('/'), "/opml.axd"), UriKind.RelativeOrAbsolute, out blogRoll))
			{
				writer.WriteElementString("blogChannel", "blogRoll", "http://backend.userland.com/blogChannelModule", blogRoll.ToString());
			}

			if (!String.IsNullOrEmpty(this.Settings.Endorsement))
			{
				Uri blink;
				if (Uri.TryCreate(this.Settings.Endorsement, UriKind.RelativeOrAbsolute, out blink))
				{
					writer.WriteElementString("blogChannel", "blink", "http://backend.userland.com/blogChannelModule", blink.ToString());
				}
			}

			//------------------------------------------------------------
			//	Write Dublin Core syndication extension elements
			//------------------------------------------------------------
			if (!String.IsNullOrEmpty(this.Settings.AuthorName))
			{
				writer.WriteElementString("dc", "creator", "http://purl.org/dc/elements/1.1/", this.Settings.AuthorName);
			}
			if (!String.IsNullOrEmpty(this.Settings.Description))
			{
				writer.WriteElementString("dc", "description", "http://purl.org/dc/elements/1.1/", this.Settings.Description);
			}
			if (!String.IsNullOrEmpty(this.Settings.Language))
			{
				writer.WriteElementString("dc", "language", "http://purl.org/dc/elements/1.1/", this.Settings.Language);
			}
			if (!String.IsNullOrEmpty(this.Settings.Name))
			{
				writer.WriteElementString("dc", "title", "http://purl.org/dc/elements/1.1/", this.Settings.Name);
			}

			//------------------------------------------------------------
			//	Write basic geo-coding syndication extension elements
			//------------------------------------------------------------
			NumberFormatInfo decimalFormatInfo = new NumberFormatInfo();
			decimalFormatInfo.NumberDecimalDigits = 6;

			if (this.Settings.GeocodingLatitude != Single.MinValue)
			{
				writer.WriteElementString("geo", "lat", "http://www.w3.org/2003/01/geo/wgs84_pos#", this.Settings.GeocodingLatitude.ToString("N", decimalFormatInfo));
			}
			if (this.Settings.GeocodingLongitude != Single.MinValue)
			{
				writer.WriteElementString("geo", "long", "http://www.w3.org/2003/01/geo/wgs84_pos#", this.Settings.GeocodingLongitude.ToString("N", decimalFormatInfo));
			}
		}
		#endregion

		#region WriteAtomEntry(XmlWriter writer, IPublishable publishable)
		/// <summary>
		/// Writes the Atom feed entry element information to the specified <see cref="XmlWriter"/> using the supplied <see cref="Page"/>.
		/// </summary>
		/// <param name="writer">The <see cref="XmlWriter"/> to write feed entry element information to.</param>
		/// <param name="publishable">The <see cref="IPublishable"/> used to generate feed entry content.</param>
		private static void WriteAtomEntry(XmlWriter writer, IPublishable publishable)
		{
			Post post = publishable as Post;
			Comment comment = publishable as Comment;

			//------------------------------------------------------------
			//	Raise serving event
			//------------------------------------------------------------                
			ServingEventArgs arg = new ServingEventArgs(publishable.Content, ServingLocation.Feed);
			publishable.OnServing(arg);
			if (arg.Cancel)
			{
				return;
			}

			//------------------------------------------------------------
			//	Modify publishable content to make references absolute
			//------------------------------------------------------------
			string content = ConvertPathsToAbsolute(arg.Body);

			writer.WriteStartElement("entry");
			//------------------------------------------------------------
			//	Write required entry elements
			//------------------------------------------------------------
			writer.WriteElementString("id", Utils.ConvertToAbsolute(publishable.RelativeLink).ToString());
			writer.WriteElementString("title", publishable.Title);
			writer.WriteElementString("updated", SyndicationGenerator.ToW3CDateTime(publishable.DateCreated.ToUniversalTime()));

			//------------------------------------------------------------
			//	Write recommended entry elements
			//------------------------------------------------------------
			writer.WriteStartElement("link");
			writer.WriteAttributeString("rel", "self");
			writer.WriteAttributeString("href", SyndicationGenerator.GetPermaLink(publishable).ToString());
			writer.WriteEndElement();

			writer.WriteStartElement("link");
			writer.WriteAttributeString("href", Utils.ConvertToAbsolute(publishable.RelativeLink).ToString());
			writer.WriteEndElement();

			writer.WriteStartElement("author");
			writer.WriteElementString("name", publishable.Author);
			writer.WriteEndElement();

			writer.WriteStartElement("summary");
			writer.WriteAttributeString("type", "html");
			writer.WriteString(content);
			writer.WriteEndElement();

			//------------------------------------------------------------
			//	Write optional entry elements
			//------------------------------------------------------------
			writer.WriteElementString("published", SyndicationGenerator.ToW3CDateTime(publishable.DateCreated.ToUniversalTime()));

			writer.WriteStartElement("link");
			writer.WriteAttributeString("rel", "related");
			writer.WriteAttributeString("href", String.Concat(Utils.ConvertToAbsolute(publishable.RelativeLink).ToString(), "#comment"));
			writer.WriteEndElement();

      //------------------------------------------------------------
      //	Write enclosure tag for podcasting support
      //------------------------------------------------------------
      if (BlogSettings.Instance.EnableEnclosures)
      {
        string encloser = GetEnclosure(content);
        if (!string.IsNullOrEmpty(encloser))
          writer.WriteRaw(encloser);
      }

			//------------------------------------------------------------
			//	Write entry category elements
			//------------------------------------------------------------
			if (publishable.Categories != null)
			{
				foreach (Category category in publishable.Categories)
				{
					writer.WriteStartElement("category");
					writer.WriteAttributeString("term", category.Title);
					writer.WriteEndElement();
				}
			}

			//------------------------------------------------------------
			//	Write Dublin Core syndication extension elements
			//------------------------------------------------------------
			if (!String.IsNullOrEmpty(publishable.Author))
			{
				writer.WriteElementString("dc", "publisher", "http://purl.org/dc/elements/1.1/", publishable.Author);
			}
			if (!String.IsNullOrEmpty(publishable.Description))
			{
				writer.WriteElementString("dc", "description", "http://purl.org/dc/elements/1.1/", publishable.Description);
			}

			//------------------------------------------------------------
			//	Write pingback syndication extension elements
			//------------------------------------------------------------
			Uri pingbackServer;
			if (Uri.TryCreate(String.Concat(Utils.AbsoluteWebRoot.ToString().TrimEnd('/'), "/pingback.axd"), UriKind.RelativeOrAbsolute, out pingbackServer))
			{
				writer.WriteElementString("pingback", "server", "http://madskills.com/public/xml/rss/module/pingback/", pingbackServer.ToString());
				writer.WriteElementString("pingback", "target", "http://madskills.com/public/xml/rss/module/pingback/", SyndicationGenerator.GetPermaLink(publishable).ToString());
			}

			//------------------------------------------------------------
			//	Write slash syndication extension elements
			//------------------------------------------------------------
			if (post != null && post.Comments != null)
			{
				writer.WriteElementString("slash", "comments", "http://purl.org/rss/1.0/modules/slash/", post.Comments.Count.ToString(CultureInfo.InvariantCulture));
			}

			//------------------------------------------------------------
			//	Write trackback syndication extension elements
			//------------------------------------------------------------
			if (post != null && post.TrackbackLink != null)
			{
				writer.WriteElementString("trackback", "ping", "http://madskills.com/public/xml/rss/module/trackback/", post.TrackbackLink.ToString());
			}

			//------------------------------------------------------------
			//	Write well-formed web syndication extension elements
			//------------------------------------------------------------
			writer.WriteElementString("wfw", "comment", "http://wellformedweb.org/CommentAPI/", String.Concat(Utils.ConvertToAbsolute(publishable.RelativeLink).ToString(), "#comment"));
			writer.WriteElementString("wfw", "commentRss", "http://wellformedweb.org/CommentAPI/", Utils.AbsoluteWebRoot.ToString().TrimEnd('/') + "/syndication.axd?post=" + publishable.Id.ToString());

			//------------------------------------------------------------
			//	Write </entry> element
			//------------------------------------------------------------
			writer.WriteEndElement();
		}
		#endregion

    #region Enclosure support
    private static long _fileSize = 0;
    private static bool _fileExists = false;
    //------------------------------------------------------------
    //      builds enclosure tag for podcast if post has media file
    //------------------------------------------------------------
    private static string GetEnclosure(string content)
    {
      string enclosure = string.Empty;
      _fileSize = 0;
      _fileExists = false;

      foreach (KeyValuePair<string, string> media in SupportedMedia)
      {
        enclosure = GetMediaEnclosure(content, media.Key, media.Value);
        if (enclosure.Length > 0)
          break;
      }
      return enclosure;
    }
    //------------------------------------------------------------
    //      get enclosure for supported media type
    //------------------------------------------------------------
    private static string GetMediaEnclosure(string content, string media, string mediatype)
    {
      string regex = @"<a href=((.|\n)*?)>((.|\n)*?)</a>";
      string enclosure = "<enclosure url=\"{0}\" length=\"{1}\" type=\"{2}\" />";
      MatchCollection matches = Regex.Matches(content, regex);

      if (matches.Count > 0)
      {
        string filename = string.Empty;

        foreach (Match match in matches)
        {
          if (match.Value.Contains(media))
          {
            filename = match.Value.Substring(match.Value.IndexOf("http"));
            filename = filename.Substring(0, filename.IndexOf(">")).Replace("\"", "").Trim();
            filename = ValidateFileName(filename);

            if (_fileExists)
            {
              enclosure = string.Format(enclosure, filename, _fileSize, mediatype);
              return enclosure;
            }
          }
        }
      }
      return string.Empty;
    }
    //------------------------------------------------------------
    // returns validated name of the media file
    // this file has to be "local" - must exist on the server
    //------------------------------------------------------------
    private static string ValidateFileName(string fileName)
    {
      fileName = fileName.Replace(Utils.AbsoluteWebRoot.ToString(), "");

      try
      {
        string phisicalPath = HttpContext.Current.Server.MapPath(fileName);
        FileInfo info = new FileInfo(phisicalPath);
        _fileSize = info.Length;
        _fileExists = true;
      }
      catch (Exception)
      {
        // if file does not exist - try to strip down leading
        // directory in the path; sometimes it duplicated
        if (fileName.IndexOf("/") > 0)
        {
          fileName = fileName.Substring(fileName.IndexOf("/") + 1);
          ValidateFileName(fileName);
        }
      }

      return Utils.AbsoluteWebRoot + fileName;
    }
    //------------------------------------------------------------
    //      all media formats that support podcasting
    //------------------------------------------------------------
    private static Dictionary<string, string> SupportedMedia
    {
      get
      {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        dic.Add(".mp3", "audio/mpeg");
        dic.Add(".m4a3", "audio/x-m4a");
        dic.Add(".mp4", "video/mp4");
        dic.Add(".m4v", "video/x-m4v");
        dic.Add(".mov", "video/quicktime");
        dic.Add(".pdf", "application/pdf");
        return dic;
      }
    }
    #endregion
	}
}
