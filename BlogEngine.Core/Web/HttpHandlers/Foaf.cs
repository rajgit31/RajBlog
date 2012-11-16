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

using BlogEngine;
using BlogEngine.Core;
using BlogEngine.Core.DataStore;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
	/// <summary>
	/// Based on John Dyer's (http://johndyer.name/) extension.
	/// </summary>
	public class Foaf : IHttpHandler
	{

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
			// attempt to grab the username from the URL
			// where URL = www.mysite.com/foaf_admin.axd
			// username = 'admin'
			string filename = context.Request.Url.ToString();
			string name = filename.Substring(filename.LastIndexOf("/") + 1).Replace(".axd", "").Replace("foaf_", "").Replace("foaf", "");

			// if no name is specificied, then grab the first user from Membership
			if (name == "")
			{
				foreach (MembershipUser user in Membership.GetAllUsers())
				{
					name = user.UserName;
					break;
				}
			}

			context.Response.ContentType = "application/rdf+xml";
			WriteFoaf(context, name);
		}

		private static Dictionary<string, string> xmlNamespaces;
		private static Dictionary<string, string> SupportedNamespaces
		{
			get
			{
				if (xmlNamespaces == null)
				{
					xmlNamespaces = new Dictionary<string, string>();

					xmlNamespaces.Add("foaf", "http://xmlns.com/foaf/0.1/");
					xmlNamespaces.Add("admin", "http://webns.net/mvcb/");
					xmlNamespaces.Add("rdfs", "http://www.w3.org/2000/01/rdf-schema#");
				}

				return xmlNamespaces;
			}
		}


		/// <summary>
		/// Writes FOAF data to the output stream
		/// </summary>
		/// <param name="context"></param>
		/// <param name="name"></param>
		private void WriteFoaf(HttpContext context, string name)
		{
			// begin FOAF
			XmlWriter writer = GetWriter(context.Response.OutputStream);

			// write DOCUMENT
			writer.WriteStartElement("foaf", "PersonalProfileDocument", null);
			writer.WriteAttributeString("rdf", "about", null, "");
			writer.WriteStartElement("foaf", "maker", null);
			writer.WriteAttributeString("rdf", "resource", null, "#me");
			writer.WriteEndElement(); // foaf:maker
			writer.WriteStartElement("foaf", "primaryTopic", null);
			writer.WriteAttributeString("rdf", "resource", null, "#me");
			writer.WriteEndElement(); // foaf:primaryTopic
			writer.WriteEndElement();  // foaf:PersonalProfileDocument


			// get main person's data
			AuthorProfile ap = AuthorProfile.GetProfile(name);

			if (!ap.IsPrivate)
			{

				// main author object
				FoafPerson me = new FoafPerson("#me", ap);
				me.Friends = new List<FoafPerson>();


				// TODO: this really should be it's own data store

				// assume all other authors are friends
				foreach (MembershipUser user in Membership.GetAllUsers())
				{
					if (!user.UserName.Equals(name, StringComparison.OrdinalIgnoreCase))
					{
						AuthorProfile friend = AuthorProfile.GetProfile(user.UserName);
						if (friend != null)
						{
								me.Friends.Add(new FoafPerson("#" + user.UserName, friend));
						}
					}
				}

				// assume blog roll = friends
				foreach (BlogRollItem br in BlogRollItem.BlogRolls)
				{
					string title = br.Title;
					string url = br.BlogUrl.ToString();

					FoafPerson foaf = new FoafPerson(title);
					foaf.Name = title;
					foaf.Blog = url;

					if (context.Cache["foaf:" + title] == null)
					{
						Dictionary<Uri, XmlDocument> docs = Utils.FindSemanticDocuments(new Uri(url), "foaf");
						if (docs.Count > 0)
						{
							foreach (Uri key in docs.Keys)
							{
								context.Cache.Insert("foaf:" + title, key.ToString());
								break;
							}
						}
						else
						{
							context.Cache.Insert("foaf:" + title, "0");
						}
					}

					string seeAlso = (string)context.Cache["foaf:" + title];
					if (seeAlso != null && seeAlso.Contains("://"))
						foaf.Rdf = seeAlso;

					me.Friends.Add(foaf);
				}


				// begin writing FOAF Persons
				WriteFoafPerson(writer, me);
			}
			CloseWriter(writer);
		}

		/// <summary>
		/// Creates the necessary startup information for the FOAF document
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		private XmlWriter GetWriter(Stream stream)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
			XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

			foreach (string prefix in SupportedNamespaces.Keys)
			{
				xmlWriter.WriteAttributeString("xmlns", prefix, null, SupportedNamespaces[prefix]);
			}

			return xmlWriter;
		}

		/// <summary>
		/// Closes up the FOAF document
		/// </summary>
		/// <param name="xmlWriter"></param>
		private void CloseWriter(XmlWriter xmlWriter)
		{
			xmlWriter.WriteEndElement(); // rdf:RDF
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();
		}

		/// <summary>
		/// Write a FOAF:Person and any friends to the output stream
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="person"></param>
		private void WriteFoafPerson(XmlWriter writer, FoafPerson person)
		{
			writer.WriteStartElement("foaf", "Person", null);
			//if (person.ID != "")
			//{
			//    writer.WriteAttributeString("rdf", "ID", null, person.ID);
			//}


			writer.WriteElementString("foaf", "name", null, person.Name);
			if (person.Title != "")
			{
				writer.WriteElementString("foaf", "title", null, person.Title);
			}
			if (person.Firstname != "")
			{
				writer.WriteElementString("foaf", "givenname", null, person.Firstname);
			}
			if (person.Lastname != "")
			{
				writer.WriteElementString("foaf", "family_name", null, person.Lastname);
			}
			if (!string.IsNullOrEmpty(person.Email))
			{
				writer.WriteElementString("foaf", "mbox_sha1sum", null, CalculateSHA1(person.Email, Encoding.UTF8));
			}
			if (!string.IsNullOrEmpty(person.Homepage))
			{
				writer.WriteStartElement("foaf", "homepage", null);
				writer.WriteAttributeString("rdf", "resource", null, person.Homepage);
				writer.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(person.Blog))
			{
				writer.WriteStartElement("foaf", "weblog", null);
				writer.WriteAttributeString("rdf", "resource", null, person.Blog);
				writer.WriteEndElement();
			}

			if (person.Rdf != "" && person.Rdf != HttpContext.Current.Request.Url.ToString())
			{
				writer.WriteStartElement("rdfs", "seeAlso", null);
				writer.WriteAttributeString("rdf", "resource", null, person.Rdf);
				writer.WriteEndElement();
			}
			
			if (!string.IsNullOrEmpty(person.Birthday))
			{
				writer.WriteElementString("foaf", "birthday", null, person.Birthday);
			}

			if (!string.IsNullOrEmpty(person.PhotoUrl))
			{
				writer.WriteStartElement("foaf", "depiction", null);
				writer.WriteAttributeString("rdf", "resource", null, person.PhotoUrl);
				writer.WriteEndElement();
			}

			if (!string.IsNullOrEmpty(person.Phone))
			{
				writer.WriteElementString("foaf", "phone", null, person.Phone);
			}

			if (person.Friends != null && person.Friends.Count > 0)
			{

				foreach (FoafPerson friend in person.Friends)
				{
					writer.WriteStartElement("foaf", "knows", null);

					WriteFoafPerson(writer, friend);

					writer.WriteEndElement();  // foaf:knows
				}


			}
			writer.WriteEndElement(); // foaf:Person
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

	}

}

