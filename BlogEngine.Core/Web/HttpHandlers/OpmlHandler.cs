#region Using

using System;
using System.Web;
using System.Xml;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class OpmlHandler : IHttpHandler
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
            context.Response.ContentType = "text/xml";

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Encoding = System.Text.Encoding.UTF8;
            writerSettings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(context.Response.OutputStream, writerSettings))
            {
                // open OPML
                writer.WriteStartElement("opml");

                writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("version", "1.0");

                // open BODY
                writer.WriteStartElement("body");

                foreach (BlogRollItem br in BlogRollItem.BlogRolls)
                {
                    // open OUTLINE
                    writer.WriteStartElement("outline");

                    if (!string.IsNullOrEmpty(br.Xfn))
                        writer.WriteAttributeString("xfn", br.Xfn);

                    writer.WriteAttributeString("title", br.Title);
                    writer.WriteAttributeString("description", br.Description);
                    writer.WriteAttributeString("xmlUrl", br.FeedUrl.ToString());
                    writer.WriteAttributeString("htmlUrl", br.BlogUrl.ToString());

                    // close OUTLINE
                    writer.WriteEndElement();
                }

                // close BODY
                writer.WriteEndElement();

                // close OPML
                writer.WriteEndElement();
            }
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