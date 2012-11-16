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
		/// Loads the stop words used in the search feature.
		/// </summary>
		/// <returns></returns>
		public override StringCollection LoadStopWords()
		{
			string fileName = _Folder + "stopwords.txt";
			if (!File.Exists(fileName))
				return new StringCollection();

			using (StreamReader reader = new StreamReader(fileName))
			{
				string file = reader.ReadToEnd();
				string[] words = file.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

				StringCollection col = new StringCollection();
				col.AddRange(words);

				return col;
			}

		}
	}
}