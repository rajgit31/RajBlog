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
    /// Retrieves a Page from the data store.
    /// </summary>
    public override Page SelectPage(Guid id)
    {
      string fileName = _Folder + "pages" + Path.DirectorySeparatorChar + id.ToString() + ".xml";
      XmlDocument doc = new XmlDocument();
      doc.Load(fileName);

      Page page = new Page();

      page.Title = doc.SelectSingleNode("page/title").InnerText;
      page.Description = doc.SelectSingleNode("page/description").InnerText;
      page.Content = doc.SelectSingleNode("page/content").InnerText;
      page.Keywords = doc.SelectSingleNode("page/keywords").InnerText;

			if (doc.SelectSingleNode("page/slug") != null)
				page.Slug = doc.SelectSingleNode("page/slug").InnerText;

      if (doc.SelectSingleNode("page/parent") != null)
        page.Parent = new Guid(doc.SelectSingleNode("page/parent").InnerText);

      if (doc.SelectSingleNode("page/isfrontpage") != null)
        page.IsFrontPage = bool.Parse(doc.SelectSingleNode("page/isfrontpage").InnerText);

      if (doc.SelectSingleNode("page/showinlist") != null)
        page.ShowInList = bool.Parse(doc.SelectSingleNode("page/showinlist").InnerText);

      if (doc.SelectSingleNode("page/ispublished") != null)
        page.IsPublished = bool.Parse(doc.SelectSingleNode("page/ispublished").InnerText);

      page.DateCreated = DateTime.Parse(doc.SelectSingleNode("page/datecreated").InnerText, CultureInfo.InvariantCulture);
      page.DateModified = DateTime.Parse(doc.SelectSingleNode("page/datemodified").InnerText, CultureInfo.InvariantCulture);

      return page;
    }

    /// <summary>
    /// Inserts a new Page to the data store.
    /// </summary>
    public override void InsertPage(Page page)
    {
      if (!Directory.Exists(_Folder + "pages"))
        Directory.CreateDirectory(_Folder + "pages");

      string fileName = _Folder + "pages" + Path.DirectorySeparatorChar + page.Id.ToString() + ".xml";
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.Indent = true;

      using (XmlWriter writer = XmlWriter.Create(fileName, settings))
      {
        writer.WriteStartDocument(true);
        writer.WriteStartElement("page");

        writer.WriteElementString("title", page.Title);
        writer.WriteElementString("description", page.Description);
        writer.WriteElementString("content", page.Content);
        writer.WriteElementString("keywords", page.Keywords);
				writer.WriteElementString("slug", page.Slug);
        writer.WriteElementString("parent", page.Parent.ToString());
        writer.WriteElementString("isfrontpage", page.IsFrontPage.ToString());
        writer.WriteElementString("showinlist", page.ShowInList.ToString());
        writer.WriteElementString("ispublished", page.IsPublished.ToString());
        writer.WriteElementString("datecreated", page.DateCreated.AddHours(-BlogSettings.Instance.Timezone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        writer.WriteElementString("datemodified", page.DateModified.AddHours(-BlogSettings.Instance.Timezone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        writer.WriteEndElement();
      }
    }

    /// <summary>
    /// Updates a Page.
    /// </summary>
    public override void UpdatePage(Page page)
    {
      InsertPage(page);
    }

    /// <summary>
    /// Deletes a page from the data store.
    /// </summary>
    public override void DeletePage(Page page)
    {
      string fileName = _Folder + "pages" + Path.DirectorySeparatorChar + page.Id.ToString() + ".xml";
      if (File.Exists(fileName))
        File.Delete(fileName);

      if (Page.Pages.Contains(page))
        Page.Pages.Remove(page);
    }

    /// <summary>
    /// Retrieves all pages from the data store
    /// </summary>
    /// <returns>List of Pages</returns>
    public override List<Page> FillPages()
    {
      string folder = Category._Folder + "pages" + Path.DirectorySeparatorChar;
      List<Page> pages = new List<Page>();

      foreach (string file in Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly))
      {
        FileInfo info = new FileInfo(file);
        string id = info.Name.Replace(".xml", string.Empty);
        Page page = Page.Load(new Guid(id));
        pages.Add(page);
      }

      return pages;
    }


  }
}
