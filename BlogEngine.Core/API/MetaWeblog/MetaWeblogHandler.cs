using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Security;
using BlogEngine.Core;

namespace BlogEngine.Core.API.MetaWeblog
{
	/// <summary>
	/// HTTP Handler for MetaWeblog API
	/// </summary>
	internal class MetaWeblogHandler : IHttpHandler
	{
		#region IHttpHandler Members

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
		/// </summary>
		/// <value></value>
		/// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
		public bool IsReusable
		{
			get { return false; }
		}

		/// <summary>
		/// Process the HTTP Request.  Create XMLRPC request, find method call, process it and create response object and sent it back.
		/// This is the heart of the MetaWeblog API
		/// </summary>
		/// <param name="context"></param>
		public void ProcessRequest(HttpContext context)
		{
			try
			{
				string rootUrl = Utils.AbsoluteWebRoot.ToString();// context.Request.Url.ToString().Substring(0, context.Request.Url.ToString().IndexOf("metaweblog.axd"));
				XMLRPCRequest input = new XMLRPCRequest(context);
				XMLRPCResponse output = new XMLRPCResponse(input.MethodName);

				switch (input.MethodName)
				{
					case "metaWeblog.newPost":
						output.PostID = NewPost(input.BlogID, input.UserName, input.Password, input.Post, input.Publish);
						break;
					case "metaWeblog.editPost":
						output.Completed = EditPost(input.PostID, input.UserName, input.Password, input.Post, input.Publish);
						break;
					case "metaWeblog.getPost":
						output.Post = GetPost(input.PostID, input.UserName, input.Password);
						break;
					case "metaWeblog.newMediaObject":
						output.MediaInfo = NewMediaObject(input.BlogID, input.UserName, input.Password, input.MediaObject, context);
						break;
					case "metaWeblog.getCategories":
						output.Categories = GetCategories(input.BlogID, input.UserName, input.Password, rootUrl);
						break;
					case "metaWeblog.getRecentPosts":
						output.Posts = GetRecentPosts(input.BlogID, input.UserName, input.Password, input.NumberOfPosts);
						break;
					case "blogger.getUsersBlogs":
					case "metaWeblog.getUsersBlogs":
						output.Blogs = GetUserBlogs(input.AppKey, input.UserName, input.Password, rootUrl);
						break;
					case "blogger.deletePost":
						output.Completed = DeletePost(input.AppKey, input.PostID, input.UserName, input.Password, input.Publish);
						break;
					case "blogger.getUserInfo":
						//Not implemented.  Not planned.
						throw new MetaWeblogException("10", "The method GetUserInfo is not implemented.");
					case "wp.newPage":
						output.PageID = NewPage(input.BlogID, input.UserName, input.Password, input.Page, input.Publish);
						break;
					case "wp.getPageList":
					case "wp.getPages":
						output.Pages = GetPages(input.BlogID, input.UserName, input.Password);
						break;
					case "wp.getPage":
						output.Page = GetPage(input.BlogID, input.PageID, input.UserName, input.Password);
						break;
					case "wp.editPage":
						output.Completed = EditPage(input.BlogID, input.PageID, input.UserName, input.Password, input.Page, input.Publish);
						break;
					case "wp.deletePage":
						output.Completed = DeletePage(input.BlogID, input.PageID, input.UserName, input.Password);
						break;
                    case "wp.getAuthors":
				        output.Authors = GetAuthors(input.BlogID, input.UserName, input.Password);
				        break;
                    case "wp.getTags":
                        output.Keywords = GetKeywords(input.BlogID, input.UserName, input.Password);
				        break;
				}

				output.Response(context);
			}
			catch (MetaWeblogException mex)
			{
				XMLRPCResponse output = new XMLRPCResponse("fault");
				MWAFault fault = new MWAFault();
				fault.faultCode = mex.Code;
				fault.faultString = mex.Message;
				output.Fault = fault;
				output.Response(context);
			}
			catch (Exception ex)
			{
				XMLRPCResponse output = new XMLRPCResponse("fault");
				MWAFault fault = new MWAFault();
				fault.faultCode = "0";
				fault.faultString = ex.Message;
				output.Fault = fault;
				output.Response(context);
			}
		}

