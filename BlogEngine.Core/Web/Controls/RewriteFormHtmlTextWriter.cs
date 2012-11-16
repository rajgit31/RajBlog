#region Using

using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

#endregion

namespace BlogEngine.Core.Web.Controls
{

	/// <summary>
	/// The RewriteFormHtmlTextWriter class implements Form action tag rewriting for rewritten pages 
	/// on Mono.
	/// </summary>
	public class RewriteFormHtmlTextWriter : HtmlTextWriter
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="RewriteFormHtmlTextWriter"/> class.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public RewriteFormHtmlTextWriter(Html32TextWriter writer)
			: base(writer)
		{
			this.InnerWriter = writer.InnerWriter;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RewriteFormHtmlTextWriter"/> class.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public RewriteFormHtmlTextWriter(System.IO.TextWriter writer)
			: base(writer)
		{
			this.InnerWriter = writer;
		}

		/// <summary>
		/// Writes the specified markup attribute and value to the output stream, and, if specified, writes the value encoded.
		/// </summary>
		/// <param name="name">The markup attribute to write to the output stream.</param>
		/// <param name="value">The value assigned to the attribute.</param>
		/// <param name="fEncode">true to encode the attribute and its assigned value; otherwise, false.</param>
		public override void WriteAttribute(string name, string value, bool fEncode)
		{
			// Mono has issues identifying relative paths when the url is rewritten,
			// so we need to place the full path in the form tag's action attribute
			// or postbacks won't work in rewritten pages.
			if (BlogEngine.Core.Utils.IsMono)
			{
				if (name == "action")
				{
					if (HttpContext.Current.Items["ActionAlreadyWritten"] == null)
					{
						value = BlogEngine.Core.Utils.AbsoluteWebRoot + value;
						HttpContext.Current.Items["ActionAlreadyWritten"] = true;
					}
				}
			}
			else
			{
				if (name == "action")
				{
					if (HttpContext.Current.Items["ActionAlreadyWritten"] == null)
					{
						value = HttpContext.Current.Request.RawUrl;
						HttpContext.Current.Items["ActionAlreadyWritten"] = true;
					}
				}
			}
			base.WriteAttribute(name, value, fEncode);
		}

	}
}