#region Using

using System;
using System.IO;
using System.Net.Mail;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Xml;
using System.Net;
using BlogEngine.Core.Web.Controls;

#endregion

namespace BlogEngine.Core
{
	/// <summary>
	/// Utilities for the entire solution to use.
	/// </summary>
	public static class Utils
	{

		/// <summary>
		/// Strips all illegal characters from the specified title.
		/// </summary>
		public static string RemoveIllegalCharacters(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			text = text.Replace(":", string.Empty);
			text = text.Replace("/", string.Empty);
			text = text.Replace("?", string.Empty);
			text = text.Replace("#", string.Empty);
			text = text.Replace("[", string.Empty);
			text = text.Replace("]", string.Empty);
			text = text.Replace("@", string.Empty);
			text = text.Replace("*", string.Empty);
			text = text.Replace(".", string.Empty);
			text = text.Replace(",", string.Empty);
			text = text.Replace("\"", string.Empty);
			text = text.Replace("&", string.Empty);
			text = text.Replace("'", string.Empty);
			text = text.Replace(" ", "-");
			text = RemoveDiacritics(text);
			text = RemoveExtraHyphen(text);

			return HttpUtility.UrlEncode(text).Replace("%", string.Empty);
		}

		private static string RemoveExtraHyphen(string text)
		{
			if (text.Contains("--"))
			{
				text = text.Replace("--", "-");
				return RemoveExtraHyphen(text);
			}

			return text;
		}

		private static String RemoveDiacritics(string text)
		{
			String normalized = text.Normalize(NormalizationForm.FormD);
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < normalized.Length; i++)
			{
				Char c = normalized[i];
				if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
					sb.Append(c);
			}

			return sb.ToString();
		}

		private static readonly Regex STRIP_HTML = new Regex("<[^>]*>", RegexOptions.Compiled);
		/// <summary>
		/// Strips all HTML tags from the specified string.
		/// </summary>
		/// <param name="html">The string containing HTML</param>
		/// <returns>A string without HTML tags</returns>
		public static string StripHtml(string html)
		{
			if (string.IsNullOrEmpty(html))
				return string.Empty;

			return STRIP_HTML.Replace(html, string.Empty);
		}

		private static readonly Regex REGEX_BETWEEN_TAGS = new Regex(@">\s+", RegexOptions.Compiled);
		private static readonly Regex REGEX_LINE_BREAKS = new Regex(@"\n\s+", RegexOptions.Compiled);

		/// <summary>
		/// Removes the HTML whitespace.
		/// </summary>
		/// <param name="html">The HTML.</param>
		public static string RemoveHtmlWhitespace(string html)
		{
			if (string.IsNullOrEmpty(html))
				return string.Empty;

			html = REGEX_BETWEEN_TAGS.Replace(html, "> ");
			html = REGEX_LINE_BREAKS.Replace(html, string.Empty);

			return html.Trim();
		}