		#endregion

		#region API Methods

		/// <summary>
		/// metaWeblog.newPost
		/// </summary>
		/// <param name="blogID">always 1000 in BlogEngine since it is a singlar blog instance</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="sentPost">struct with post details</param>
		/// <param name="publish">mark as published?</param>
		/// <returns>postID as string</returns>
		internal string NewPost(string blogID, string userName, string password, MWAPost sentPost, bool publish)
		{
			ValidateRequest(userName, password);

			Post post = new Post();

            if (String.IsNullOrEmpty(sentPost.author))
                post.Author = userName;
            else
                post.Author = sentPost.author;
			post.Title = sentPost.title;
			post.Content = sentPost.description;
			post.IsPublished = publish;
			post.Slug = sentPost.slug;
			post.Description = sentPost.excerpt;

			if (sentPost.commentPolicy != "")
			{
				if (sentPost.commentPolicy == "1")
					post.IsCommentsEnabled = true;
				else
					post.IsCommentsEnabled = false;
			}

			post.Categories.Clear();
			foreach (string item in sentPost.categories)
			{
				Category cat;
				if (LookupCategoryGuidByName(item, out cat))
					post.Categories.Add(cat);
				else
				{
					// Allowing new categories to be added.  (This breaks spec, but is supported via WLW)
					Category newcat = new Category(item, "");
					newcat.Save();
					post.Categories.Add(newcat);
				}
			}
			post.Tags.Clear();
			foreach (string item in sentPost.tags)
			{
				if (item != null && item.Trim() != string.Empty)
					post.Tags.Add(item);
			}

			if (sentPost.postDate != new DateTime())
				post.DateCreated = sentPost.postDate;

			post.Save();

			return post.Id.ToString();
		}

		/// <summary>
		/// metaWeblog.editPost
		/// </summary>
		/// <param name="postID">post guid in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="sentPost">struct with post details</param>
		/// <param name="publish">mark as published?</param>
		/// <returns>1 if successful</returns>
		internal bool EditPost(string postID, string userName, string password, MWAPost sentPost, bool publish)
		{
			ValidateRequest(userName, password);

			Post post = Post.GetPost(new Guid(postID));

            if (String.IsNullOrEmpty(sentPost.author))
                post.Author = userName;
            else
                post.Author = sentPost.author;
			post.Title = sentPost.title;
			post.Content = sentPost.description;
			post.IsPublished = publish;
			post.Slug = sentPost.slug;
			post.Description = sentPost.excerpt;

			if (sentPost.commentPolicy != "")
			{
				if (sentPost.commentPolicy == "1")
					post.IsCommentsEnabled = true;
				else
					post.IsCommentsEnabled = false;
			}
			post.Categories.Clear();
			foreach (string item in sentPost.categories)
			{
				Category cat;
				if (LookupCategoryGuidByName(item, out cat))
					post.Categories.Add(cat);
				else
				{
					// Allowing new categories to be added.  (This breaks spec, but is supported via WLW)
					Category newcat = new Category(item, "");
					newcat.Save();
					post.Categories.Add(newcat);
				}
			}
			post.Tags.Clear();
			foreach (string item in sentPost.tags)
			{
				if (item != null && item.Trim() != string.Empty)
					post.Tags.Add(item);
			}

			if (sentPost.postDate != new DateTime())
				post.DateCreated = sentPost.postDate.AddHours(-BlogSettings.Instance.Timezone);

			post.Save();

			return true;
		}

