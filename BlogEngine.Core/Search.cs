#region Using

using System;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using BlogEngine.Core;
using System.Text.RegularExpressions;

#endregion

namespace BlogEngine.Core
{
	/// <summary>
	/// Searches the post collection and returns a result based on a search term.
	/// <remarks>It is used for related posts and the in-site search feature.</remarks>
	/// </summary>
	public static class Search
	{

		static Search()
		{
			BuildCatalog();
			Post.Saved += new EventHandler<SavedEventArgs>(Post_Saved);
			Page.Saved += new EventHandler<SavedEventArgs>(Page_Saved);
			BlogSettings.Changed += delegate { BuildCatalog(); };
			Post.CommentAdded += new EventHandler<EventArgs>(Post_CommentAdded);
			Post.CommentRemoved += delegate { BuildCatalog(); };
			Comment.Approved += new EventHandler<EventArgs>(Post_CommentAdded);
		}

		#region Event handlers

		/// <summary>
		/// Adds a post to the catalog when it is added.
		/// </summary>
		private static void Post_Saved(object sender, SavedEventArgs e)
		{
			lock (_SyncRoot)
			{
				if (e.Action == SaveAction.Insert)
				{
					AddItem(sender as Post);
				}
				else
				{
					BuildCatalog();
				}
			}
		}

		private static void Page_Saved(object sender, SavedEventArgs e)
		{
			lock (_SyncRoot)
			{
				if (e.Action == SaveAction.Insert)
				{
					AddItem(sender as Page);
				}
				else
				{
					BuildCatalog();
				}
			}
		}

		static void Post_CommentAdded(object sender, EventArgs e)
		{
			if (BlogSettings.Instance.EnableCommentSearch)
			{
				Comment comment = (Comment)sender;
				if (comment.IsApproved)
					AddItem(comment);
			}
		}

		#endregion

		#region Search

		/// <summary>
		/// Searches all the posts and returns a ranked result set.
		/// </summary>
		/// <param name="searchTerm">The term to search for</param>
		/// <param name="includeComments">True to include a post's comments and their authors in search</param>
		public static List<IPublishable> Hits(string searchTerm, bool includeComments)
		{
			lock (_SyncRoot)
			{
				List<Result> results = BuildResultSet(searchTerm, includeComments);
				List<IPublishable> items = results.ConvertAll(new Converter<Result, IPublishable>(ResultToPost));
				results.Clear();
				OnSearcing(searchTerm);
				return items;
			}
		}

		private static Dictionary<string, float> SortDictionary(Dictionary<string, float> dic)
		{
			List<KeyValuePair<string, float>> list = new List<KeyValuePair<string, float>>();
			foreach (string key in dic.Keys)
			{
				list.Add(new KeyValuePair<string, float>(key, dic[key]));
			}

			list.Sort(delegate(KeyValuePair<string, float> obj1, KeyValuePair<string, float> obj2)
			{
				return obj2.Value.CompareTo(obj1.Value);
			});

			Dictionary<string, float> sortedDic = new Dictionary<string, float>();
			foreach (KeyValuePair<string, float> pair in list)
			{
				sortedDic.Add(pair.Key, pair.Value);
			}

			return sortedDic;
		}

		/// <summary>
		/// Returns a list of posts that is related to the specified post.
		/// </summary>
		public static List<IPublishable> FindRelatedItems(IPublishable post)
		{
			string term = CleanContent(post.Title, false);
			return Hits(term, false);
		}

		/// <summary>
		/// Builds the results set and ranks it.
		/// </summary>
		private static List<Result> BuildResultSet(string searchTerm, bool includeComments)
		{
			List<Result> results = new List<Result>();
			string term = CleanContent(searchTerm.ToLowerInvariant().Trim(), false);
			string[] terms = term.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string regex = string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0})", string.Join("|", terms));

			foreach (Entry entry in _Catalog)
			{
				Result result = new Result();
				if (!(entry.Item is Comment))
				{
					int titleMatches = Regex.Matches(entry.Title, regex).Count;
					result.Rank = titleMatches * 20;

					int postMatches = Regex.Matches(entry.Content, regex).Count;
					result.Rank += postMatches;

					int descriptionMatches = Regex.Matches(entry.Item.Description, regex).Count;
					result.Rank += descriptionMatches * 2;
				}
				else if (includeComments)
				{
					int commentMatches = Regex.Matches(entry.Content + entry.Title, regex).Count;
					result.Rank += commentMatches;
				}

				if (result.Rank > 0)
				{
					result.Item = entry.Item;
					results.Add(result);
				}
			}

