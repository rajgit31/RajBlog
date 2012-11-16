#region Using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Providers
{
    public partial class XmlBlogProvider : BlogProvider
    {

        #region Referrer

        /// <summary>
        /// Gets a Referrer based on the Id.
        /// </summary>
        /// <param name="Id">The Referrer's Id.</param>
        /// <returns>A matching Referrer.</returns>
        public override Referrer SelectReferrer(Guid Id)
        {
            Referrer refer = Referrer.Referrers.Find(r => r.Id.Equals(Id));
            if (refer == null)
            {
                refer = new Referrer();
            }
            refer.MarkOld();
            return refer;
        }

        /// <summary>
        /// Inserts a Referrer.
        /// </summary>
        /// <param name="referrer">Must be a valid Referrer object.</param>
        public override void InsertReferrer(Referrer referrer)
        {
            Referrer.Referrers.Add(referrer);

            referrer.MarkOld();
            List<Referrer> day = Referrer.Referrers.FindAll(r => r.Day.ToShortDateString() == referrer.Day.ToShortDateString());
            writeReferrerFile(day, referrer.Day);
        }

        /// <summary>
        /// Updates a Referrer.
        /// </summary>
        /// <param name="referrer">Must be a valid Referrer object.</param>
        public override void UpdateReferrer(Referrer referrer)
        {
            List<Referrer> day = Referrer.Referrers.FindAll(r => r.Day.ToShortDateString() == referrer.Day.ToShortDateString());
            writeReferrerFile(day, referrer.Day);
        }

        /// <summary>
        /// Fills an unsorted list of Referrers.
        /// </summary>
        /// <returns>A List&lt;Referrer&gt; of all Referrers.</returns>
        public override List<Referrer> FillReferrers()
        {
            string folder = Path.Combine(_Folder, "log");

            List<Referrer> referrers = new List<Referrer>();
            DateTime oldFileDate = DateTime.Today.AddDays(-BlogSettings.Instance.NumberOfReferrerDays);

            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            if (dirInfo.Exists)
            {
                List<FileInfo> logFiles = new List<FileInfo>(dirInfo.GetFiles());
                foreach (FileInfo file in logFiles)
                {
                    string fileName = file.Name.Replace(".xml", string.Empty);
                    string[] dateStrings = fileName.Split(new char[] { '.' });
                    if (dateStrings.Length != 3)
                    {
                        file.Delete();
                        continue;
                    }

                    DateTime day = new DateTime(int.Parse(dateStrings[0]), int.Parse(dateStrings[1]), int.Parse(dateStrings[2]));
                    if (day < oldFileDate)
                    {
                        file.Delete();
                        continue;
                    }

                    referrers.AddRange(getReferrersFromFile(file, day));
                }
            }
            return referrers;
        }

        private List<Referrer> getReferrersFromFile(FileInfo file, DateTime day)
        {
            List<Referrer> referrers = new List<Referrer>();

            XmlDocument doc = new XmlDocument();
            doc.Load(file.FullName);

            XmlNodeList nodes = doc.SelectNodes("referrers/referrer");
            foreach (XmlNode node in nodes)
            {
                Referrer refer = new Referrer()
                {
                    Url = node.Attributes["url"] == null ? null : new Uri(node.Attributes["url"].InnerText),
                    Count = node.Attributes["count"] == null ? 0 : int.Parse(node.Attributes["count"].InnerText),
                    Day = day,
                    PossibleSpam = node.Attributes["isSpam"] == null ? false : bool.Parse(node.Attributes["isSpam"].InnerText),
                    ReferrerUrl = new Uri(node.InnerText),
                    Id = Guid.NewGuid()
                };

                refer.MarkOld();
                referrers.Add(refer);
            }

            return referrers;
        }

        private void writeReferrerFile(List<Referrer> referrers, DateTime day)
        {
            string folder = Path.Combine(_Folder, "log");
            string fileName = Path.Combine(folder, day.ToString("yyyy.MM.dd") + ".xml");
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            using (XmlTextWriter writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;
                writer.WriteStartDocument(true);
                writer.WriteStartElement("referrers");

                foreach (Referrer refer in referrers)
                {
                    writer.WriteStartElement("referrer");
                    writer.WriteAttributeString("url", refer.Url.ToString());
                    writer.WriteAttributeString("count", refer.Count.ToString());
                    writer.WriteAttributeString("isSpam", refer.PossibleSpam.ToString());
                    writer.WriteString(refer.ReferrerUrl.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        #endregion

    }
}
