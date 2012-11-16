#region Using

using System;
using System.Web;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
	/// <summary>
	/// Receives and records all ratings comming in from the rating control.
	/// </summary>
	public class RatingHandler : IHttpHandler
	{

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
			string id = context.Request.QueryString["id"];
			string rating = context.Request.QueryString["rating"];
			int rate = 0;
			if (rating != null && int.TryParse(rating, out rate))
			{
				if (id != null && id.Length == 36 && rate > 0 && rate < 6)
				{
					bool hasRated = HasRated(id);

					if (hasRated)
					{
						context.Response.Write(rate + "HASRATED");
						context.Response.End();
					}

					Post post = Post.GetPost(new Guid(id));
					post.Rate(rate);

					SetCookie(id, context);
					context.Response.Write(rate + "OK");
					context.Response.End();
				}
			}

				context.Response.Write("FAIL");			
		}

		private static void SetCookie(string id, HttpContext context)
		{
			HttpCookie cookie;
			if (context.Request.Cookies["rating"] != null)
			{
				cookie = context.Request.Cookies["rating"];
			}
			else
			{
				cookie = new HttpCookie("rating");
			}

			cookie.Expires = DateTime.Now.AddYears(2);
			cookie.Value += id;
			context.Response.Cookies.Add(cookie);
		}

		private static bool HasRated(string postId)
		{
			if (HttpContext.Current.Request.Cookies["rating"] != null)
			{
				HttpCookie cookie = HttpContext.Current.Request.Cookies["rating"];
				return cookie.Value.Contains(postId);
			}

			return false;
		}

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
		/// </summary>
		/// <value></value>
		/// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
		public bool IsReusable
		{
			get { return false; }
		}

	}
}