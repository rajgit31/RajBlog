#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
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

        #region BlogRoll

        /// <summary>
        /// Gets a BlogRoll based on a Guid.
        /// </summary>
        /// <param name="id">The BlogRoll's Guid.</param>
        /// <returns>A matching BlogRoll</returns>
        public override BlogRollItem SelectBlogRollItem(Guid id)
        {
            BlogRollItem blogRoll = BlogRollItem.BlogRolls.Find(br => br.Id == id);
            if (blogRoll == null)
            {
                blogRoll = new BlogRollItem();
            }
            blogRoll.MarkOld();
            return blogRoll;
        }

        /// <summary>
        /// Inserts a BlogRoll
        /// </summary>
        /// <param name="blogRoll">Must be a valid BlogRoll object.</param>
        public override void InsertBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRolls = BlogRollItem.BlogRolls;
            blogRolls.Add(blogRollItem);

            writeBlogRollFile(blogRolls);
        }

        private void writeBlogRollFile(List<BlogRollItem> blogRollItems)
        {
            string fileName = _Folder + "blogroll.xml";

            using (XmlTextWriter writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;
                writer.WriteStartDocument(true);
                writer.WriteStartElement("blogRoll");

                foreach (BlogRollItem br in blogRollItems)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("id", br.Id.ToString());
                    writer.WriteAttributeString("title", br.Title);
                    writer.WriteAttributeString("description", br.Description != null ? br.Description : string.Empty);
                    writer.WriteAttributeString("htmlUrl", br.BlogUrl != null ? br.BlogUrl.ToString() : string.Empty);
                    writer.WriteAttributeString("xmlUrl", br.FeedUrl != null ? br.FeedUrl.ToString() : string.Empty);
                    writer.WriteAttributeString("xfn", br.Xfn != null ? br.Xfn : string.Empty);
                    writer.WriteAttributeString("sortIndex", br.SortIndex.ToString());
                    writer.WriteEndElement();
                    br.MarkOld();
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Updates a BlogRoll
        /// </summary>
        /// <param name="blogRoll">Must be a valid BlogRoll object.</param>
        public override void UpdateBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRolls = BlogRollItem.BlogRolls;
            blogRolls.Remove(blogRollItem);
            blogRolls.Add(blogRollItem);
            writeBlogRollFile(blogRolls);
        }

        /// <summary>
        /// Deletes a BlogRoll
        /// </summary>
        /// <param name="blogRoll">Must be a valid BlogRoll object.</param>
        public override void DeleteBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRoll = BlogRollItem.BlogRolls;
            blogRoll.Remove(blogRollItem);
            writeBlogRollFile(blogRoll);
        }

        /// <summary>
        /// Fills an unsorted list of BlogRolls.
        /// </summary>
        /// <returns>A List&lt;BlogRoll&gt; of all BlogRolls</returns>
        public override List<BlogRollItem> FillBlogRoll()
        {
            string fileName = _Folder + "blogroll.xml";
            if (!File.Exists(fileName))
                return null;

            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            List<BlogRollItem> blogRoll = new List<BlogRollItem>();

            int largestSortIndex = -1;
            bool isLegacyFormat = false;
            XmlNodeList nodes = doc.SelectNodes("blogRoll/item");
            if (nodes.Count == 0)
            {
                // legacy file format.
                nodes = doc.SelectNodes("opml/body/outline");
                isLegacyFormat = true;
            }
            foreach (XmlNode node in nodes)
            {
                BlogRollItem br = new BlogRollItem()
                {
                    Id = node.Attributes["id"] == null ? Guid.NewGuid() : new Guid(node.Attributes["id"].InnerText),
                    Title = node.Attributes["title"] == null ? null : node.Attributes["title"].InnerText,
                    Description = node.Attributes["description"] == null ? null : node.Attributes["description"].InnerText,
                    BlogUrl = node.Attributes["htmlUrl"] == null ? null : new Uri(node.Attributes["htmlUrl"].InnerText),
                    FeedUrl = node.Attributes["xmlUrl"] == null ? null : new Uri(node.Attributes["xmlUrl"].InnerText),
                    Xfn = node.Attributes["xfn"] == null ? null : node.Attributes["xfn"].InnerText,
                    SortIndex = node.Attributes["sortIndex"] == null ? (blogRoll.Count == 0 ? 0 : largestSortIndex + 1) : int.Parse(node.Attributes["sortIndex"].InnerText)
                };

                if (br.SortIndex > largestSortIndex)
                    largestSortIndex = br.SortIndex;

                blogRoll.Add(br);
                br.MarkOld();
            }

            if (isLegacyFormat && blogRoll.Count > 0)
            {
                // if we're upgrading from a legacy format, re-write the file to conform to the new format.
                writeBlogRollFile(blogRoll);
            }

            return blogRoll;
        }

        #endregion

    }
}
