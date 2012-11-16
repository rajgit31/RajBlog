#region Using

using System;
using System.Xml;
using System.IO;
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

    /// <summary>
    /// Loads the ping services.
    /// </summary>
    /// <returns></returns>
    public override StringCollection LoadPingServices()
    {
      string fileName = _Folder + "pingservices.xml";
      if (!File.Exists(fileName))
        return new StringCollection();

      StringCollection col = new StringCollection();
      XmlDocument doc = new XmlDocument();
      doc.Load(fileName);

      foreach (XmlNode node in doc.SelectNodes("services/service"))
      {
        if (!col.Contains(node.InnerText))
          col.Add(node.InnerText);
      }

      return col;
    }

    /// <summary>
    /// Saves the ping services.
    /// </summary>
    /// <param name="services">The services.</param>
    public override void SavePingServices(StringCollection services)
    {
      if (services == null)
        throw new ArgumentNullException("services");

      string fileName = _Folder + "pingservices.xml";

      using (XmlTextWriter writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
      {
        writer.Formatting = Formatting.Indented;
        writer.Indentation = 4;
        writer.WriteStartDocument(true);
        writer.WriteStartElement("services");

        foreach (string service in services)
        {
          writer.WriteElementString("service", service);
        }

        writer.WriteEndElement();
      }
    }

  }
}
