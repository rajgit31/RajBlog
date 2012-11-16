using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using BlogEngine.Core;

namespace BlogEngine.Core.Web.HttpHandlers
{
  /// <summary>
  /// RSD (Really Simple Discoverability) Handler
  /// http://cyber.law.harvard.edu/blogs/gems/tech/rsd.html
  /// </summary>
  public class RsdHandler : IHttpHandler
  {

    #region IHttpHandler Members
    /// <summary>
    /// IsReusable implmentation for IHttpHandler
    /// </summary>
    public bool IsReusable
    {
      get { return false; }
    }

    /// <summary>
    /// Process to return RSD page.
    /// </summary>
    /// <param name="context">context</param>
    public void ProcessRequest(HttpContext context)
    {      
      context.Response.ContentType = "text/xml";
      using (XmlTextWriter rsd = new XmlTextWriter(context.Response.OutputStream, Encoding.UTF8))
      {
        rsd.Formatting = Formatting.Indented;
        rsd.WriteStartDocument();

        // Rsd tag
        rsd.WriteStartElement("rsd");
        rsd.WriteAttributeString("version", "1.0");

        // Service 
        rsd.WriteStartElement("service");
        rsd.WriteElementString("engineName", "BlogEngine.NET " + BlogSettings.Instance.Version());
        rsd.WriteElementString("engineLink", "http://dotnetblogengine.com");
        rsd.WriteElementString("homePageLink", Utils.AbsoluteWebRoot.ToString());

        // APIs
        rsd.WriteStartElement("apis");

        // MetaWeblog
        rsd.WriteStartElement("api");
        rsd.WriteAttributeString("name", "MetaWeblog");
        rsd.WriteAttributeString("preferred", "true");
				string prefix = BlogSettings.Instance.RequireSSLMetaWeblogAPI ? "https://" : "http://";
				rsd.WriteAttributeString("apiLink", prefix + context.Request.Url.Authority + Utils.RelativeWebRoot + "metaweblog.axd");
        rsd.WriteAttributeString("blogID", Utils.AbsoluteWebRoot.ToString());
        rsd.WriteEndElement();

        // BlogML
        rsd.WriteStartElement("api");
        rsd.WriteAttributeString("name", "BlogML");
        rsd.WriteAttributeString("preferred", "false");
        rsd.WriteAttributeString("apiLink", Utils.AbsoluteWebRoot + "api/BlogImporter.asmx");
        rsd.WriteAttributeString("blogID", Utils.AbsoluteWebRoot.ToString());
        rsd.WriteEndElement();

        // End APIs
        rsd.WriteEndElement();

        // End Service
        rsd.WriteEndElement();

        // End Rsd
        rsd.WriteEndElement();

        rsd.WriteEndDocument();

      }
    }

    #endregion
  }
}
