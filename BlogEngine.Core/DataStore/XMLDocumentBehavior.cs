using System;
using System.Xml;
using System.Xml.Serialization;
using BlogEngine.Core.Providers;
using System.IO;
using System.Configuration;
using System.Text;

namespace BlogEngine.Core.DataStore
{
  /// <summary>
  /// Class to encapsulate saving and retreaving 
  /// XML documents to and from data storage
  /// </summary>
  public class XMLDocumentBehavior : ISettingsBehavior   
  {
    private static BlogProviderSection _section = (BlogProviderSection)ConfigurationManager.GetSection("BlogEngine/blogProvider");

    /// <summary>
    /// Default constructor
    /// </summary>
    public XMLDocumentBehavior()
    {
      //
      // TODO: Add constructor logic here
      //
    }

    /// <summary>
    /// Saves XML document to data storage
    /// </summary>
    /// <param name="exType">Extension Type</param>
    /// <param name="exId">Extension ID</param>
    /// <param name="settings">Settings as XML document</param>
    /// <returns>True if saved</returns>
    public bool SaveSettings(ExtensionType exType, string exId, object settings)
    {
      try
      {
        XmlDocument xml = (XmlDocument)settings;

        if (_section.DefaultProvider == "XmlBlogProvider")
        {
          BlogService.SaveToDataStore(exType, exId, xml);
        }
        else
        {
          WidgetData wd = new WidgetData();
          wd.Settings = xml.InnerXml;
          BlogService.SaveToDataStore(exType, exId, wd);
        }
        return true;
      }
      catch (Exception)
      {
        throw;
      }
    }

    /// <summary>
    /// Gets settings from data store
    /// </summary>
    /// <param name="exType">Extension Type</param>
    /// <param name="exId">Extension ID</param>
    /// <returns>Settings as Stream</returns>
    public object GetSettings(ExtensionType exType, string exId)
    {
      WidgetData wd = new WidgetData();
      XmlDocument xml = new XmlDocument();

      if (_section.DefaultProvider == "XmlBlogProvider")
      {
        Stream stm = (Stream)BlogService.LoadFromDataStore(exType, exId);
        if (stm != null)
        {
          XmlSerializer x = new XmlSerializer(typeof(XmlDocument));
          xml = (XmlDocument)x.Deserialize(stm);
          stm.Close();
        }
      }
      else
      {
        object o = BlogService.LoadFromDataStore(exType, exId);
        if (!string.IsNullOrEmpty((string)o))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(WidgetData));
          using (StringReader reader = new StringReader((string)o))
          {
            wd = (WidgetData)serializer.Deserialize(reader);
          }

          if (wd.Settings.Length > 0)
            xml.InnerXml = wd.Settings;
        }
      }
      return xml;
    }
  }
  /// <summary>
  /// Wrap around xml document
  /// </summary>
  [Serializable()]
  public class WidgetData 
  {
    /// <summary>
    /// Defatul constructor
    /// </summary>
    public WidgetData() { }

    private string settings = string.Empty;
    /// <summary>
    /// Settings data
    /// </summary>
    public string Settings { get { return settings; } set { settings = value; } }
  }
}
