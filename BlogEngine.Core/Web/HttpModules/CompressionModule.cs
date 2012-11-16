#region Using

using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

#endregion

namespace BlogEngine.Core.Web.HttpModules
{
	/// <summary>
	/// Compresses the output using standard gzip/deflate.
	/// </summary>
	public sealed class CompressionModule : IHttpModule
	{

		#region IHttpModule Members

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module 
		/// that implements <see cref="T:System.Web.IHttpModule"></see>.
		/// </summary>
		void IHttpModule.Dispose()
		{
			// Nothing to dispose; 
		}

		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpApplication"></see> 
		/// that provides access to the methods, properties, and events common to 
		/// all application objects within an ASP.NET application.
		/// </param>
		void IHttpModule.Init(HttpApplication context)
		{
			if (BlogSettings.Instance.EnableHttpCompression)
			{
				context.PreRequestHandlerExecute += new EventHandler(context_PostReleaseRequestState);
			}
		}

		#endregion

		private const string GZIP = "gzip";
		private const string DEFLATE = "deflate";

		#region Compress page

		/// <summary>
		/// Handles the BeginRequest event of the context control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void context_PostReleaseRequestState(object sender, EventArgs e)
		{
			HttpContext context = ((HttpApplication)sender).Context;
            if (context.CurrentHandler is System.Web.UI.Page && context.Request["HTTP_X_MICROSOFTAJAX"] == null && context.Request.HttpMethod == "GET")
			{
				CompressResponse(context);
			
				if (BlogSettings.Instance.CompressWebResource)
					context.Response.Filter = new WebResourceFilter(context.Response.Filter);
			}
			else if (!BlogSettings.Instance.CompressWebResource && context.Request.Path.Contains("WebResource.axd"))
			{
				context.Response.Cache.SetExpires(DateTime.Now.AddDays(30));
			}
		}

		/// <summary>
		/// Compresses the response stream using either deflate or gzip depending on the client.
		/// </summary>
		/// <param name="context">The HTTP context to compress.</param>
		public static void CompressResponse(HttpContext context)
		{
			if (IsEncodingAccepted(DEFLATE))
			{
				context.Response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress);
				SetEncoding(DEFLATE);
			}
			else if (IsEncodingAccepted(GZIP))
			{
				context.Response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress);
				SetEncoding(GZIP);
			}
		}

		/// <summary>
		/// Checks the request headers to see if the specified
		/// encoding is accepted by the client.
		/// </summary>
		private static bool IsEncodingAccepted(string encoding)
		{
			HttpContext context = HttpContext.Current;
			return context.Request.Headers["Accept-encoding"] != null && context.Request.Headers["Accept-encoding"].Contains(encoding);
		}

		/// <summary>
		/// Adds the specified encoding to the response headers.
		/// </summary>
		/// <param name="encoding"></param>
		private static void SetEncoding(string encoding)
		{
			HttpContext.Current.Response.AppendHeader("Content-encoding", encoding);
		}

		#endregion

		#region WebResourceFilter

		private class WebResourceFilter : Stream
		{

			public WebResourceFilter(Stream sink)
			{
				_sink = sink;
			}

			private Stream _sink;

			#region Properites

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return true; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override void Flush()
			{
				_sink.Flush();
			}

			public override long Length
			{
				get { return 0; }
			}

			private long _position;
			public override long Position
			{
				get { return _position; }
				set { _position = value; }
			}

			#endregion

			#region Methods

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _sink.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _sink.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_sink.SetLength(value);
			}

			public override void Close()
			{
				_sink.Close();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				string html = System.Text.Encoding.Default.GetString(buffer);

				Regex regex = new Regex("<script\\s*src=\"((?=[^\"]*webresource.axd)[^\"]*)\"\\s*type=\"text/javascript\"[^>]*>[^<]*(?:</script>)?", RegexOptions.IgnoreCase);
                html = regex.Replace(html, new MatchEvaluator(Evaluator));

				byte[] outdata = System.Text.Encoding.Default.GetBytes(html);
				_sink.Write(outdata, 0, outdata.GetLength(0));
			}

            /// <summary>
            /// Evaluates the replacement for each link match.
            /// </summary>
            public string Evaluator(Match match)
            {
                string relative = match.Groups[1].Value;
                string absolute = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                return match.Value.Replace(relative, Utils.RelativeWebRoot + "js.axd?path=" + HttpUtility.UrlEncode(absolute + relative));
            }
			#endregion

		}

		#endregion

	}
}