/// <summary>
/// Temporary class for transmitting FOAF attributes
/// </summary>
public class FoafPerson
{

	public FoafPerson(string id)
	{
		ID = id;
	}

	public FoafPerson(string id, BlogEngine.Core.AuthorProfile ap)
	{
		ID = "#me";
		Name = ap.FullName;
		Email = ap.EmailAddress;
		// no homepage
		// this website = blog
		Blog = Utils.AbsoluteWebRoot.ToString();
		Rdf = Utils.AbsoluteWebRoot.ToString() + "foaf_" + ap.UserName + ".axd";
		Firstname = ap.FirstName;
		Lastname = ap.LastName;
		Image = ap.PhotoURL;
		Birthday = ap.Birthday.ToString("yyyy-MM-dd");
		Phone = ap.PhoneMain;
		PhotoUrl = ap.PhotoURL;
	}

	public FoafPerson(string id, string name, string title, string email, string homepage, string blog, string rdf, string firstname, string lastname, string image, string birthday, string phone)
	{
		ID = id;
		Name = name;
		Title = title;
		Email = email;
		Homepage = homepage;
		Blog = blog;
		Rdf = rdf;
		Firstname = firstname;
		Lastname = lastname;
		Image = image;
		Birthday = birthday;
		Phone = phone;
	}

	public string ID = String.Empty;
	public string Name = String.Empty;
	public string Email = String.Empty;
	public string Homepage = String.Empty;
	public string Blog = String.Empty;
	public string Rdf = String.Empty;
	public string Firstname = String.Empty;
	public string Lastname = String.Empty;
	public string Image = String.Empty;
	public string Title = String.Empty;
	public string Birthday = String.Empty;
	public string Phone = String.Empty;
	public string PhotoUrl = string.Empty;
	public List<FoafPerson> Friends;
}