		/// <summary>
		/// metaWeblog.getPost
		/// </summary>
		/// <param name="postID">post guid in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <returns>struct with post details</returns>
		internal MWAPost GetPost(string postID, string userName, string password)
		{
			ValidateRequest(userName, password);

			MWAPost sendPost = new MWAPost();
			Post post = Post.GetPost(new Guid(postID));

			sendPost.postID = post.Id.ToString();
			sendPost.postDate = post.DateCreated;
			sendPost.title = post.Title;
			sendPost.description = post.Content;
			sendPost.link = post.AbsoluteLink.AbsoluteUri;
			sendPost.slug = post.Slug;
			sendPost.excerpt = post.Description;
			if (post.IsCommentsEnabled)
				sendPost.commentPolicy = "1";
			else
				sendPost.commentPolicy = "0";


			sendPost.publish = post.IsPublished;

			List<string> cats = new List<string>();
			for (int i = 0; i < post.Categories.Count; i++)
			{
				cats.Add(Category.GetCategory(post.Categories[i].Id).ToString());
			}
			sendPost.categories = cats;

			List<string> tags = new List<string>();
			for (int i = 0; i < post.Tags.Count; i++)
			{
				tags.Add(post.Tags[i]);
			}
			sendPost.tags = tags;

			return sendPost;
		}

