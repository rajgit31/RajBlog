#region Using

using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Providers
{
	/// <summary>
	/// A storage provider for BlogEngine that uses XML files.
	/// <remarks>
	/// To build another provider, you can just copy and modify
	/// this one. Then add it to the web.config's BlogEngine section.
	/// </remarks>
	/// </summary>
	public partial class XmlBlogProvider : BlogProvider
	{
		//private static string _Folder = System.Web.HttpContext.Current.Server.MapPath(BlogSettings.Instance.StorageLocation);

		internal string _Folder
		{
			get
			{
				string p =  StorageLocation().Replace("~/", "");
				return System.IO.Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, p);
			}
		}

		/// <summary>
		/// Retrieves a post based on the specified Id.
		/// </summary>
		public override Post SelectPost(Guid id)
		{
			string fileName = _Folder + "posts" + Path.DirectorySeparatorChar + id.ToString() + ".xml";
			Post post = new Post();
			XmlDocument doc = new XmlDocument();
			doc.Load(fileName);

			post.Title = doc.SelectSingleNode("post/title").InnerText;
			post.Description = doc.SelectSingleNode("post/description").InnerText;
			post.Content = doc.SelectSingleNode("post/content").InnerText;

			if (doc.SelectSingleNode("post/pubDate") != null)
				post.DateCreated = DateTime.Parse(doc.SelectSingleNode("post/pubDate").InnerText, CultureInfo.InvariantCulture);

			if (doc.SelectSingleNode("post/lastModified") != null)
				post.DateModified = DateTime.Parse(doc.SelectSingleNode("post/lastModified").InnerText, CultureInfo.InvariantCulture);

			if (doc.SelectSingleNode("post/author") != null)
				post.Author = doc.SelectSingleNode("post/author").InnerText;

			if (doc.SelectSingleNode("post/ispublished") != null)
				post.IsPublished = bool.Parse(doc.SelectSingleNode("post/ispublished").InnerText);

			if (doc.SelectSingleNode("post/iscommentsenabled") != null)
				post.IsCommentsEnabled = bool.Parse(doc.SelectSingleNode("post/iscommentsenabled").InnerText);

			if (doc.SelectSingleNode("post/raters") != null)
				post.Raters = int.Parse(doc.SelectSingleNode("post/raters").InnerText, CultureInfo.InvariantCulture);

			if (doc.SelectSingleNode("post/rating") != null)
				post.Rating = float.Parse(doc.SelectSingleNode("post/rating").InnerText, System.Globalization.CultureInfo.GetCultureInfo("en-gb"));

			if (doc.SelectSingleNode("post/slug") != null)
				post.Slug = doc.SelectSingleNode("post/slug").InnerText;

			// Tags
			foreach (XmlNode node in doc.SelectNodes("post/tags/tag"))
			{
				if (!string.IsNullOrEmpty(node.InnerText))
					post.Tags.Add(node.InnerText);
			}

			// comments			
			foreach (XmlNode node in doc.SelectNodes("post/comments/comment"))
			{
				Comment comment = new Comment();
				comment.Id = new Guid(node.Attributes["id"].InnerText);
				comment.ParentId = (node.Attributes["parentid"] != null) ? new Guid(node.Attributes["parentid"].InnerText) : Guid.Empty;
				comment.Author = node.SelectSingleNode("author").InnerText;
				comment.Email = node.SelectSingleNode("email").InnerText;
				comment.Parent = post;

				if (node.SelectSingleNode("country") != null)
					comment.Country = node.SelectSingleNode("country").InnerText;

				if (node.SelectSingleNode("ip") != null)
					comment.IP = node.SelectSingleNode("ip").InnerText;

				if (node.SelectSingleNode("website") != null)
				{
					Uri website;
					if (Uri.TryCreate(node.SelectSingleNode("website").InnerText, UriKind.Absolute, out website))
						comment.Website = website;
				}

                if (node.SelectSingleNode("moderatedby") != null)
                    comment.ModeratedBy = node.SelectSingleNode("moderatedby").InnerText;

				if (node.Attributes["approved"] != null)
					comment.IsApproved = bool.Parse(node.Attributes["approved"].InnerText);
				else
					comment.IsApproved = true;

                if (node.SelectSingleNode("avatar") != null)
                {
                    comment.Avatar = node.SelectSingleNode("avatar").InnerText;
                }

				comment.Content = node.SelectSingleNode("content").InnerText;
				comment.DateCreated = DateTime.Parse(node.SelectSingleNode("date").InnerText, CultureInfo.InvariantCulture);
				post.Comments.Add(comment);
			}
			

			post.Comments.Sort();

			// categories
			foreach (XmlNode node in doc.SelectNodes("post/categories/category"))
			{
				Guid key = new Guid(node.InnerText);
				Category cat = Category.GetCategory(key);
				if (cat != null)//CategoryDictionary.Instance.ContainsKey(key))
					post.Categories.Add(cat);
			}

			// Notification e-mails
			foreach (XmlNode node in doc.SelectNodes("post/notifications/email"))
			{
				post.NotificationEmails.Add(node.InnerText);
			}

			return post;
		}


		///// <summary>
		///// Retrieves the content of the post in order to lazy load.
		///// </summary>
		///// <param name="id"></param>
		///// <returns></returns>
		//public override string SelectPostContent(Guid id)
		//{
		//  string fileName = _Folder + "posts" + Path.DirectorySeparatorChar + id.ToString() + ".xml";
		//  Post post = new Post();
		//  XmlDocument doc = new XmlDocument();
		//  doc.Load(fileName);

		//  if (doc.SelectSingleNode("post/content") != null)
		//    return doc.SelectSingleNode("post/content").InnerText;

		//  return string.Empty;
		//}

		/// <summary>
		/// Inserts a new Post to the data store.
		/// </summary>
		/// <param name="post"></param>
		public override void InsertPost(Post post)
		{
			if (!Directory.Exists(_Folder + "posts"))
				Directory.CreateDirectory(_Folder + "posts");

			string fileName = _Folder + "posts" + Path.DirectorySeparatorChar + post.Id.ToString() + ".xml";
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;

			MemoryStream ms = new MemoryStream();

			using (XmlWriter writer = XmlWriter.Create(ms, settings))
			{
				writer.WriteStartDocument(true);
				writer.WriteStartElement("post");

				writer.WriteElementString("author", post.Author);
				writer.WriteElementString("title", post.Title);
				writer.WriteElementString("description", post.Description);
				writer.WriteElementString("content", post.Content);
				writer.WriteElementString("ispublished", post.IsPublished.ToString());
				writer.WriteElementString("iscommentsenabled", post.IsCommentsEnabled.ToString());
				writer.WriteElementString("pubDate", post.DateCreated.AddHours(-BlogSettings.Instance.Timezone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
				writer.WriteElementString("lastModified", post.DateModified.AddHours(-BlogSettings.Instance.Timezone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
				writer.WriteElementString("raters", post.Raters.ToString(CultureInfo.InvariantCulture));
				writer.WriteElementString("rating", post.Rating.ToString(CultureInfo.InvariantCulture));
				writer.WriteElementString("slug", post.Slug);

				// Tags
				writer.WriteStartElement("tags");
				foreach (string tag in post.Tags)
				{
					writer.WriteElementString("tag", tag);
				}
				writer.WriteEndElement();

				// comments
				writer.WriteStartElement("comments");
				foreach (Comment comment in post.Comments)
				{
					writer.WriteStartElement("comment");
					writer.WriteAttributeString("id", comment.Id.ToString());
					writer.WriteAttributeString("parentid", comment.ParentId.ToString());
					writer.WriteAttributeString("approved", comment.IsApproved.ToString());
					writer.WriteElementString("date", comment.DateCreated.AddHours(-BlogSettings.Instance.Timezone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
					writer.WriteElementString("author", comment.Author);
					writer.WriteElementString("email", comment.Email);
					writer.WriteElementString("country", comment.Country);
					writer.WriteElementString("ip", comment.IP);
                    
                    if (comment.Website != null)
						writer.WriteElementString("website", comment.Website.ToString());
                    
                    if(!string.IsNullOrEmpty(comment.ModeratedBy))
                        writer.WriteElementString("moderatedby", comment.ModeratedBy);

                    if (comment.Avatar != null)
                        writer.WriteElementString("avatar", comment.Avatar);

                    writer.WriteElementString("content", comment.Content);

					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				// categories
				writer.WriteStartElement("categories");
				foreach (Category cat in post.Categories)
				{
					//if (cat.Id = .Instance.ContainsKey(key))
					//     writer.WriteElementString("category", key.ToString());
					writer.WriteElementString("category", cat.Id.ToString());

				}
				writer.WriteEndElement();

				// Notification e-mails
				writer.WriteStartElement("notifications");
				foreach (string email in post.NotificationEmails)
				{
					writer.WriteElementString("email", email);
				}
				writer.WriteEndElement();

				writer.WriteEndElement();
			}

			using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.Write))
			{
				ms.WriteTo(fs);
				ms.Dispose();
			}
		}


		/// <summary>
		/// Updates a Post.
		/// </summary>
		public override void UpdatePost(Post post)
		{
			InsertPost(post);
		}

		/// <summary>
		/// Deletes a post from the data store.
		/// </summary>
		public override void DeletePost(Post post)
		{
			string fileName = _Folder + "posts" + Path.DirectorySeparatorChar + post.Id.ToString() + ".xml";
			if (File.Exists(fileName))
				File.Delete(fileName);
		}

		/// <summary>
		/// Retrieves all posts from the data store
		/// </summary>
		/// <returns>List of Posts</returns>
		public override List<Post> FillPosts()
		{
			string folder = Category._Folder + "posts" + Path.DirectorySeparatorChar;
			List<Post> posts = new List<Post>();

			foreach (string file in Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly))
			{
				FileInfo info = new FileInfo(file);
				string id = info.Name.Replace(".xml", string.Empty);
				//Post post = SelectPost(new Guid(id));
				Post post = Post.Load(new Guid(id));
				posts.Add(post);
			}

			posts.Sort();
			return posts;
		}

	}
}