		/// <summary>
		/// Writes ETag and Last-Modified headers and sets the conditional get headers.
		/// </summary>
		/// <param name="date">The date.</param>
        public static bool SetConditionalGetHeaders(DateTime date)
        {
            // SetLastModified() below will throw an error if the 'date' is a future date.
            // If the date is 1/1/0001, Mono will throw a 404 error
            if (date > DateTime.Now || date.Year < 1900)
                date = DateTime.Now;

            HttpResponse response = HttpContext.Current.Response;
            HttpRequest request = HttpContext.Current.Request;

            string etag = "\"" + date.Ticks + "\"";
            string incomingEtag = request.Headers["If-None-Match"];

            DateTime incomingLastModifiedDate = DateTime.MinValue;
            DateTime.TryParse(request.Headers["If-Modified-Since"], out incomingLastModifiedDate);

            response.Cache.SetLastModified(date);
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetETag(etag);

            if (String.Compare(incomingEtag, etag) == 0 || incomingLastModifiedDate == date)
            {
                response.Clear();
                response.StatusCode = (int)HttpStatusCode.NotModified;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Occurs when a message will be logged. The sender is a string containing the log message.
        /// </summary>
        public static event EventHandler<EventArgs> OnLog;
        /// <summary>
        /// Sends a message to any subscribed log listeners.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Log(object message)
        {
            if (OnLog != null)
            {
                OnLog(message, new EventArgs());
            }
        }

		#region URL handling

		/// <summary>
		/// Gets the relative URL of the blog feed. If a Feedburner username
		/// is entered in the admin settings page, it will return the 
		/// absolute Feedburner URL to the feed.
		/// </summary>
		public static string FeedUrl
		{
			get
			{
				if (!string.IsNullOrEmpty(BlogSettings.Instance.AlternateFeedUrl))
					return BlogSettings.Instance.AlternateFeedUrl;
				else
					return AbsoluteWebRoot + "syndication.axd";
			}
		}

		private static string _RelativeWebRoot;
		/// <summary>
		/// Gets the relative root of the website.
		/// </summary>
		/// <value>A string that ends with a '/'.</value>
		public static string RelativeWebRoot
		{
			get
			{
				if (_RelativeWebRoot == null)
					_RelativeWebRoot = VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["BlogEngine.VirtualPath"]);

				return _RelativeWebRoot;
			}
		}

		//private static Uri _AbsoluteWebRoot;

		/// <summary>
		/// Gets the absolute root of the website.
		/// </summary>
		/// <value>A string that ends with a '/'.</value>
		public static Uri AbsoluteWebRoot
		{
			get
			{
				//if (_AbsoluteWebRoot == null)
				//{
				HttpContext context = HttpContext.Current;
				if (context == null)
					throw new System.Net.WebException("The current HttpContext is null");

				if (context.Items["absoluteurl"] == null)
					context.Items["absoluteurl"] = new Uri(context.Request.Url.GetLeftPart(UriPartial.Authority) + RelativeWebRoot);

				return context.Items["absoluteurl"] as Uri;
				//_AbsoluteWebRoot = new Uri(context.Request.Url.GetLeftPart(UriPartial.Authority) + RelativeWebRoot);// new Uri(context.Request.Url.Scheme + "://" + context.Request.Url.Authority + RelativeWebRoot);
				//}
				//return _AbsoluteWebRoot;
			}
		}

		/// <summary>
		/// Converts a relative URL to an absolute one.
		/// </summary>
		public static Uri ConvertToAbsolute(Uri relativeUri)
		{
			return ConvertToAbsolute(relativeUri.ToString()); ;
		}

		/// <summary>
		/// Converts a relative URL to an absolute one.
		/// </summary>
		public static Uri ConvertToAbsolute(string relativeUri)
		{
			if (String.IsNullOrEmpty(relativeUri))
				throw new ArgumentNullException("relativeUri");

			string absolute = AbsoluteWebRoot.ToString();
			int index = absolute.LastIndexOf(RelativeWebRoot.ToString());

			return new Uri(absolute.Substring(0, index) + relativeUri);
		}

		/// Retrieves the subdomain from the specified URL.
		/// </summary>
		/// <param name="url">The URL from which to retrieve the subdomain.</param>
		/// <returns>The subdomain if it exist, otherwise null.</returns>
		public static string GetSubDomain(Uri url)
		{
			if (url.HostNameType == UriHostNameType.Dns)
			{
				string host = url.Host;
				if (host.Split('.').Length > 2)
				{
					int lastIndex = host.LastIndexOf(".");
					int index = host.LastIndexOf(".", lastIndex - 1);
					return host.Substring(0, index);
				}
			}

			return null;
		}

        /// <summary>
        /// Translates the specified string using the resource files.
        /// </summary>
        public static string Translate(string text)
        {
            return Translate(text, null, null);
        }

        /// <summary>
        /// Translates the specified string using the resource files.  If a translation
        /// is not found, defaultValue will be returned.
        /// </summary>
        public static string Translate(string text, string defaultValue)
        {
            return Translate(text, defaultValue, null);
        }

        /// <summary>
        /// Translates the specified string using the resource files and specified culture.
        /// If a translation is not found, defaultValue will be returned.
        /// </summary>
        public static string Translate(string text, string defaultValue, CultureInfo culture)
        {
            object resource;

            if (culture == null)
                resource = HttpContext.GetGlobalResourceObject("labels", text);
            else
                resource = HttpContext.GetGlobalResourceObject("labels", text, culture);

            if (resource != null)
                return resource.ToString();

            if (string.IsNullOrEmpty(defaultValue))
                return string.Format("Missing Resource [{0}]", text);
            else
                return defaultValue;
        }

        /// <summary>
        /// Returns the default culture.  This is either the culture specified in the blog settings,
        /// or the default culture installed with the operating system.
        /// </summary>
        public static CultureInfo GetDefaultCulture()
        {
            if (string.IsNullOrEmpty(BlogSettings.Instance.Culture) ||
                BlogSettings.Instance.Culture.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.InstalledUICulture;
            }
            return CultureInfo.CreateSpecificCulture(BlogSettings.Instance.Culture);
        }

		#endregion

		#region Is mobile device

		private static readonly Regex MOBILE_REGEX = new Regex(ConfigurationManager.AppSettings.Get("BlogEngine.MobileDevices"), RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Gets a value indicating whether the client is a mobile device.
		/// </summary>
		/// <value><c>true</c> if this instance is mobile; otherwise, <c>false</c>.</value>
		public static bool IsMobile
		{
			get
			{
				HttpContext context = HttpContext.Current;
				if (context != null)
				{
					HttpRequest request = context.Request;
					if (request.Browser.IsMobileDevice)
						return true;

					if (!string.IsNullOrEmpty(request.UserAgent) && MOBILE_REGEX.IsMatch(request.UserAgent))
						return true;
				}

				return false;
			}
		}

		#endregion

		#region Is Mono/Linux

		private static int mono = 0;
		/// <summary>
		/// Gets a value indicating whether we're running under Mono.
		/// </summary>
		/// <value><c>true</c> if Mono; otherwise, <c>false</c>.</value>
		public static bool IsMono
		{
			get
			{
				if (mono == 0)
				{
					if (Type.GetType("Mono.Runtime") != null)
						mono = 1;
					else
						mono = 2;
				}

				return mono == 1;
			}
		}

		/// <summary>
		/// Gets a value indicating whether we're running under Linux or a Unix variant.
		/// </summary>
		/// <value><c>true</c> if Linux/Unix; otherwise, <c>false</c>.</value>
		public static bool IsLinux
		{
			get
			{
				int p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 128));
			}
		}

