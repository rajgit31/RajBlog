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
	public class Apml : IHttpHandler
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
			WriteApmlDocument(context.Response.OutputStream);
		}

		#endregion

		#region Write XML

		private XmlWriter GetWriter(Stream stream)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
			XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("APML");

			return xmlWriter;
		}

		private void CloseWriter(XmlWriter xmlWriter)
		{
			xmlWriter.WriteEndElement(); // APML
			xmlWriter.WriteEndDocument();
			xmlWriter.Close();
		}

		private void WriteApmlDocument(Stream stream)
		{
			XmlWriter writer = GetWriter(stream);

			// HEAD
			writer.WriteStartElement("Head");
			writer.WriteElementString("Title", "APML data for " + BlogSettings.Instance.Name + " - " + BlogSettings.Instance.Description);
			writer.WriteElementString("Generator", "BlogEngine.NET " + BlogSettings.Instance.Version());
			writer.WriteElementString("UserEmail", "");
			writer.WriteElementString("DateCreated", DateTime.Now.ToString());
			writer.WriteEndElement();  // Head

			// BODY
			writer.WriteStartElement("Body");
			writer.WriteAttributeString("defaultProfile", "tags");

			// tags
			writer.WriteStartElement("Profile");
			writer.WriteAttributeString("name", "tags");
			writer.WriteStartElement("ImplicitData");
			writer.WriteStartElement("Concepts");

			List<Concept> tagList = CreateTagList();
			foreach (Concept key in tagList)
			{
				writer.WriteStartElement("Concept");
				writer.WriteAttributeString("key", key.Title);
				writer.WriteAttributeString("value", key.Score.ToString());
				writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
				writer.WriteAttributeString("updated", key.LastUpdated.ToString());
				writer.WriteEndElement();  // Concept
			}

			writer.WriteEndElement();  // Concepts
			writer.WriteEndElement();  // ImplicitData
			writer.WriteEndElement();  // Profile

			// categories
			//writer.WriteStartElement("Profile");
			//writer.WriteAttributeString("name", "categories");
			//writer.WriteStartElement("ImplicitData");
			//writer.WriteStartElement("Concepts");

			//Dictionary<string, Concept> catList = CreateCategoryList();
			//foreach (string key in catList.Keys)
			//{
			//  writer.WriteStartElement("Concept");
			//  writer.WriteAttributeString("key", key);
			//  writer.WriteAttributeString("value", catList[key].Score.ToString());
			//  writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
			//  writer.WriteAttributeString("updated", catList[key].LastUpdated.ToString());
			//  writer.WriteEndElement();  // Concept
			//}

			//writer.WriteEndElement();  // Concepts
			//writer.WriteEndElement();  // ImplicitData
			//writer.WriteEndElement();  // Profile


			// links
			writer.WriteStartElement("Profile");
			writer.WriteAttributeString("name", "links");
			writer.WriteStartElement("ExplicitData");
			writer.WriteStartElement("Concepts");

			Dictionary<string, string> links = CreateLinkList();
			if (links != null)
			{
				foreach (string title in links.Keys)
				{
					string website = links[title];
					writer.WriteStartElement("Source");
					writer.WriteAttributeString("key", website);
					writer.WriteAttributeString("name", title);
					writer.WriteAttributeString("value", "1.0");
					writer.WriteAttributeString("type", "text/html");
					writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
					writer.WriteAttributeString("updated", DateTime.Now.ToString());

					writer.WriteStartElement("Author");
					writer.WriteAttributeString("key", title);
					writer.WriteAttributeString("value", "1.0");
					writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
					writer.WriteAttributeString("updated", DateTime.Now.ToString());
					writer.WriteEndElement();  // Author

					writer.WriteEndElement();  // Source
				}
			}

			writer.WriteEndElement();  // Concepts
			writer.WriteEndElement();  // ImplicitData
			writer.WriteEndElement();  // Profile			

			writer.WriteEndElement();  // Body			

			CloseWriter(writer);
		}

		#endregion

		#region Create tag-, categorie- and link lists

		// modified version of what's happening in the tag cloud
		private List<Concept> CreateTagList()
		{
			// get all the tags and count the number of usages
			Dictionary<string, Concept> dic = new Dictionary<string, Concept>();
			foreach (Post post in Post.Posts)
			{
				if (post.IsVisible)
				{
					foreach (string tag in post.Tags)
					{
						if (dic.ContainsKey(tag))
						{
							Concept concept = dic[tag];
							concept.Score++;
							if (post.DateModified > concept.LastUpdated)
								concept.LastUpdated = post.DateModified;
							dic[tag] = concept;
						}
						else
						{
							dic[tag] = new Concept(post.DateModified, 1, tag);
						}
					}
				}
			}

			return FindMax(dic);
		}

		private static List<Concept> FindMax(Dictionary<string, Concept> dic)
		{
			float max = 0;
			foreach (Concept concept in dic.Values)
			{
				if (concept.Score > max)
					max = concept.Score;
			}

			List<Concept> list = new List<Concept>();

			// reset values as a percentage of the max
			foreach (string key in dic.Keys)
			{
				list.Add(new Concept(dic[key].LastUpdated, dic[key].Score / max, key));
			}

			list.Sort(delegate(Concept c1, Concept c2) { return c2.Score.CompareTo(c1.Score); });
			
			return list;
		}

		//// modified version of what's happening in the tag cloud
		//private Dictionary<string, Concept> CreateCategoryList()
		//{
		//  // get all the tags and count the number of usages
		//  Dictionary<string, Concept> dic = new Dictionary<string, Concept>();
		//  foreach (Post post in Post.Posts)
		//  {
		//    if (post.IsVisible)
		//    {
		//      foreach (Category cat in post.Categories)
		//      {
		//        if (dic.ContainsKey(cat.Title))
		//        {
		//          Concept concept = dic[cat.Title];
		//          concept.Score++;
		//          if (post.DateModified > concept.LastUpdated)
		//            concept.LastUpdated = post.DateModified;
		//          dic[cat.Title] = concept;
		//        }
		//        else
		//        {
		//          dic[cat.Title] = new Concept(post.DateModified, 1);
		//        }
		//      }
		//    }
		//  }

		//  return FindMax(dic);
		//}

		private Dictionary<string, string> CreateLinkList()
		{
			Dictionary<string, string> dic = new Dictionary<string, string>();

            foreach (BlogRollItem br in BlogRollItem.BlogRolls)
            {
                string title = br.Title;
				string website = br.BlogUrl.ToString();
				dic.Add(title, website);
			}

			return dic;
		}

		#endregion

		private class Concept
		{
			public Concept(DateTime lastUpdated, float score, string title)
			{
				LastUpdated = lastUpdated;
				Score = score;
				Title = title;
			}
			public DateTime LastUpdated;
			public float Score;
			public string Title;
		}

	}
}