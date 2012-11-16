using System;
using System.IO;
using System.Text;
using BlogEngine.Core.DataStore;
using System.Xml;
using System.Xml.Serialization;
using System.Web.Hosting;

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
    /// Loads settings from generic data store
    /// </summary>
    /// <param name="exType">Extension Type</param>
    /// <param name="exId">Extension ID</param>
    /// <returns>Stream Settings</returns>
    public override object LoadFromDataStore(ExtensionType exType, string exId)
    {
      string _fileName = StorageLocation(exType) + exId + ".xml";
      StreamReader reader = null;
      Stream str = null;
      try
      {
        if (!Directory.Exists(StorageLocation(exType)))
          Directory.CreateDirectory(StorageLocation(exType));

        if (File.Exists(_fileName))
        {
          reader = new StreamReader(_fileName);
          str = reader.BaseStream;
        }
      }
      catch (Exception)
      {
        throw;
      }
      return str;
    }

    /// <summary>
    /// Save settings to generic data store
    /// </summary>
    /// <param name="exType">Type of extension</param>
    /// <param name="exId">Extension ID</param>
    /// <param name="settings">Stream Settings</param>
    public override void SaveToDataStore(ExtensionType exType, string exId, object settings)
    {
      string _fileName = StorageLocation(exType) + exId + ".xml";
      try
      {
        if (!Directory.Exists(StorageLocation(exType)))
          Directory.CreateDirectory(StorageLocation(exType));

        TextWriter writer = new StreamWriter(_fileName);
        XmlSerializer x = new XmlSerializer(settings.GetType());
        x.Serialize(writer, settings);
        writer.Close();
      }
      catch (Exception e)
      {
        string s = e.Message;
        throw;
      }
    }

    /// <summary>
    /// Removes settings from data store
    /// </summary>
    /// <param name="exType">Extension Type</param>
    /// <param name="exId">Extension Id</param>
    public override void RemoveFromDataStore(DataStore.ExtensionType exType, string exId)
    {
      string _fileName = StorageLocation(exType) + exId + ".xml";
      try
      {
        File.Delete(_fileName);
      }
      catch (Exception e)
      {
        string s = e.Message;
        throw;
      }
    }

    /// <summary>
    /// Data Store Location
    /// </summary>
    /// <param name="exType">Type of extension</param>
    /// <returns>Path to storage directory</returns>
    private string StorageLocation(ExtensionType exType)
    {
      switch (exType)
      {
        case ExtensionType.Extension:
          return HostingEnvironment.MapPath(Path.Combine(BlogSettings.Instance.StorageLocation, @"datastore\extensions\"));
        case ExtensionType.Widget:
          return HostingEnvironment.MapPath(Path.Combine(BlogSettings.Instance.StorageLocation, @"datastore\widgets\"));
        case ExtensionType.Theme:
          return HostingEnvironment.MapPath(Path.Combine(BlogSettings.Instance.StorageLocation, @"datastore\themes\"));
      }
      return string.Empty;
    }
  }
}