		#endregion

		#region Send e-mail

		/// <summary>
		/// Sends a MailMessage object using the SMTP settings.
		/// </summary>
		public static void SendMailMessage(MailMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			try
			{
				message.IsBodyHtml = true;
				message.BodyEncoding = Encoding.UTF8;
				SmtpClient smtp = new SmtpClient(BlogSettings.Instance.SmtpServer);
                // don't send credentials if a server doesn't require it,
                // linux smtp servers don't like that 
                if (!string.IsNullOrEmpty(BlogSettings.Instance.SmtpUserName)) {
				    smtp.Credentials = new System.Net.NetworkCredential(BlogSettings.Instance.SmtpUserName, BlogSettings.Instance.SmtpPassword);
                }
				smtp.Port = BlogSettings.Instance.SmtpServerPort;
				smtp.EnableSsl = BlogSettings.Instance.EnableSsl;
				smtp.Send(message);
				OnEmailSent(message);
			}
			catch (SmtpException)
			{
				OnEmailFailed(message);
			}
			finally
			{
				// Remove the pointer to the message object so the GC can close the thread.
				message.Dispose();
				message = null;
			}
		}

		/// <summary>
		/// Sends the mail message asynchronously in another thread.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public static void SendMailMessageAsync(MailMessage message)
		{
			ThreadPool.QueueUserWorkItem(delegate { Utils.SendMailMessage(message); });
		}

		/// <summary>
		/// Occurs after an e-mail has been sent. The sender is the MailMessage object.
		/// </summary>
		public static event EventHandler<EventArgs> EmailSent;
		private static void OnEmailSent(MailMessage message)
		{
			if (EmailSent != null)
			{
				EmailSent(message, new EventArgs());
			}
		}

		/// <summary>
		/// Occurs after an e-mail has been sent. The sender is the MailMessage object.
		/// </summary>
		public static event EventHandler<EventArgs> EmailFailed;
		private static void OnEmailFailed(MailMessage message)
		{
			if (EmailFailed != null)
			{
				EmailFailed(message, new EventArgs());
			}
		}

