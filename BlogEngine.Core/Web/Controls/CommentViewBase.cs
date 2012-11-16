#region Using

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using BlogEngine.Core;
using System.Web.Security;

#endregion

namespace BlogEngine.Core.Web.Controls
{

	/// <summary>
	/// Inherit from this class when you are building the
	/// commentview.ascx user control in your custom theme.
	/// </summary>
	/// <remarks>
	/// The class exposes a lot of functionality to the custom
	/// comment control in the theme folder.
	/// </remarks>
	public class CommentViewBase : UserControl
	{

		#region Properties

		private Post _Post;

		/// <summary>
		/// Gets or sets the Post from which the comment belongs.
		/// </summary>
		/// <value>The Post object.</value>
		public Post Post
		{
			get { return _Post; }
			set { _Post = value; }
		}

		private Comment _Comment;

		/// <summary>
		/// Gets or sets the Comment.
		/// </summary>
		/// <value>The comment.</value>
		public Comment Comment
		{
			get { return _Comment; }
			set { _Comment = value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// The regular expression used to parse links.
		/// </summary>
		private static readonly Regex regex = new Regex("((http://|www\\.)([A-Z0-9.-]{1,})\\.[0-9A-Z?;~&#=\\-_\\./]{2,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private const string link = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";

		/// <summary>
		/// Examins the comment body for any links and turns them
		/// automatically into one that can be clicked.
		/// </summary>
		[Obsolete("Use the Text property instead. This method will be removed in a future version.")]
		protected string ResolveLinks(string body)
		{
			return Text;
		}

		/// <summary>
		/// Gets the text of the comment.
		/// </summary>
		/// <value>The text.</value>
		public string Text
		{
			get
			{
				ServingEventArgs arg = new ServingEventArgs(Comment.Content, ServingLocation.SinglePost);
				Comment.OnServing(Comment, arg);
				if (arg.Cancel)
				{
					this.Visible = false;
				}

				string body = arg.Body.Replace("\n", "<br />");
				body = body.Replace("\t", "&nbsp;&nbsp;");
				body = body.Replace("  ", "&nbsp;&nbsp;");
				return body;
			}
		}

		/// <summary>
		/// Displays a link that lets a user reply to a specific comment
		/// </summary>
		protected string ReplyToLink
		{
			get
			{
				if (!BlogSettings.Instance.IsCommentsEnabled ||
					!BlogSettings.Instance.IsCommentNestingEnabled ||
					!Post.IsCommentsEnabled ||
					!Comment.IsApproved ||
					(BlogSettings.Instance.DaysCommentsAreEnabled > 0 && Post.DateCreated.AddDays(BlogSettings.Instance.DaysCommentsAreEnabled) < DateTime.Now.Date))
				{
					return "";
				}
				else
				{
					return "<a href=\"javascript:void(0);\" class=\"reply-to-comment\" onclick=\"BlogEngine.replyToComment('" + Comment.Id.ToString() + "');\">" + Utils.Translate("replyToThis") + "</a>";
				}
			}
		}


		/// <summary>
		/// Displays a delete link to visitors that are authenticated
		/// using the default membership provider.
		/// </summary>
		protected string AdminLinks
		{
			get
			{
				if (Page.User.IsInRole(BlogSettings.Instance.AdministratorRole) || Page.User.Identity.Name.Equals(Post.Author))
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					sb.AppendFormat(" | <a class=\"email\" href=\"mailto:{0}\">{0}</a>", Comment.Email);
					sb.AppendFormat(" | <a href=\"http://www.domaintools.com/go/?service=whois&amp;q={0}/\">{0}</a>", Comment.IP);

					if (Comment.Comments.Count > 0)
					{
						string confirmDelete = string.Format(CultureInfo.InvariantCulture, Utils.Translate("areYouSure"), Utils.Translate("delete").ToLowerInvariant(), Utils.Translate("theComment"));
						sb.AppendFormat(" | <a href=\"javascript:void(0);\" onclick=\"if (confirm('{1}?')) location.href='?deletecomment={0}'\">{2}</a>", Comment.Id, confirmDelete, Utils.Translate("deleteKeepReplies"));

                        string confirmRepliesDelete = string.Format(CultureInfo.InvariantCulture, Utils.Translate("areYouSure"), Utils.Translate("delete").ToLowerInvariant(), Utils.Translate("theComment"));
						sb.AppendFormat(" | <a href=\"javascript:void(0);\" onclick=\"if (confirm('{1}?')) location.href='?deletecommentandchildren={0}'\">{2}</a>", Comment.Id, confirmRepliesDelete, Utils.Translate("deletePlusReplies"));
					}
					else
					{
                        string confirmDelete = string.Format(CultureInfo.InvariantCulture, Utils.Translate("areYouSure"), Utils.Translate("delete").ToLowerInvariant(), Utils.Translate("theComment"));
						sb.AppendFormat(" | <a href=\"javascript:void(0);\" onclick=\"if (confirm('{1}?')) location.href='?deletecomment={0}'\">{2}</a>", Comment.Id, confirmDelete, Utils.Translate("delete"));
					}

					if (!Comment.IsApproved)
					{
                        sb.AppendFormat(" | <a href=\"?approvecomment={0}\">{1}</a>", Comment.Id, Utils.Translate("approve"));

					}
					return sb.ToString();
				}
				return string.Empty;
			}
		}

		private const string FLAG_IMAGE = "<span class=\"adr\"><img src=\"{0}pics/flags/{1}.png\" class=\"country-name flag\" title=\"{2}\" alt=\"{2}\" /></span>";

		/// <summary>
		/// Displays the flag of the country from which the comment was written.
		/// <remarks>
		/// If the country hasn't been resolved from the authors IP address or
		/// the flag does not exist for that country, nothing is displayed.
		/// </remarks>
		/// </summary>
		protected string Flag
		{
			get
			{
				if (!string.IsNullOrEmpty(Comment.Country))
				{
					//return "<img src=\"" + Utils.RelativeWebRoot + "pics/flags/" + Comment.Country + ".png\" class=\"country-name flag\" title=\"" + Comment.Country + "\" alt=\"" + Comment.Country + "\" />";
					return string.Format(FLAG_IMAGE, Utils.RelativeWebRoot, Comment.Country, FindCountry(Comment.Country));
				}

				return null;
			}
		}

		string FindCountry(string isoCode)
		{
			foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
			{
				RegionInfo ri = new RegionInfo(ci.Name);
				if (ri.TwoLetterISORegionName.Equals(isoCode, StringComparison.OrdinalIgnoreCase))
				{
					return ri.DisplayName;
				}
			}

			return isoCode;
		}

		private const string GRAVATAR_IMAGE = "<img class=\"photo\" src=\"{0}\" alt=\"{1}\" />";

		/// <summary>
		/// Displays the Gravatar image that matches the specified email.
		/// </summary>
		protected string Gravatar(int size)
		{
			if (BlogSettings.Instance.Avatar == "none")
				return null;

            if (!string.IsNullOrEmpty(Comment.Avatar))
            {
                return string.Format(CultureInfo.InvariantCulture, GRAVATAR_IMAGE, Comment.Avatar, Comment.Author);
            }

			if (String.IsNullOrEmpty(Comment.Email) || !Comment.Email.Contains("@"))
			{
				if (Comment.Website != null && Comment.Website.ToString().Length > 0 && Comment.Website.ToString().Contains("http://"))
				{
					return string.Format(CultureInfo.InvariantCulture, "<img class=\"thumb\" src=\"http://images.websnapr.com/?url={0}&amp;size=t\" alt=\"{1}\" />", Server.UrlEncode(Comment.Website.ToString()), Comment.Email);
				}

				return "<img src=\"" + Utils.AbsoluteWebRoot + "themes/" + BlogSettings.Instance.Theme + "/noavatar.jpg\" alt=\"" + Comment.Author + "\" />";
			}

			string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(Comment.Email.ToLowerInvariant().Trim(), "MD5").ToLowerInvariant();
			string gravatar = "http://www.gravatar.com/avatar/" + hash + ".jpg?s=" + size + "&amp;d=";

			string link = string.Empty;
			switch (BlogSettings.Instance.Avatar)
			{
				case "identicon":
					link = gravatar + "identicon";
					break;

				case "wavatar":
					link = gravatar + "wavatar";
					break;

				default:
					link = gravatar + "monsterid";
					break;
			}

			return string.Format(CultureInfo.InvariantCulture, GRAVATAR_IMAGE, link, Comment.Author);
		}

		#endregion

	}
}