			results.Sort();
			return results;
		}

		/// <summary>
		/// A converter delegate used for converting Results to Posts.
		/// </summary>
		private static IPublishable ResultToPost(Result result)
		{
			return result.Item;
		}

		#endregion

		#region APML

		//public static List<IPublishable> ApmlMatches(Uri url, int maxInterests)
		//{
		//  using (System.Net.WebClient client = new System.Net.WebClient())
		//  {
		//    client.UseDefaultCredentials = true;
		//    client.Headers.Add(System.Net.HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1;)");
		//    using (StreamReader reader = new StreamReader(client.OpenRead(url)))
		//    {
		//      XmlDocument doc = new XmlDocument();
		//      string content = reader.ReadToEnd();
		//      string upper = content.ToUpperInvariant();

		//      if (upper.Contains("<HTML") && upper.Contains("</HTML"))
		//      {
		//        Collection<Uri> urls = FindLinks("apml", content);
		//        if (urls.Count > 0)
		//        {
		//          LoadDocument(url, doc, urls[0]);
		//        }
		//        else
		//        {
		//          throw new NotSupportedException("No APML link on page");
		//        }
		//      }
		//      else
		//      {
		//        doc.LoadXml(content);
		//      }

		//      return Search.ApmlMatches(doc, 10);
		//    }
		//  }
		//}

		//private static void LoadDocument(Uri url, XmlDocument doc, Uri ApmlUrl)
		//{
		//  if (url.IsAbsoluteUri)
		//  {
		//    doc.Load(ApmlUrl.ToString());
		//  }
		//  else
		//  {
		//    string absoluteUrl = null;
		//    if (!url.ToString().StartsWith("/"))
		//      absoluteUrl = (url + ApmlUrl.ToString());
		//    else
		//      absoluteUrl = url.Scheme + "://" + url.Authority + ApmlUrl;

		//    doc.Load(absoluteUrl);
		//  }
		//}

		public static List<IPublishable> ApmlMatches(XmlDocument apmlFile, int maxInterests)
		{
			Dictionary<string, float> concepts = new Dictionary<string, float>();
			XmlNodeList nodes = apmlFile.SelectNodes("//Concept");
			foreach (XmlNode node in nodes)
			{
				string key = node.Attributes["key"].InnerText.ToLowerInvariant().Trim();
				float value = float.Parse(node.Attributes["value"].InnerText, System.Globalization.CultureInfo.InvariantCulture);
				if (!concepts.ContainsKey(key))
				{
					concepts.Add(key, value);
				}
				else if (concepts[key] < value)
				{
					concepts[key] = value;
				}
			}

			concepts = SortDictionary(concepts);
			int max = Math.Min(concepts.Count, maxInterests);
			int counter = 0;
			List<Result> resultSet = new List<Result>();
			foreach (string key in concepts.Keys)
			{
				counter++;
				List<Result> results = BuildResultSet(key, false);
				//results = results.FindAll(delegate(Result r) { return r.Rank > 1; });
				resultSet.AddRange(results);
				if (counter == max)
					break;
			}

			resultSet.Sort();
			List<Result> aggregatedResults = new List<Result>();
			foreach (Result r in resultSet)
			{
				if (!aggregatedResults.Contains(r))
				{
					aggregatedResults.Add(r);
				}
				else
				{
					Result existingResult = aggregatedResults.Find(delegate(Result res) { return res.GetHashCode() == r.GetHashCode(); });
					existingResult.Rank += r.Rank;
				}
			}

			aggregatedResults = aggregatedResults.FindAll(delegate(Result r) { return r.Rank > 1; });
			List<IPublishable> items = aggregatedResults.ConvertAll(new Converter<Result, IPublishable>(ResultToPost));
			List<IPublishable> uniqueItems = new List<IPublishable>();

			foreach (IPublishable item in items)
			{
				if (!uniqueItems.Contains(item))
					uniqueItems.Add(item);
			}

			return uniqueItems;
		}

		//private const string PATTERN = "<head.*<link( [^>]*title=\"{0}\"[^>]*)>.*</head>";
		//private static readonly Regex HREF = new Regex("href=\"(.*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		///// <summary>
		///// Finds semantic links in a given HTML document.
		///// </summary>
		///// <param name="type">The type of link. Could be foaf, apml or sioc.</param>
		///// <param name="html">The HTML to look through.</param>
		///// <returns></returns>
		//public static Collection<Uri> FindLinks(string type, string html)
		//{
		//  MatchCollection matches = Regex.Matches(html, string.Format(PATTERN, type), RegexOptions.IgnoreCase | RegexOptions.Singleline);
		//  Collection<Uri> urls = new Collection<Uri>();

		//  foreach (Match match in matches)
		//  {
		//    if (match.Groups.Count == 2)
		//    {
		//      string link = match.Groups[1].Value;
		//      Match hrefMatch = HREF.Match(link);

		//      if (hrefMatch.Groups.Count == 2)
		//      {
		//        Uri url;
		//        string value = hrefMatch.Groups[1].Value;
		//        if (Uri.TryCreate(value, UriKind.Absolute, out url))
		//        {
		//          urls.Add(url);
		//        }
		//      }
		//    }
		//  }

		//  return urls;
		//}

		#endregion

		#region Properties and private fields

		private readonly static object _SyncRoot = new object();
		private readonly static StringCollection _StopWords = BlogEngine.Core.Providers.BlogService.LoadStopWords();
		private static Collection<Entry> _Catalog = new Collection<Entry>();

		#endregion

		#region BuildCatalog

		/// <summary>
		/// Builds the catalog so it can be searched.
		/// </summary>
		private static void BuildCatalog()
		{
			OnIndexBuilding();

			lock (_SyncRoot)
			{
				_Catalog.Clear();
				foreach (Post post in Post.Posts)
				{
					if (!post.IsVisibleToPublic)
						continue;

					AddItem(post);
					if (BlogSettings.Instance.EnableCommentSearch)
					{
						foreach (Comment comment in post.Comments)
						{
							if (comment.IsApproved)
								AddItem(comment);
						}
					}
				}

				foreach (Page page in Page.Pages)
				{
                    if (page.IsVisibleToPublic)
						AddItem(page);
				}
			}

			OnIndexBuild();
		}

		/// <summary>
		/// Adds an IPublishable item to the search catalog. 
		/// That will make it immediately searchable.
		/// </summary>
		public static void AddItem(IPublishable item)
		{
			Entry entry = new Entry();
			entry.Item = item;
			entry.Title = CleanContent(item.Title, false);
			entry.Content = HttpUtility.HtmlDecode(CleanContent(item.Content, true));
			if (item is Comment)
			{
				entry.Content += HttpUtility.HtmlDecode(CleanContent(item.Author, false));
			}
			_Catalog.Add(entry);
		}

		/// <summary>
		/// Removes stop words and HTML from the specified string.
		/// </summary>
		private static string CleanContent(string content, bool removeHtml)
		{
			if (removeHtml)
				content = Utils.StripHtml(content);

			content = content
											.Replace("\\", string.Empty)
											.Replace("|", string.Empty)
											.Replace("(", string.Empty)
											.Replace(")", string.Empty)
											.Replace("[", string.Empty)
											.Replace("]", string.Empty)
											.Replace("*", string.Empty)
											.Replace("?", string.Empty)
											.Replace("}", string.Empty)
											.Replace("{", string.Empty)
											.Replace("^", string.Empty)
											.Replace("+", string.Empty);

			string[] words = content.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < words.Length; i++)
			{
				string word = words[i].ToLowerInvariant().Trim();
				if (word.Length > 1 && !_StopWords.Contains(word))
					sb.Append(word + " ");
			}

			return sb.ToString();
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a search is performed. (The search term is the sender).
		/// </summary>
		public static event EventHandler<EventArgs> Searching;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		private static void OnSearcing(string searchTerm)
		{
			if (Searching != null)
			{
				Searching(searchTerm, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs just before the search index is being build.
		/// </summary>
		public static event EventHandler<EventArgs> IndexBuilding;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		private static void OnIndexBuilding()
		{
			if (IndexBuilding != null)
			{
				IndexBuilding(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs after the index has been build.
		/// </summary>
		public static event EventHandler<EventArgs> IndexBuild;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		private static void OnIndexBuild()
		{
			if (IndexBuild != null)
			{
				IndexBuild(null, EventArgs.Empty);
			}
		}

		#endregion

	}

	#region Entry and Result structs

	/// <summary>
	/// A search optimized post object cleansed from HTML and stop words.
	/// </summary>
	internal struct Entry
	{
		/// <summary>The post object reference</summary>
		internal IPublishable Item;
		/// <summary>The title of the post cleansed for stop words</summary>
		internal string Title;
		/// <summary>The content of the post cleansed for stop words and HTML</summary>
		internal string Content;
	}

	/// <summary>
	/// A result is a search result which contains a post and its ranking.
	/// </summary>
	internal class Result : IComparable<Result>
	{
		/// <summary>
		/// The rank of the post based on the search term. The higher the rank, the higher the post is in the result set.
		/// </summary>
		internal int Rank;

		/// <summary>
		/// The post of the result.
		/// </summary>
		internal IPublishable Item;

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value 
		/// has the following meanings: Value Meaning Less than zero This object is less than the other parameter.Zero 
		/// This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		public int CompareTo(Result other)
		{
			return other.Rank.CompareTo(Rank);
		}

		public override int GetHashCode()
		{
			return Item.Id.GetHashCode();
		}
	}

	#endregion

}