		#endregion

		#region Code Assemblies
		/// <summary>
		/// This method returns all code assemblies in app_code
		/// If app_code has subdirectories for c#, vb.net etc
		/// Each one will come back as a separate assembly
		/// So we can support extensions in multiple languages
		/// </summary>
		/// <returns>List of code assemblies</returns>
		public static ArrayList CodeAssemblies()
		{
			ArrayList codeAssemblies = new ArrayList();
			CompilationSection s = null;
			try
			{
				string assemblyName = "__code";
				try
				{
					s = (CompilationSection)WebConfigurationManager.GetSection("system.web/compilation");
				}
				catch (System.Security.SecurityException)
				{
					// No read permissions on web.config due to the trust level (must be High or Full)
				}

				if (s != null && s.CodeSubDirectories != null && s.CodeSubDirectories.Count > 0)
				{
					for (int i = 0; i < s.CodeSubDirectories.Count; i++)
					{
						assemblyName = "App_SubCode_" + s.CodeSubDirectories[i].DirectoryName;
						codeAssemblies.Add(Assembly.Load(assemblyName));
					}
				}
				else
				{
					Type t = Type.GetType("Mono.Runtime");
					if (t != null) assemblyName = "App_Code";
					codeAssemblies.Add(Assembly.Load(assemblyName));
				}

                GetCompiledExtensions(codeAssemblies);
			}
			catch (System.IO.FileNotFoundException) {/*ignore - code directory has no files*/}

			return codeAssemblies;
		}

        /// <summary>
        /// Run through all code assemblies and creates object
        /// instance for types marked with extension attribute
        /// </summary>
        public static void LoadExtensions()
        {
            ArrayList codeAssemblies = CodeAssemblies();
            List<SortedExtension> sortedExtensions = new List<SortedExtension>();

            foreach (Assembly a in codeAssemblies)
            {
                Type[] types = a.GetTypes();
                foreach (Type type in types)
                {
                    object[] attributes = type.GetCustomAttributes(typeof(ExtensionAttribute), false);
                    foreach (object attribute in attributes)
                    {
                        if (attribute.GetType().Name == "ExtensionAttribute")
                        {
                            ExtensionAttribute ext = (ExtensionAttribute)attribute;
                            sortedExtensions.Add(new SortedExtension(ext.Priority, type.Name, type.FullName));
                        }
                    }
                }

                sortedExtensions.Sort(delegate(SortedExtension e1, SortedExtension e2)
                {
                    if (e1.Priority == e2.Priority)
                        return string.CompareOrdinal(e1.Name, e2.Name);
                    return e1.Priority.CompareTo(e2.Priority);
                });

                foreach (SortedExtension x in sortedExtensions)
                {
                    if (ExtensionManager.ExtensionEnabled(x.Name))
                    {
                        a.CreateInstance(x.Type);
                    }
                }
            }

            // initialize comment rules and filters
            CommentHandlers.Listen();
        }

        /// <summary>
        /// To support compiled extensions
        /// This methed looks for DLLs in the "/bin" folder
        /// and if assembly compiled with configuration
        /// attributed set to "BlogEngineExtension" it will
        /// be added to the list of code assemlies
        /// </summary>
        /// <param name="assemblies">List of code assemblies</param>
        private static void GetCompiledExtensions(ArrayList assemblies)
        {
            string s = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "bin");
            string[] fileEntries = Directory.GetFiles(s);
            foreach (string fileName in fileEntries)
            {
                if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    Assembly asm = Assembly.LoadFrom(fileName);
                    object[] attr = asm.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
                    if (attr != null && attr.Length > 0)
                    {
                        AssemblyConfigurationAttribute aca = (AssemblyConfigurationAttribute)attr[0];
                        if (aca != null && aca.Configuration == "BlogEngineExtension")
                        {
                            assemblies.Add(asm);
                        }
                    }
                }
            }
        }

		#endregion

		#region Semantic discovery

		/// <summary>
		/// Finds the semantic documents from a URL based on the type.
		/// </summary>
		/// <param name="url">The URL of the semantic document or a document containing semantic links.</param>
		/// <param name="type">The type. Could be "foaf", "apml" or "sioc".</param>
		/// <returns>A dictionary of the semantic documents. The dictionary is empty if no documents were found.</returns>
		public static Dictionary<Uri, XmlDocument> FindSemanticDocuments(Uri url, string type)
		{
			Dictionary<Uri, XmlDocument> list = new Dictionary<Uri, XmlDocument>();

			string content = DownloadWebPage(url);
			if (!string.IsNullOrEmpty(content))
			{
				string upper = content.ToUpperInvariant();

				if (upper.Contains("</HEAD") && upper.Contains("</HTML"))
				{
					List<Uri> urls = FindLinks(type, content);
					foreach (Uri xmlUrl in urls)
					{
						XmlDocument doc = LoadDocument(url, xmlUrl);
						if (doc != null)
							list.Add(xmlUrl, doc);
					}
				}
				else
				{
					XmlDocument doc = LoadDocument(url, url);
					if (doc != null)
						list.Add(url, doc);
				}
			}

			return list;
		}

