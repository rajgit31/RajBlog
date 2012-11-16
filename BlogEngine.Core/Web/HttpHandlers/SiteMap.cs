#region Using

using System;
using System.Xml;
using System.Web;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
  /// <summary>
  /// A blog sitemap suitable for Google Sitemap as well as
  /// other big search engines such as MSN/Live, Yahoo and Ask.
  /// </summary>
  public class SiteMap : IHttpHandler
  {

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
      using (XmlWriter writer = XmlWriter.Create(context.Response.OutputStream))
      {
        writer.WriteStartElement("urlset", "http://www.google.com/schemas/sitemap/0.84");

        // Posts
				foreach (Post post in Post.Posts)
				{
                    if (post.IsVisibleToPublic)
					{
						writer.WriteStartElement("url");
						writer.WriteElementString("loc", post.AbsoluteLink.ToString());
						writer.WriteElementString("lastmod", post.DateModified.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
						writer.WriteElementString("changefreq", "monthly");
						writer.WriteEndElement();
					}
				}

        // Pages
				foreach (Page page in Page.Pages)
				{
					if (page.IsVisibleToPublic)
					{
						writer.WriteStartElement("url");
						writer.WriteElementString("loc", page.AbsoluteLink.ToString());
						writer.WriteElementString("lastmod", page.DateModified.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
						writer.WriteElementString("changefreq", "monthly");
						writer.WriteEndElement();
					}
				}

				// Removed for SEO reasons
				//// Archive
				//writer.WriteStartElement("url");
				//writer.WriteElementString("loc", Utils.AbsoluteWebRoot.ToString() + "archive.aspx");
				//writer.WriteElementString("lastmod", DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
				//writer.WriteElementString("changefreq", "daily");
				//writer.WriteEndElement();

        // Contact
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", Utils.AbsoluteWebRoot.ToString() + "contact.aspx");
        writer.WriteElementString("lastmod", DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteElementString("changefreq", "monthly");
        writer.WriteEndElement();

        // Blog
        if (Page.GetFrontPage() != null)
        {
          writer.WriteStartElement("url");
          writer.WriteElementString("loc", Utils.AbsoluteWebRoot.ToString() + "blog.aspx");
          writer.WriteElementString("lastmod", DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
          writer.WriteElementString("changefreq", "daily");
          writer.WriteEndElement();
        }

        writer.WriteEndElement();
      }

      context.Response.ContentType = "text/xml";
    }

    /// <summary>
    /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
    /// </summary>
    /// <value></value>
    /// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
    public bool IsReusable
    {
      get { return false; }
    }

  }
}