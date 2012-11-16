#region Using

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using System.Web.Security;
using System.IO;
using System.Security;
using System.Security.Cryptography;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
	/// <summary>
	/// Based on John Dyer's (http://johndyer.name/) extension.
	/// </summary>
	public class Sioc : IHttpHandler
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
		/// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
		public void ProcessRequest(HttpContext context)
		{
			context.Response.ContentType = "text/xml";
			string siocType = context.Request["sioc_type"] + string.Empty;
			string siocID = context.Request["sioc_id"] + string.Empty;

			switch (siocType)
			{
				default:
				case "site":
					List<IPublishable> list = Post.Posts.ConvertAll(new Converter<Post, IPublishable>(ConvertToIPublishable));
					int max = Math.Min(BlogSettings.Instance.PostsPerFeed, list.Count);
					list = list.GetRange(0, max);
					WriteSite(context.Response.OutputStream, list);
					break;

				case "post":
					Guid postID = Guid.Empty;
					try
					{
						postID = new Guid(siocID);
					}
					catch { }

					if (postID != Guid.Empty)
					{
						Post post = Post.GetPost(postID);
						if (post != null)
						{
							WritePub(context.Response.OutputStream, post);
						}
					}
					break;

				case "comment":
					Guid commentID = Guid.Empty;
					try
					{
						commentID = new Guid(siocID);
					}
					catch { }

					if (commentID != Guid.Empty)
					{
						// TODO: is it possible to get a single comment?
						Comment comment = GetComment(commentID);

						if (comment != null)
						{
							WritePub(context.Response.OutputStream, comment);
						}
					}
					break;

				case "user":
					WriteAuthor(context.Response.OutputStream, siocID);
					break;
				/*
				case "post":
					generator.WriteSite(context.Response.OutputStream);
					break;
				*/
			}
		}

		#endregion

		#region Urls

		private static string GetSiocSiteUrl()
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=site";
		}

		private static string GetSiocAuthorsUrl()
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=site#authors";
		}

		private static string GetSiocBlogUrl()
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=site#webblog";
		}

		private static string GetSiocPostUrl(string id)
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=post&sioc_id=" + id;
		}

		private static string GetSiocAuthorUrl(string username)
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=user&sioc_id=" + System.Web.HttpUtility.UrlEncode(username);
		}

		private static string GetBlogAuthorUrl(string username)
		{
			return Utils.AbsoluteWebRoot.ToString() + "author/" + System.Web.HttpUtility.UrlEncode(username) + ".aspx";
		}

		private string GetSiocCommentUrl(string id)
		{
			return Utils.AbsoluteWebRoot.ToString() + "sioc.axd?sioc_type=comment&sioc_id=" + id;
		}

		#endregion

		#region Write XML

		private static Dictionary<string, string> xmlNamespaces;
		private static Dictionary<string, string> SupportedNamespaces
		{
			get
			{
				if (xmlNamespaces == null)
				{
					xmlNamespaces = new Dictionary<string, string>();

					xmlNamespaces.Add("foaf", "http://xmlns.com/foaf/0.1/");
					xmlNamespaces.Add("rss", "http://purl.org/rss/1.0/");
					xmlNamespaces.Add("admin", "http://webns.net/mvcb/");
					xmlNamespaces.Add("dc", "http://purl.org/dc/elements/1.1/");
					xmlNamespaces.Add("dcterms", "http://purl.org/dc/terms/");
					xmlNamespaces.Add("rdfs", "http://www.w3.org/2000/01/rdf-schema#");
					xmlNamespaces.Add("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
					xmlNamespaces.Add("content", "http://purl.org/rss/1.0/modules/content");
					xmlNamespaces.Add("sioc", "http://rdfs.org/sioc/ns#");
				}

				return xmlNamespaces;
			}
		}

		private XmlWriter GetWriter(Stream stream)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
			XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"); //"http://xmlns.com/foaf/0.1/");

			foreach (string prefix in SupportedNamespaces.Keys)
			{
				xmlWriter.WriteAttributeString("xmlns", prefix, null, SupportedNamespaces[prefix]);
			}

			return xmlWriter;
		}

		private void CloseWriter(XmlWriter xmlWriter)
		{
			xmlWriter.WriteEndElement(); // rdf:RDF
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();
		}

		private void WriteSite(Stream stream, List<IPublishable> list)
		{
			XmlWriter xmlWriter = GetWriter(stream);

			WriteUserGroup(xmlWriter);
			WriteFoafDocument(xmlWriter, "site", Utils.AbsoluteWebRoot.ToString());
			WriteSiocSite(xmlWriter);
			WriteForum(xmlWriter, list);

			CloseWriter(xmlWriter);
		}

		private void WriteUserGroup(XmlWriter xmlWriter)
		{
			xmlWriter.WriteStartElement("sioc", "Usergroup", null);

			xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocAuthorsUrl());
			xmlWriter.WriteElementString("dc", "title", null, "Authors at \"" + BlogSettings.Instance.Name + "\"");

			int count = 0;
			MembershipUserCollection members = Membership.Provider.GetAllUsers(0, 999, out count);

			foreach (MembershipUser user in members)
			{
				xmlWriter.WriteStartElement("sioc", "has_member", null);
				xmlWriter.WriteStartElement("sioc", "User", null);
				xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(user.UserName));
				xmlWriter.WriteAttributeString("rdfs", "label", null, user.UserName);

				xmlWriter.WriteStartElement("sioc", "see_also", null);
				xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(user.UserName));
				xmlWriter.WriteEndElement(); //sioc:see_also

				xmlWriter.WriteEndElement(); //sioc:User
				xmlWriter.WriteEndElement(); //sioc:has_member
			}

			xmlWriter.WriteEndElement(); //foaf:Document
		}

		private void WriteFoafDocument(XmlWriter xmlWriter, string siocType, string url)
		{
			string title = "SIOC " + siocType + " profile for \"" + BlogSettings.Instance.Name + "\"";
			string description = "A SIOC profile describes the structure and contents of a weblog in a machine readable form. For more information please refer to http://sioc-project.org/.";

			xmlWriter.WriteStartElement("foaf", "Document", null);
			xmlWriter.WriteAttributeString("rdf", "about", null, Utils.AbsoluteWebRoot.ToString());

			xmlWriter.WriteElementString("dc", "title", null, title);
			xmlWriter.WriteElementString("dc", "description", null, description);
			xmlWriter.WriteElementString("foaf", "primaryTopic", null, url);
			xmlWriter.WriteElementString("admin", "generatorAgent", null, "BlogEngine.NET" + BlogSettings.Instance.Version());

			xmlWriter.WriteEndElement(); //foaf:Document
		}

		private void WriteSiocSite(XmlWriter xmlWriter)
		{
			xmlWriter.WriteStartElement("sioc", "Site", null);
			xmlWriter.WriteAttributeString("rdf", "about", null, Utils.AbsoluteWebRoot.ToString());

			xmlWriter.WriteElementString("dc", "title", null, BlogSettings.Instance.Name);
			xmlWriter.WriteElementString("dc", "description", null, BlogSettings.Instance.Description);
			xmlWriter.WriteElementString("sioc", "link", null, Utils.AbsoluteWebRoot.ToString());
			xmlWriter.WriteElementString("sioc", "host_of", null, GetSiocBlogUrl());
			xmlWriter.WriteElementString("sioc", "has_group", null, GetSiocAuthorsUrl());

			xmlWriter.WriteEndElement(); //sioc:Site
		}

		private void WriteForum(XmlWriter xmlWriter, List<IPublishable> list)
		{
			xmlWriter.WriteStartElement("sioc", "Forum", null);

			xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocBlogUrl());
			xmlWriter.WriteElementString("sioc", "name", null, BlogSettings.Instance.Name);
			xmlWriter.WriteStartElement("sioc", "link", null);
			xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocBlogUrl());
			xmlWriter.WriteEndElement();

			foreach (IPublishable pub in list)
			{
				xmlWriter.WriteStartElement("sioc", "container_of", null);
				xmlWriter.WriteStartElement("sioc", "Post", null);
				xmlWriter.WriteAttributeString("rdf", "about", null, pub.AbsoluteLink.ToString());
				xmlWriter.WriteAttributeString("dc", "title", null, pub.Title);

				xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
				xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocPostUrl(pub.Id.ToString()));
				xmlWriter.WriteEndElement(); //sioc:Post

				xmlWriter.WriteEndElement(); //sioc:Post
				xmlWriter.WriteEndElement(); //sioc:Forum
			}

			xmlWriter.WriteEndElement(); //sioc:Forum
		}

		private void WritePub(Stream stream, IPublishable pub)
		{
			XmlWriter xmlWriter = GetWriter(stream);

			if (pub is Post)
				WriteFoafDocument(xmlWriter, "post", pub.AbsoluteLink.ToString());
			else
				WriteFoafDocument(xmlWriter, "comment", pub.AbsoluteLink.ToString());

			WriteSiocPost(xmlWriter, pub);
			CloseWriter(xmlWriter);
		}

		private void WriteSiocPost(XmlWriter xmlWriter, IPublishable pub)
		{
			xmlWriter.WriteStartElement("sioc", "Post", null);
			xmlWriter.WriteAttributeString("rdf", "about", null, pub.AbsoluteLink.ToString());

			xmlWriter.WriteStartElement("sioc", "link", null);
			xmlWriter.WriteAttributeString("rdf", "resource", null, pub.AbsoluteLink.ToString());
			xmlWriter.WriteEndElement(); //sioc:link

			xmlWriter.WriteStartElement("sioc", "has_container", null);
			xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocBlogUrl());
			xmlWriter.WriteEndElement(); //sioc:has_container

			xmlWriter.WriteElementString("dc", "title", null, pub.Title);

			// SIOC:USER
			if (pub is Post)
			{
				xmlWriter.WriteStartElement("sioc", "has_creator", null);
				xmlWriter.WriteStartElement("sioc", "User", null);
				xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(pub.Author));
				xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
				xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(pub.Author));
				xmlWriter.WriteEndElement(); //rdf:about
				xmlWriter.WriteEndElement(); //sioc:User
				xmlWriter.WriteEndElement(); //sioc:has_creator
			}

			// FOAF:maker
			xmlWriter.WriteStartElement("foaf", "maker", null);
			xmlWriter.WriteStartElement("foaf", "Person", null);
			if (pub is Post)
				xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(pub.Author));

			xmlWriter.WriteAttributeString("foaf", "name", null, pub.Author);

			if (pub is Post)
			{
				MembershipUser user = Membership.GetUser(pub.Author);
				xmlWriter.WriteElementString("foaf", "mbox_sha1sum", null, (user != null) ? CalculateSHA1(user.Email, Encoding.UTF8) : "");
			}
			else
			{
				xmlWriter.WriteElementString("foaf", "mbox_sha1sum", null, CalculateSHA1(((Comment)pub).Email, Encoding.UTF8));
			}

			xmlWriter.WriteStartElement("foaf", "homepage", null);
			if (pub is Post)
				xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot.ToString());
			else
				xmlWriter.WriteAttributeString("rdf", "resource", null, "TODO:");
			xmlWriter.WriteEndElement(); //foaf:homepage

			if (pub is Post)
			{
				xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
				xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(pub.Author));
				xmlWriter.WriteEndElement(); //rdfs:seeAlso
			}

			xmlWriter.WriteEndElement(); //foaf:Person
			xmlWriter.WriteEndElement(); //foaf:maker

			// CONTENT
			//xmlWriter.WriteElementString("dcterms", "created", null, DpUtility.ToW3cDateTime(pub.DateCreated));
			xmlWriter.WriteElementString("sioc", "content", null, Utils.StripHtml(pub.Content));
			xmlWriter.WriteElementString("content", "encoded", null, pub.Content);

			// TOPICS
			if (pub is Post)
			{
				// categories
				foreach (Category category in ((Post)pub).Categories)
				{
					xmlWriter.WriteStartElement("sioc", "topic", null);
					xmlWriter.WriteAttributeString("rdfs", "label", null, category.Title);
					xmlWriter.WriteAttributeString("rdf", "resource", null, category.AbsoluteLink.ToString());
					xmlWriter.WriteEndElement(); //sioc:topic
				}

				// tags are also supposed to be treated as sioc:topic 
				foreach (string tag in ((Post)pub).Tags)
				{
					xmlWriter.WriteStartElement("sioc", "topic", null);
					xmlWriter.WriteAttributeString("rdfs", "label", null, tag);
					xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot.ToString() + "?tag=/" + tag);
					xmlWriter.WriteEndElement(); //sioc:topic
				}

				// COMMENTS
				foreach (Comment comment in ((Post)pub).ApprovedComments)
				{
					xmlWriter.WriteStartElement("sioc", "has_reply", null);
					xmlWriter.WriteStartElement("sioc", "Post", null);
					xmlWriter.WriteAttributeString("rdf", "about", null, comment.AbsoluteLink.ToString());

					xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
					xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocCommentUrl(comment.Id.ToString()));
					xmlWriter.WriteEndElement(); //rdfs:seeAlso

					xmlWriter.WriteEndElement(); //sioc:Post
					xmlWriter.WriteEndElement(); //sioc:has_reply
				}

				// TODO: LINKS
				MatchCollection linkMatches = Regex.Matches(pub.Content, @"<a[^(href)]?href=""([^""]+)""[^>]?>([^<]+)</a>");
				List<string> linkPairs = new List<string>();

				foreach (Match linkMatch in linkMatches)
				{
					string url = linkMatch.Groups[1].Value;
					string text = linkMatch.Groups[2].Value;

					if (url.IndexOf(Utils.AbsoluteWebRoot.ToString()) != 0)
					{
						string pair = url + "|" + text;
						if (!linkPairs.Contains(pair))
						{
							xmlWriter.WriteStartElement("sioc", "links_to", null);
							xmlWriter.WriteAttributeString("rdf", "resource", null, url);
							xmlWriter.WriteAttributeString("rdfs", "label", null, text);
							xmlWriter.WriteEndElement(); //sioc:links_to

							linkPairs.Add(pair);
						}
					}
				}
			}

			xmlWriter.WriteEndElement(); //sioc:Post
		}

		private void WriteAuthor(Stream stream, string authorName)
		{
			XmlWriter xmlWriter = GetWriter(stream);

			WriteFoafDocument(xmlWriter, "user", GetSiocAuthorUrl(authorName));

			MembershipUser user = Membership.GetUser(authorName);
            AuthorProfile ap = AuthorProfile.GetProfile(authorName);
			// FOAF:Person
			xmlWriter.WriteStartElement("foaf", "Person", null);
			xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocAuthorUrl(authorName));

			xmlWriter.WriteElementString("foaf", "Name", null, authorName);
            if (ap != null && !ap.IsPrivate && ap.FirstName != String.Empty)
                xmlWriter.WriteElementString("foaf", "firstName", null,ap.FirstName);
            if (ap != null && !ap.IsPrivate && ap.LastName != String.Empty)
                xmlWriter.WriteElementString("foaf", "surname", null, ap.LastName);
            xmlWriter.WriteElementString("foaf", "mbox_sha1sum", null, (user != null) ? CalculateSHA1(user.Email, Encoding.UTF8) : "");
			xmlWriter.WriteStartElement("foaf", "homepage", null);
			xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot.ToString());
			xmlWriter.WriteEndElement(); //foaf:homepage 

			xmlWriter.WriteStartElement("foaf", "holdsAccount", null);
			xmlWriter.WriteAttributeString("rdf", "resource", null, GetBlogAuthorUrl(authorName));
			xmlWriter.WriteEndElement(); //foaf:holdsAccount  

			xmlWriter.WriteEndElement(); //foaf:Person

			//SIOC:User
			xmlWriter.WriteStartElement("sioc", "User", null);
			xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(authorName));
			xmlWriter.WriteElementString("foaf", "accountName", null, authorName);
			xmlWriter.WriteElementString("sioc", "name", null, authorName);
			//xmlWriter.WriteElementString("dcterms", "created", null, "TODO:" + authorName);

			xmlWriter.WriteEndElement(); //sioc:User

			CloseWriter(xmlWriter);
		}

		#endregion

		#region Helper methods

		private static IPublishable ConvertToIPublishable(IPublishable item)
		{
			return item;
		}

		private Comment GetComment(Guid commentID)
		{
			List<Post> posts = Post.Posts;
			foreach (Post post in posts)
			{

				foreach (Comment comm in post.Comments)
				{
					if (comm.Id == commentID)
					{
						return comm;
					}
				}

			}
			return null;
		}

		private static string CalculateSHA1(string text, Encoding enc)
		{
			byte[] buffer = enc.GetBytes(text);
			SHA1CryptoServiceProvider cryptoTransformSHA1 =
			new SHA1CryptoServiceProvider();
			string hash = BitConverter.ToString(
				cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");

			return hash.ToLower();
		}

		#endregion

	}
}