/// <summary>
/// Downloads a web page from the Internet and returns the HTML as a string. .
/// </summary>
/// <param name="url">The URL to download from.</param>
/// <returns>The HTML or null if the URL isn't valid.</returns>
public static string DownloadWebPage(Uri url)
{
	try
	{
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
		request.Headers["Accept-Encoding"] = "gzip";
		request.Headers["Accept-Language"] = "en-us";
		request.Credentials = CredentialCache.DefaultNetworkCredentials;
		request.AutomaticDecompression = DecompressionMethods.GZip;

		using (WebResponse response = request.GetResponse())
		{
			using (StreamReader reader = new StreamReader(response.GetResponseStream()))
			{
				return reader.ReadToEnd();
			}
		}
	}
	catch (Exception)
	{
		return null;
	}
}

		private static XmlDocument LoadDocument(Uri url, Uri xmlUrl)
		{
			XmlDocument doc = new XmlDocument();

			try
			{
				if (url.IsAbsoluteUri)
				{
					doc.Load(xmlUrl.ToString());
				}
				else
				{
					string absoluteUrl = null;
					if (!url.ToString().StartsWith("/"))
						absoluteUrl = (url + xmlUrl.ToString());
					else
						absoluteUrl = url.Scheme + "://" + url.Authority + xmlUrl;

					doc.Load(absoluteUrl);
				}
			}
			catch (Exception)
			{
				return null;
			}

			return doc;
		}

		private const string PATTERN = "<head.*<link( [^>]*title=\"{0}\"[^>]*)>.*</head>";
		private static readonly Regex HREF = new Regex("href=\"(.*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Finds semantic links in a given HTML document.
		/// </summary>
		/// <param name="type">The type of link. Could be foaf, apml or sioc.</param>
		/// <param name="html">The HTML to look through.</param>
		/// <returns></returns>
		public static List<Uri> FindLinks(string type, string html)
		{
			MatchCollection matches = Regex.Matches(html, string.Format(PATTERN, type), RegexOptions.IgnoreCase | RegexOptions.Singleline);
			List<Uri> urls = new List<Uri>();

			foreach (Match match in matches)
			{
				if (match.Groups.Count == 2)
				{
					string link = match.Groups[1].Value;
					Match hrefMatch = HREF.Match(link);

					if (hrefMatch.Groups.Count == 2)
					{
						Uri url;
						string value = hrefMatch.Groups[1].Value;
						if (Uri.TryCreate(value, UriKind.Absolute, out url))
						{
							urls.Add(url);
						}
					}
				}
			}

			return urls;
		}

		#endregion

        #region Password Util
        /// <summary>
		/// Encrypts a string using the SHA256 algorithm.
		/// </summary>
		public static string HashPassword(string plainMessage)
		{
			byte[] data = Encoding.UTF8.GetBytes(plainMessage);
			using (HashAlgorithm sha = new SHA256Managed())
			{
				byte[] encryptedBytes = sha.TransformFinalBlock(data, 0, data.Length);
				return Convert.ToBase64String(sha.Hash);
			}
		}
        #endregion

    }
}
