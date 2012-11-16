#region Using

using System;
using System.IO;
using System.Web;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{

	/// <summary>
	/// The FileHandler serves all files that is uploaded from
	/// the admin pages except for images. 
	/// </summary>
	/// <remarks>
	/// By using a HttpHandler to serve files, it is very easy
	/// to add the capability to stop bandwidth leeching or
	/// to create a statistics analysis feature upon it.
	/// </remarks>
	public class FileHandler : IHttpHandler
	{

		#region IHttpHandler Members

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
		/// </summary>
		/// <value></value>
		/// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
		public bool IsReusable
		{
			get { return false; }
		}

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
			if (!string.IsNullOrEmpty(context.Request.QueryString["file"]))
			{
                string fileName = context.Request.QueryString["file"];
				OnServing(fileName);

				try
				{
					string folder = context.Server.MapPath(BlogSettings.Instance.StorageLocation + "/files");
					FileInfo info = new FileInfo(folder + Path.DirectorySeparatorChar + fileName);

					if (info.Exists && info.Directory.FullName.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
					{
						context.Response.AppendHeader("Content-Disposition", "inline; filename=\"" + info.Name + "\"");
						SetContentType(context, fileName);

						if (Utils.SetConditionalGetHeaders(info.CreationTimeUtc))
							return;

						context.Response.TransmitFile(info.FullName);
						OnServed(fileName);
					}
					else
					{
						OnBadRequest(fileName);
						context.Response.Redirect(Utils.AbsoluteWebRoot + "error404.aspx");
					}
				}
				catch (Exception)
				{
					OnBadRequest(fileName);
					context.Response.Redirect(Utils.AbsoluteWebRoot + "error404.aspx");
				}
			}
		}

		/// <summary>
		/// Sets the content type depending on the filename's extension.
		/// </summary>
		private static void SetContentType(HttpContext context, string fileName)
		{
			if (fileName.EndsWith(".pdf"))
			{
				context.Response.AddHeader("Content-Type", "application/pdf");
			}
			else
			{
				context.Response.AddHeader("Content-Type", "application/octet-stream");
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the requested file does not exist;
		/// </summary>
		public static event EventHandler<EventArgs> Serving;

		private static void OnServing(string file)
		{
			if (Serving != null)
			{
				Serving(file, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when a file is served;
		/// </summary>
		public static event EventHandler<EventArgs> Served;

		private static void OnServed(string file)
		{
			if (Served != null)
			{
				Served(file, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when the requested file does not exist;
		/// </summary>
		public static event EventHandler<EventArgs> BadRequest;

		private static void OnBadRequest(string file)
		{
			if (BadRequest != null)
			{
				BadRequest(file, EventArgs.Empty);
			}
		}

		#endregion

	}

}