		/// <summary>
		/// metaWeblog.newMediaObject
		/// </summary>
		/// <param name="blogID">always 1000 in BlogEngine since it is a singlar blog instance</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="mediaObject">struct with media details</param>
		/// <param name="request">The HTTP request.</param>
		/// <returns>struct with url to media</returns>
		internal MWAMediaInfo NewMediaObject(string blogID, string userName, string password, MWAMediaObject mediaObject, HttpContext request)
		{
			ValidateRequest(userName, password);

			MWAMediaInfo mediaInfo = new MWAMediaInfo();

			string rootPath = BlogSettings.Instance.StorageLocation + "files/";
			string serverPath = request.Server.MapPath(rootPath);
			string saveFolder = serverPath;
			string fileName = mediaObject.name;
            string mediaFolder = "";

			// Check/Create Folders & Fix fileName
			if (mediaObject.name.LastIndexOf('/') > -1)
			{
                mediaFolder = mediaObject.name.Substring(0, mediaObject.name.LastIndexOf('/'));
				saveFolder += mediaFolder;
                mediaFolder += "/";
				saveFolder = saveFolder.Replace('/', Path.DirectorySeparatorChar);
				fileName = mediaObject.name.Substring(mediaObject.name.LastIndexOf('/') + 1);
			}
			else
			{
				if (saveFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
					saveFolder = saveFolder.Substring(0, saveFolder.Length - 1);
			}
			if (!Directory.Exists(saveFolder))
				Directory.CreateDirectory(saveFolder);
			saveFolder += Path.DirectorySeparatorChar;

            if (File.Exists(saveFolder + fileName))
            {
                // Find unique fileName
                for (int count = 1; count < 30000; count++)
                {
                    string tempFileName = fileName.Insert(fileName.LastIndexOf('.'), "_" + count);
                    if (!File.Exists(saveFolder + tempFileName))
                    {
                        fileName = tempFileName;
                        break;
                    }
                }
            }

			// Save File
			FileStream fs = new FileStream(saveFolder + fileName, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(fs);
			bw.Write(mediaObject.bits);
			bw.Close();

			// Set Url
			string rootUrl = Utils.AbsoluteWebRoot.ToString();
			if (BlogSettings.Instance.RequireSSLMetaWeblogAPI)
				rootUrl = rootUrl.Replace("https://", "http://");

			string mediaType = mediaObject.type;
			if (mediaType.IndexOf('/') > -1)
				mediaType = mediaType.Substring(0, mediaType.IndexOf('/'));
			switch (mediaType)
			{
				case "image":
				case "notsent": // If there wasn't a type, let's pretend it is an image.  (Thanks Zoundry.  This is for you.)
					rootUrl += "image.axd?picture=";
					break;
				default:
					rootUrl += "file.axd?file=";
					break;
			}

			mediaInfo.url = rootUrl + mediaFolder + fileName;
			return mediaInfo;
		}

		/// <summary>
		/// metaWeblog.getCategories
		/// </summary>
		/// <param name="blogID">always 1000 in BlogEngine since it is a singlar blog instance</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="rootUrl">The root URL.</param>
		/// <returns>array of category structs</returns>
		internal List<MWACategory> GetCategories(string blogID, string userName, string password, string rootUrl)
		{
			List<MWACategory> categories = new List<MWACategory>();

			ValidateRequest(userName, password);

			foreach (Category cat in Category.Categories)
			{
				MWACategory temp = new MWACategory();
				temp.title = cat.Title;
				temp.description = cat.Title; //cat.Description;
                temp.htmlUrl = cat.AbsoluteLink.ToString();
                temp.rssUrl = cat.FeedAbsoluteLink.ToString();
				categories.Add(temp);
			}

			return categories;
		}

        /// <summary>
        /// wp.getTags
        /// </summary>
        /// <param name="blogID"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>list of tags</returns>
        internal List<string> GetKeywords(string blogID, string userName, string password)
        {
            List<string> keywords = new List<string>();

            ValidateRequest(userName, password);

            foreach (Post post in Post.Posts)
            {
                if (post.IsVisible)
                {
                    foreach (string tag in post.Tags)
                    {
                        if (!keywords.Contains(tag))
                            keywords.Add(tag);
                    }
                }
            }
            keywords.Sort();

            return keywords;
        }

		/// <summary>
		/// metaWeblog.getRecentPosts
		/// </summary>
		/// <param name="blogID">always 1000 in BlogEngine since it is a singlar blog instance</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="numberOfPosts">number of posts to return</param>
		/// <returns>array of post structs</returns>
		internal List<MWAPost> GetRecentPosts(string blogID, string userName, string password, int numberOfPosts)
		{
			ValidateRequest(userName, password);

			List<MWAPost> sendPosts = new List<MWAPost>();
			List<Post> posts = Post.Posts;

			// Set End Point
			int stop = numberOfPosts;
			if (stop > posts.Count)
				stop = posts.Count;

			foreach (Post post in posts.GetRange(0, stop))
			{
				MWAPost tempPost = new MWAPost();
				List<string> tempCats = new List<string>();
				List<string> tempTags = new List<string>();

				tempPost.postID = post.Id.ToString();
				tempPost.postDate = post.DateCreated;
				tempPost.title = post.Title;
				tempPost.description = post.Content;
				tempPost.link = post.AbsoluteLink.AbsoluteUri;
				tempPost.slug = post.Slug;
				tempPost.excerpt = post.Description;
				if (post.IsCommentsEnabled)
					tempPost.commentPolicy = "";
				else
					tempPost.commentPolicy = "0";
				tempPost.publish = post.IsPublished;
				for (int i = 0; i < post.Categories.Count; i++)
				{
					tempCats.Add(Category.GetCategory(post.Categories[i].Id).ToString());
				}
				tempPost.categories = tempCats;

				for (int i = 0; i < post.Tags.Count; i++)
				{
					tempTags.Add(post.Tags[i]);
				}
				tempPost.tags = tempTags;

				sendPosts.Add(tempPost);

			}

			return sendPosts;
		}

		/// <summary>
		/// blogger.getUsersBlogs
		/// </summary>
		/// <param name="appKey">Key from application.  Outdated methodology that has no use here.</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="rootUrl">The root URL.</param>
		/// <returns>array of blog structs</returns>
		internal List<MWABlogInfo> GetUserBlogs(string appKey, string userName, string password, string rootUrl)
		{
			List<MWABlogInfo> blogs = new List<MWABlogInfo>();

			ValidateRequest(userName, password);

			MWABlogInfo temp = new MWABlogInfo();
			temp.url = rootUrl;
			temp.blogID = "1000";
			temp.blogName = BlogSettings.Instance.Name;
			blogs.Add(temp);

			return blogs;
		}

		/// <summary>
		/// blogger.deletePost
		/// </summary>
		/// <param name="appKey">Key from application.  Outdated methodology that has no use here.</param>
		/// <param name="postID">post guid in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="publish">mark as published?</param>
		/// <returns></returns>
		internal bool DeletePost(string appKey, string postID, string userName, string password, bool publish)
		{
			ValidateRequest(userName, password);
			try
			{
				Post post = Post.GetPost(new Guid(postID));
				post.Delete();
				post.Save(userName, password);
			}
			catch (Exception ex)
			{
				throw new MetaWeblogException("12", "DeletePost failed.  Error: " + ex.Message);
			}

			return true;
		}

		/// <summary>
		/// wp.newPage
		/// </summary>
		/// <param name="blogID">blogID in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <param name="mPage"></param>
		/// <param name="publish"></param>
		/// <returns></returns>
		internal string NewPage(string blogID, string userName, string password, MWAPage mPage, bool publish)
		{
			ValidateRequest(userName, password);

			Page page = new Page();
			page.Title = mPage.title;
			page.Content = mPage.description;
			page.Description = ""; // Can not be set from WLW
			page.Keywords = mPage.mt_keywords;
			if (mPage.pageDate != new DateTime())
				page.DateCreated = mPage.pageDate;
			page.ShowInList = publish;
			page.IsPublished = publish;
			if (mPage.pageParentID != "0")
				page.Parent = new Guid(mPage.pageParentID);

			page.Save();

			return page.Id.ToString();
		}

		/// <summary>
		/// wp.getPages
		/// </summary>
		/// <param name="blogID">blogID in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <returns></returns>
		internal List<MWAPage> GetPages(string blogID, string userName, string password)
		{
			ValidateRequest(userName, password);

			List<MWAPage> pages = new List<MWAPage>();

			foreach (Page page in Page.Pages)
			{
				MWAPage mPage = new MWAPage();
				mPage.pageID = page.Id.ToString();
				mPage.title = page.Title;
				mPage.description = page.Content;
				mPage.mt_keywords = page.Keywords;
				mPage.pageDate = page.DateCreated;
				mPage.link = page.AbsoluteLink.AbsoluteUri;
				mPage.mt_convert_breaks = "__default__";
				mPage.pageParentID = page.Parent.ToString();

				pages.Add(mPage);
			}

			return pages;
		}

		/// <summary>
		/// wp.getPage
		/// </summary>
		/// <param name="blogID">blogID in string format</param>
		/// <param name="pageID">page guid in string format</param>
		/// <param name="userName">login username</param>
		/// <param name="password">login password</param>
		/// <returns>struct with post details</returns>
		internal MWAPage GetPage(string blogID, string pageID, string userName, string password)
		{
			ValidateRequest(userName, password);

			MWAPage sendPage = new MWAPage();
			Page page = Page.GetPage(new Guid(pageID));

			sendPage.pageID = page.Id.ToString();
			sendPage.title = page.Title;
			sendPage.description = page.Content;
			sendPage.mt_keywords = page.Keywords;
			sendPage.pageDate = page.DateCreated;
			sendPage.link = page.AbsoluteLink.AbsoluteUri;
			sendPage.mt_convert_breaks = "__default__";
			if (page.Parent != null)
				sendPage.pageParentID = page.Parent.ToString();

			return sendPage;
		}

		internal bool EditPage(string blogID, string pageID, string userName, string password, MWAPage mPage, bool publish)
		{
			ValidateRequest(userName, password);

			Page page = Page.GetPage(new Guid(pageID));

			page.Title = mPage.title;
			page.Content = mPage.description;
			page.Keywords = mPage.mt_keywords;
			page.ShowInList = publish;
			page.IsPublished = publish;
			if (mPage.pageParentID != "0")
				page.Parent = new Guid(mPage.pageParentID);

			page.Save();

			return true;
		}

		internal bool DeletePage(string blogID, string pageID, string userName, string password)
		{
			ValidateRequest(userName, password);
			try
			{
				Page page = Page.GetPage(new Guid(pageID));
				page.Delete();
				page.Save(userName, password);
			}
			catch (Exception ex)
			{
				throw new MetaWeblogException("15", "DeletePage failed.  Error: " + ex.Message);
			}

			return true;
		}

        internal List<MWAAuthor> GetAuthors(string blogID, string userName, string password)
        {
            ValidateRequest(userName, password);

            List<MWAAuthor> authors = new List<MWAAuthor>();

            if (Roles.IsUserInRole(userName, BlogSettings.Instance.AdministratorRole))
            {
                int total = 0;
                int count = 0;
                MembershipUserCollection users = Membership.Provider.GetAllUsers(0, 999, out total);

                foreach (MembershipUser user in users)
                {
                    count++;
                    MWAAuthor temp = new MWAAuthor();
                    temp.user_id = user.UserName;
                    temp.user_login = user.UserName;
                    temp.display_name = user.UserName;
                    temp.user_email = user.Email;
                    temp.meta_value = "";
                    authors.Add(temp);
                }
            }
            else
            {
                // If not admin, just add that user to the options.
                MembershipUser single = Membership.GetUser(userName);
                MWAAuthor temp = new MWAAuthor();
                temp.user_id = single.UserName;
                temp.user_login = single.UserName;
                temp.display_name = single.UserName;
                temp.user_email = single.Email;
                temp.meta_value = "";
                authors.Add(temp);
            }
            return authors;
        }

		#endregion

		#region Private Methods

		/// <summary>
		/// Checks username and password.  Throws error if validation fails.
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="password"></param>
		private void ValidateRequest(string userName, string password)
		{
			if (!Membership.ValidateUser(userName, password))
			{
				throw new MetaWeblogException("11", "User authentication failed");
			}
		}

		/// <summary>
		/// Returns Category Guid from Category name.
		/// </summary>
		/// <remarks>
		/// Reverse dictionary lookups are ugly.
		/// </remarks>
		/// <param name="name"></param>
		/// <param name="cat"></param>
		/// <returns></returns>
		private bool LookupCategoryGuidByName(string name, out Category cat)
		{
			cat = new Category();
			foreach (Category item in Category.Categories)
			{
				if (item.Title == name)
				{
					cat = item;
					return true;
				}
			}
			return false;
		}

		#endregion
	}

	/// <summary>
	/// Exception specifically for MetaWeblog API.  Error (or fault) responses 
	/// request a code value.  This is our chance to add one to the exceptions
	/// which can be used to produce a proper fault.
	/// </summary>
	[Serializable()]
	public class MetaWeblogException : Exception
	{
		/// <summary>
		/// Constructor to load properties
		/// </summary>
		/// <param name="code">Fault code to be returned in Fault Response</param>
		/// <param name="message">Message to be returned in Fault Response</param>
		public MetaWeblogException(string code, string message)
			: base(message)
		{
			_code = code;
		}

		private string _code;
		/// <summary>
		/// Code is actually for Fault Code.  It will be passed back in the 
		/// response along with the error message.
		/// </summary>
		public string Code
		{
			get { return _code; }
		}
	}

	/// <summary>
	/// MetaWeblog Category struct
	/// returned as an array from GetCategories
	/// </summary>
	internal struct MWACategory
	{
		/// <summary>
		/// Category title
		/// </summary>
		public string description;
		/// <summary>
		/// Url to thml display of category
		/// </summary>
		public string htmlUrl;
		/// <summary>
		/// Url to RSS for category
		/// </summary>
		public string rssUrl;
		/// <summary>
		/// The guid of the category
		/// </summary>
		public string id;
		/// <summary>
		/// The title/name of the category
		/// </summary>
		public string title;
	}

	/// <summary>
	/// MetaWeblog BlogInfo struct
	/// returned as an array from getUserBlogs
	/// </summary>
	internal struct MWABlogInfo
	{
		/// <summary>
		/// Blog Url
		/// </summary>
		public string url;
		/// <summary>
		/// Blog ID (Since BlogEngine.NET is single instance this number is always 10.
		/// </summary>
		public string blogID;
		/// <summary>
		/// Blog Title
		/// </summary>
		public string blogName;
	}

	/// <summary>
	/// MetaWeblog Fault struct
	/// returned when error occurs
	/// </summary>
	internal struct MWAFault
	{
		/// <summary>
		/// Error code of Fault Response
		/// </summary>
		public string faultCode;
		/// <summary>
		/// Message of Fault Response
		/// </summary>
		public string faultString;
	}

	/// <summary>
	/// MetaWeblog MediaObject struct
	/// passed in the newMediaObject call
	/// </summary>
	internal struct MWAMediaObject
	{
		/// <summary>
		/// Name of media object (filename)
		/// </summary>
		public string name;
		/// <summary>
		/// Type of file
		/// </summary>
		public string type;
		/// <summary>
		/// Media
		/// </summary>
		public byte[] bits;
	}

	/// <summary>
	/// MetaWeblog MediaInfo struct
	/// returned from NewMediaObject call
	/// </summary>
	internal struct MWAMediaInfo
	{
		/// <summary>
		/// Url that points to Saved MediaObejct
		/// </summary>
		public string url;
	}

	/// <summary>
	/// MetaWeblog Post struct
	/// used in newPost, editPost, getPost, recentPosts
	/// not all properties are used everytime.
	/// </summary>
	internal struct MWAPost
	{
		/// <summary>
		/// PostID Guid in string format
		/// </summary>
		public string postID;
		/// <summary>
		/// Title of Blog Post
		/// </summary>
		public string title;
		/// <summary>
		/// Link to Blog Post
		/// </summary>
		public string link;
		/// <summary>
		/// Content of Blog Post
		/// </summary>
		public string description;
		/// <summary>
		/// List of Categories assigned for Blog Post
		/// </summary>
		public List<string> categories;
		/// <summary>
		/// List of Tags assinged for Blog Post
		/// </summary>
		public List<string> tags;
		/// <summary>
		/// Display date of Blog Post (DateCreated)
		/// </summary>
		public DateTime postDate;
		/// <summary>
		/// Whether the Post is published or not.
		/// </summary>
		public bool publish;
		/// <summary>
		/// Slug of post
		/// </summary>
		public string slug;
		/// <summary>
		/// CommentPolicy (Allow/Deny)
		/// </summary>
		public string commentPolicy;
		/// <summary>
		/// Excerpt
		/// </summary>
		public string excerpt;
        /// <summary>
        /// wp_author_id
        /// </summary>
	    public string author;

	}

	///// <summary>
	///// MetaWeblog UserInfo struct
	///// returned from GetUserInfo call
	///// </summary>
	///// <remarks>
	///// Not used currently, but here for completeness.
	///// </remarks>
	//internal struct MWAUserInfo
	//{
	//  /// <summary>
	//  /// User Name Proper
	//  /// </summary>
	//  //public string nickname;
	//  /// <summary>
	//  /// Login ID
	//  /// </summary>
	//  //public string userID;
	//  /// <summary>
	//  /// Url to User Blog?
	//  /// </summary>
	//  //public string url;
	//  /// <summary>
	//  /// Email address of User
	//  /// </summary>
	//  public string email;
	//  /// <summary>
	//  /// User LastName
	//  /// </summary>
	//  public string lastName;
	//  /// <summary>
	//  /// User First Name
	//  /// </summary>
	//  public string firstName;
	//}

	/// <summary>
	/// wp Page Struct
	/// </summary>
	internal struct MWAPage
	{
		/// <summary>
		/// PostID Guid in string format
		/// </summary>
		public string pageID;
		/// <summary>
		/// Title of Blog Post
		/// </summary>
		public string title;
		/// <summary>
		/// Link to Blog Post
		/// </summary>
		public string link;
		/// <summary>
		/// Content of Blog Post
		/// </summary>
		public string description;
		/// <summary>
		/// Display date of Blog Post (DateCreated)
		/// </summary>
		public DateTime pageDate;
		/// <summary>
		/// Convert Breaks
		/// </summary>
		public string mt_convert_breaks;
		/// <summary>
		/// Page Parent ID
		/// </summary>
		public string pageParentID;
		/// <summary>
		/// Page keywords
		/// </summary>
		public string mt_keywords;
	}

    /// <summary>
    /// wp Author struct
    /// </summary>
    internal struct MWAAuthor
    {
        /// <summary>
        /// userID - Specs call for a int, but our ID is a string.
        /// </summary>
        public string user_id;
        /// <summary>
        /// user login name
        /// </summary>
        public string user_login;
        /// <summary>
        /// display name
        /// </summary>
        public string display_name;
        /// <summary>
        /// user email
        /// </summary>
        public string user_email;
        /// <summary>
        /// nothing to see here.
        /// </summary>
        public string meta_value;
    }

}
