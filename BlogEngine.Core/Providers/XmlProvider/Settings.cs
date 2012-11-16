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

        /// <summary>
        /// The storage location is to allow Blog Providers to use alternative storage locations that app_data root directory.
        /// </summary>
        /// <returns></returns>
        public override string StorageLocation()
        {
           if(String.IsNullOrEmpty(System.Web.Configuration.WebConfigurationManager.AppSettings["StorageLocation"]))
               return @"~/app_data/";
            return System.Web.Configuration.WebConfigurationManager.AppSettings["StorageLocation"];

        }
        /// <summary>
        /// Loads the settings from the provider.
        /// </summary>
        public override StringDictionary LoadSettings()
        {
            //string filename = System.Web.HttpContext.Current.Server.MapPath(Utils.RelativeWebRoot + "App_Data/settings.xml");
            string filename = System.Web.HttpContext.Current.Server.MapPath(StorageLocation() + "settings.xml");
            StringDictionary dic = new StringDictionary();

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            foreach (XmlNode settingsNode in doc.SelectSingleNode("settings").ChildNodes)
            {
                string name = settingsNode.Name;
                string value = settingsNode.InnerText;

                dic.Add(name, value);
            }

            return dic;
        }

        /// <summary>
        /// Saves the settings to the provider.
        /// </summary>
        public override void SaveSettings(StringDictionary settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            string filename = _Folder + "settings.xml";
            XmlWriterSettings writerSettings = new XmlWriterSettings(); ;
            writerSettings.Indent = true;

            //------------------------------------------------------------
            //    Create XML writer against file path
            //------------------------------------------------------------
            using (XmlWriter writer = XmlWriter.Create(filename, writerSettings))
            {
                writer.WriteStartElement("settings");

                foreach (string key in settings.Keys)
                {
                    writer.WriteElementString(key, settings[key]);
                }

                writer.WriteEndElement();
            }
        }

    }
}
