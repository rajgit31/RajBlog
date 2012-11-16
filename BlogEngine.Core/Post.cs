#region Using

using System;
using System.Web;
using System.IO;
using System.Xml;
using System.Text;
using System.Net.Mail;
using System.Globalization;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using BlogEngine.Core.Providers;
using System.Threading;

#endregion

namespace BlogEngine.Core
{
	/// <summary>
	/// A post is an entry on the blog - a blog post.
	/// </summary>
	[Serializable]
	public class Post : BusinessBase<Post, Guid>, IComparable<Post>, IPublishable
	{

		#region Constructor

		/// <summary>
		/// The default contstructor assign default values.
		/// </summary>
		public Post()
		{
			base.Id = Guid.NewGuid();
			_Comments = new List<Comment>();
			_Categories = new StateList<Category>();
			_Tags = new StateList<string>();
            _NotificationEmails = new StateList<string>();
			DateCreated = DateTime.Now;
			_IsPublished = true;
			_IsCommentsEnabled = true;
		}

		#endregion

		#region Properties

		private string _Author;
		/// <summary>
		/// Gets or sets the Author or the post.
		/// </summary>
		public string Author
		{
			get { return _Author; }
			set
			{
				if (_Author != value) MarkChanged("Author");
				_Author = value;
			}
		}

		public AuthorProfile AuthorProfile
		{
			get { return AuthorProfile.GetProfile(Author); }
		}


		private string _Title;
		/// <summary>
		/// Gets or sets the Title or the post.
		/// </summary>
		public string Title
		{
			get { return _Title; }
			set
			{
				if (_Title != value) MarkChanged("Title");
				_Title = value;
			}
		}

		private string _Description;
		/// <summary>
		/// Gets or sets the Description or the post.
		/// </summary>
		public string Description
		{
			get { return _Description; }
			set
			{
				if (_Description != value) MarkChanged("Description");
				_Description = value;
			}
		}

		private string _Content;
		/// <summary>
		/// Gets or sets the Content or the post.
		/// </summary>
		public string Content
		{
			get { return _Content; }
			set
			{
				if (_Content != value)
				{
					MarkChanged("Content");
					HttpContext.Current.Cache.Remove("content_" + this.Id);
					_Content = value;
				}
			}
		}

		private readonly List<Comment> _Comments;
		/// <summary>
		/// A collection of Approved comments for the post sorted by date.
		/// </summary>
		public List<Comment> ApprovedComments
		{
			get { return _Comments.FindAll(c => c.IsApproved); }
		}

		/// <summary>
		/// A collection of comments waiting for approval for the post, sorted by date.
		/// </summary>
		public List<Comment> NotApprovedComments
		{
			get { return _Comments.FindAll(c => !c.IsApproved); }
		}

		/// <summary>
		/// A Collection of All Comments for the post
		/// </summary>
		public List<Comment> Comments
		{
			get { return _Comments; }

		}

		private List<Comment> _NestedComments;

		/// <summary>
		/// A collection of the comments that are nested as replies
		/// </summary>
		public List<Comment> NestedComments
		{
			get
			{
				if (_NestedComments == null)
					CreateNestedComments();
				return _NestedComments;
			}
		}

		/// <summary>
		/// Clears all nesting of comments
		/// </summary>
		private void ResetNestedComments()
		{
			// void the List<>
			_NestedComments = null;
			// go through all comments and remove sub comments
			foreach (Comment c in Comments)
				c.Comments.Clear();
		}

		/// <summary>
		/// Nests comments based on Id and ParentId
		/// </summary>
		private void CreateNestedComments()
		{
			// instantiate object
			_NestedComments = new List<Comment>();
			
			// temporary ID/Comment table
			Hashtable commentTable = new Hashtable();
		
			foreach (Comment comment in _Comments)
			{
				// add to hashtable for lookup
				commentTable.Add(comment.Id, comment);

				// check if this is a child comment
				if (comment.ParentId == Guid.Empty)
				{
					// root comment, so add it to the list
					_NestedComments.Add(comment);
				}
				else
				{
					// child comment, so find parent
					Comment parentComment = commentTable[comment.ParentId] as Comment;
					if (parentComment != null)
					{
						// double check that this sub comment has not already been added
						if (parentComment.Comments.IndexOf(comment) == -1)
							parentComment.Comments.Add(comment);	
					}
					else
					{
						// just add to the base to prevent an error
						_NestedComments.Add(comment);
					}
				}
			}
			// kill this data
			commentTable = null;
		}

		private StateList<Category> _Categories;
		/// <summary>
		/// An unsorted List of categories.
		/// </summary>
		public StateList<Category> Categories
		{
			get { return _Categories; }
		}

		private StateList<string> _Tags;
		/// <summary>
		/// An unsorted collection of tags.
		/// </summary>
		public StateList<string> Tags
		{
			get { return _Tags; }
		}

		private bool _IsCommentsEnabled;
		/// <summary>
		/// Gets or sets the EnableComments or the object.
		/// </summary>
		public bool IsCommentsEnabled
		{
			get { return _IsCommentsEnabled; }
			set
			{
				if (_IsCommentsEnabled != value) MarkChanged("IsCommentsEnabled");
				_IsCommentsEnabled = value;
			}
		}

		private bool _IsPublished;
		/// <summary>
		/// Gets or sets whether or not the post is published.
		/// </summary>
		public bool IsPublished
		{
			get { return _IsPublished; }
			set
			{
				if (_IsPublished != value) MarkChanged("IsPublished");
				_IsPublished = value;
			}
		}

		private float _Rating;
		/// <summary>
		/// Gets or sets the rating or the post.
		/// </summary>
		public float Rating
		{
			get { return _Rating; }
			set
			{
				if (_Rating != value) MarkChanged("Rating");
				_Rating = value;
			}
		}

		private int _Raters;
		/// <summary>
		/// Gets or sets the number of raters or the object.
		/// </summary>
		public int Raters
		{
			get { return _Raters; }
			set
			{
				if (_Raters != value) MarkChanged("Raters");
				_Raters = value;
			}
		}

		private string _Slug;
		/// <summary>
		/// Gets or sets the Slug of the Post.
		/// A Slug is the relative URL used by the posts.
		/// </summary>
		public string Slug
		{
			get
			{
				if (string.IsNullOrEmpty(_Slug))
					return Utils.RemoveIllegalCharacters(Title);

				return _Slug;
			}
			set
			{
				if (_Slug != value) MarkChanged("Slug");
				_Slug = value;
			}
		}

        private StateList<string> _NotificationEmails;
        /// <summary>
        /// Gets a collection of email addresses that is signed up for 
        /// comment notification on the specific post.
        /// </summary>
        public StateList<string> NotificationEmails
        {
            get { return _NotificationEmails; }
        }

		/// <summary>
		/// Gets whether or not the post is visible or not.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				if (IsAuthenticated || (IsPublished && DateCreated <= DateTime.Now.AddHours(BlogSettings.Instance.Timezone)))
					return true;

				return false;
			}
		}

        /// <summary>
        /// Gets whether a post is available to visitors not logged into the blog.
        /// </summary>
        public bool IsVisibleToPublic
        {
            get
            {
                return IsPublished && DateCreated <= DateTime.Now.AddHours(BlogSettings.Instance.Timezone);
            }
        }

		private Post _Prev;
		/// <summary>
		/// Gets the previous post relative to this one based on time.
		/// <remarks>
		/// If this post is the oldest, then it returns null.
		/// </remarks>
		/// </summary>
		public Post Previous
		{
			get { return _Prev; }
		}

		private Post _Next;
		/// <summary>
		/// Gets the next post relative to this one based on time.
		/// <remarks>
		/// If this post is the newest, then it returns null.
		/// </remarks>
		/// </summary>
		public Post Next
		{
			get { return _Next; }
		}

		#endregion

		#region Links

		/// <summary>
		/// The absolute permanent link to the post.
		/// </summary>
		public Uri PermaLink
		{
			get { return new Uri(Utils.AbsoluteWebRoot.ToString() + "post.aspx?id=" + Id.ToString()); }
		}

		/// <summary>
		/// A relative-to-the-site-root path to the post.
		/// Only for in-site use.
		/// </summary>
		public string RelativeLink
		{
			get
			{
				string slug = Utils.RemoveIllegalCharacters(Slug) + BlogSettings.Instance.FileExtension;

				if (BlogSettings.Instance.TimeStampPostLinks)
					return Utils.RelativeWebRoot + "post/" + DateCreated.ToString("yyyy/MM/dd/", CultureInfo.InvariantCulture) + slug;

				return Utils.RelativeWebRoot + "post/" + slug;
			}
		}

		/// <summary>
		/// The absolute link to the post.
		/// </summary>
		public Uri AbsoluteLink
		{
			get { return Utils.ConvertToAbsolute(RelativeLink); }
		}

		/// <summary>
		/// The trackback link to the post.
		/// </summary>
		public Uri TrackbackLink
		{
			get { return new Uri(Utils.AbsoluteWebRoot.ToString() + "trackback.axd?id=" + Id.ToString()); }
		}

		#endregion

		#region Methods

		private static object _SyncRoot = new object();
		private static List<Post> _Posts;
		/// <summary>
		/// A sorted collection of all posts in the blog.
		/// Sorted by date.
		/// </summary>
		public static List<Post> Posts
		{
			get
			{
				if (_Posts == null)
				{
					lock (_SyncRoot)
					{
						if (_Posts == null)
						{
							_Posts = BlogService.FillPosts();
							_Posts.TrimExcess();
							AddRelations();
						}
					}
				}

				return _Posts;
			}
		}

		///// <summary>
		///// Lazy loads the content of the post into cache to reduce memory footprint.
		///// </summary>
		///// <returns>The content of the post.</returns>
		//private string LoadPostContent()
		//{
		//  string key = string.Format("content_{0}", this.Id);

		//  if (HttpContext.Current.Cache[key] == null)
		//  {
		//    string content = BlogService.SelectPostContent(this.Id);
		//    HttpContext.Current.Cache.Insert(key, content, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 3, 0));
		//  }

		//  return (string)HttpContext.Current.Cache[key];
		//}

		/// <summary>
		/// Sets the Previous and Next properties to all posts.
		/// </summary>
		private static void AddRelations()
		{
			for (int i = 0; i < _Posts.Count; i++)
			{
				_Posts[i]._Next = null;
				_Posts[i]._Prev = null;
				if (i > 0)
					_Posts[i]._Next = _Posts[i - 1];

				if (i < _Posts.Count - 1)
					_Posts[i]._Prev = _Posts[i + 1];
			}
		}

		/// <summary>
		/// Returns all posts in the specified category
		/// </summary>
		public static List<Post> GetPostsByCategory(Guid categoryId)
		{
			Category cat = Category.GetCategory(categoryId);
			List<Post> col = Posts.FindAll(p => p.Categories.Contains(cat));
			col.Sort();
			col.TrimExcess();
			return col;
		}

		/// <summary>
		/// Returs a post based on the specified id.
		/// </summary>
		public static Post GetPost(Guid id)
		{
			return Posts.Find(p => p.Id == id);
		}

		/// <summary>
		/// Checks to see if the specified title has already been used
		/// by another post.
		/// <remarks>
		/// Titles must be unique because the title is part of the URL.
		/// </remarks>
		/// </summary>
		public static bool IsTitleUnique(string title)
		{
			string legal = Utils.RemoveIllegalCharacters(title);
			foreach (Post post in Posts)
			{
				if (Utils.RemoveIllegalCharacters(post.Title).Equals(legal, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			return true;
		}

		///// <summary>
		///// Returns a post based on it's title.
		///// </summary>
		//public static Post GetPostBySlug(string slug, DateTime date)
		//{
		//  return Posts.Find(delegate(Post p)
		//  {
		//    if (date != DateTime.MinValue && (p.DateCreated.Year != date.Year || p.DateCreated.Month != date.Month))
		//    {
		//      if (p.DateCreated.Day != 1 && p.DateCreated.Day != date.Day)
		//        return false;
		//    }

		//    return slug.Equals(Utils.RemoveIllegalCharacters(p.Slug), StringComparison.OrdinalIgnoreCase);
		//  });
		//}

		/// <summary>
		/// Returns all posts written by the specified author.
		/// </summary>
		public static List<Post> GetPostsByAuthor(string author)
		{
			string legalAuthor = Utils.RemoveIllegalCharacters(author);
			List<Post> list = Posts.FindAll(delegate(Post p)
			{
				string legalTitle = Utils.RemoveIllegalCharacters(p.Author);
				return legalAuthor.Equals(legalTitle, StringComparison.OrdinalIgnoreCase);
			});

			list.TrimExcess();
			return list;
		}

		/// <summary>
		/// Returns all posts tagged with the specified tag.
		/// </summary>
		public static List<Post> GetPostsByTag(string tag)
		{
			tag = Utils.RemoveIllegalCharacters(tag);
			List<Post> list = Posts.FindAll(delegate(Post p)
			{
				foreach (string t in p.Tags)
				{
                    if (Utils.RemoveIllegalCharacters(t).Equals(tag, StringComparison.OrdinalIgnoreCase))
						return true;
				}

				return false;
			});

			list.TrimExcess();
			return list;
		}

		/// <summary>
		/// Returns all posts published between the two dates.
		/// </summary>
		public static List<Post> GetPostsByDate(DateTime dateFrom, DateTime dateTo)
		{
			List<Post> list = Posts.FindAll(p => p.DateCreated.Date >= dateFrom && p.DateCreated.Date <= dateTo);
			list.TrimExcess();
			return list;
		}

		/// <summary>
		/// Adds a rating to the post.
		/// </summary>
		public void Rate(int rating)
		{
			if (Raters > 0)
			{
				float total = Raters * Rating;
				total += rating;
				Raters++;
				Rating = (float)(total / Raters);
			}
			else
			{
				Raters = 1;
				Rating = rating;
			}

			DataUpdate();
			OnRated(this);
		}

		/// <summary>
		/// Imports Post (without all standard saving routines
		/// </summary>
		public void Import()
		{
			if (this.IsDeleted)
			{
				if (!this.IsNew)
				{
					BlogService.DeletePost(this);
				}
			}
			else
			{
				if (this.IsNew)
				{
					BlogService.InsertPost(this);
				}
				else
				{
					BlogService.UpdatePost(this);
				}
			}
		}

		/// <summary>
		/// Force reload of all posts
		/// </summary>
		public static void Reload()
		{
			_Posts = BlogService.FillPosts();
			_Posts.Sort();
			AddRelations();
		}

		/// <summary>
		/// Adds a comment to the collection and saves the post.
		/// </summary>
		/// <param name="comment">The comment to add to the post.</param>
		public void AddComment(Comment comment)
		{
			CancelEventArgs e = new CancelEventArgs();
			OnAddingComment(comment, e);
			if (!e.Cancel)
			{
				Comments.Add(comment);
				DataUpdate();
				OnCommentAdded(comment);

                if (comment.IsApproved)
                    SendNotifications(comment);
			}
		}

        /// <summary>
        /// Updates a comment in the collection and saves the post.
        /// </summary>
        /// <param name="comment">The comment to update in the post.</param>
        public void UpdateComment(Comment comment)
        {
            CancelEventArgs e = new CancelEventArgs();
            OnUpdatingComment(comment, e);
            if (!e.Cancel)
            {
                int inx = Comments.IndexOf(comment);

                Comments[inx].IsApproved = comment.IsApproved;
                Comments[inx].Content = comment.Content;
                Comments[inx].Author = comment.Author;
                Comments[inx].Country = comment.Country;
                Comments[inx].Email = comment.Email;
                Comments[inx].IP = comment.IP;
                Comments[inx].Website = comment.Website;
                Comments[inx].ModeratedBy = comment.ModeratedBy;
                
                DateModified = DateTime.Now;
                DataUpdate();

                DataUpdate();
                OnCommentUpdated(comment);
            }
        }

		/// <summary>
		/// Imports a comment to comment collection and saves.  Does not
		/// notify user or run extension events.
		/// </summary>
		/// <param name="comment">The comment to add to the post.</param>
		public void ImportComment(Comment comment)
		{
			Comments.Add(comment);
			DataUpdate();

		}

		/// <summary>
		/// Sends a notification to all visitors  that has registered
		/// to retrieve notifications for the specific post.
		/// </summary>
		private void SendNotifications(Comment comment)
		{
			if (NotificationEmails.Count == 0 || comment.IsApproved == false)
				return;

			foreach (string email in NotificationEmails)
			{
				if (email != comment.Email)
				{
                    // Intentionally using AbsoluteLink instead of PermaLink so the "unsubscribe-email" QS parameter
                    // isn't dropped when post.aspx.cs does a 301 redirect to the RelativeLink, before the unsubscription
                    // process takes place.
                    string unsubscribeLink = AbsoluteLink.ToString();  
                    unsubscribeLink += (unsubscribeLink.Contains("?") ? "&" : "?") + "unsubscribe-email=" + HttpUtility.UrlEncode(email);

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(BlogSettings.Instance.Email, BlogSettings.Instance.Name);
                    mail.Subject = "New comment on " + Title;
                    mail.Body = "Comment by " + comment.Author + "<br /><br />";
                    mail.Body += comment.Content.Replace(Environment.NewLine, "<br />") + "<br /><br />";
                    mail.Body += string.Format("<a href=\"{0}\">{1}</a>", PermaLink + "#id_" + comment.Id, Title);
                    mail.Body += "<br /><br /><hr />";
                    mail.Body += string.Format("<a href=\"{0}\">{1}</a>", unsubscribeLink, Utils.Translate("commentNotificationUnsubscribe"));

					mail.To.Add(email);
					Utils.SendMailMessageAsync(mail);
				}
			}
		}

		/// <summary>
		/// Removes a comment from the collection and saves the post.
		/// </summary>
		/// <param name="comment">The comment to remove from the post.</param>
		public void RemoveComment(Comment comment)
		{
			CancelEventArgs e = new CancelEventArgs();
			OnRemovingComment(comment, e);
			if (!e.Cancel)
			{
				Comments.Remove(comment);
				DataUpdate();
				OnCommentRemoved(comment);
				comment = null;
			}
		}

		/// <summary>
		/// Approves a Comment for publication.
		/// </summary>
		/// <param name="comment">The Comment to approve</param>
		public void ApproveComment(Comment comment)
		{
			CancelEventArgs e = new CancelEventArgs();
			Comment.OnApproving(comment, e);
			if (!e.Cancel)
			{
				int inx = Comments.IndexOf(comment);
				Comments[inx].IsApproved = true;
				this.DateModified = comment.DateCreated;
				this.DataUpdate();
				Comment.OnApproved(comment);
				SendNotifications(comment);
			}
		}

        /// <summary>
		/// Approves all the comments in a post.  Included to save time on the approval process.
		/// </summary>
		public void ApproveAllComments()
		{
			foreach (Comment comment in Comments)
			{
				ApproveComment(comment);
			}
		}

		#endregion

		#region Base overrides

		///// <summary>
		///// Saves the object to the data store (inserts, updates or deletes).
		///// </summary>
		///// <returns></returns>
		//public override SaveAction Save()
		//{
		//  SaveAction action = base.Save();
		//  if (action == SaveAction.Insert || action == SaveAction.Update)
		//    _Content = null;

		//  return action;
		//}

		/// <summary>
		/// Validates the Post instance.
		/// </summary>
		protected override void ValidationRules()
		{
			AddRule("Title", "Title must be set", String.IsNullOrEmpty(Title));
			AddRule("Content", "Content must be set", String.IsNullOrEmpty(Content));
		}

		/// <summary>
		/// Returns a Post based on the specified id.
		/// </summary>
		protected override Post DataSelect(Guid id)
		{
			return BlogService.SelectPost(id);
		}

		/// <summary>
		/// Updates the Post.
		/// </summary>
		protected override void DataUpdate()
		{
			BlogService.UpdatePost(this);
			Posts.Sort();
			AddRelations();
			ResetNestedComments();
		}

		/// <summary>
		/// Inserts a new post to the current BlogProvider.
		/// </summary>
		protected override void DataInsert()
		{
			BlogService.InsertPost(this);

			if (this.IsNew)
			{
				Posts.Add(this);
				Posts.Sort();
				AddRelations();
			}
		}

		/// <summary>
		/// Deletes the Post from the current BlogProvider.
		/// </summary>
		protected override void DataDelete()
		{
			BlogService.DeletePost(this);
			if (Posts.Contains(this))
			{
				Posts.Remove(this);
				Dispose();
				AddRelations();
			}
		}

		/// <summary>
		/// Gets if the Post have been changed.
		/// </summary>
		public override bool IsChanged
		{
			get
			{
				if (base.IsChanged)
					return true;

                if (Categories.IsChanged || Tags.IsChanged || NotificationEmails.IsChanged)
					return true;

				return false;
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return Title;
		}

		/// <summary>
		/// Marks the object as being an clean,
		/// which means not dirty.
		/// </summary>
		public override void MarkOld()
		{
			this.Categories.MarkOld();
			this.Tags.MarkOld();
            this.NotificationEmails.MarkOld();
			base.MarkOld();
		}

   
		#endregion

		#region IComparable<Post> Members

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the 
		/// objects being compared. The return value has the following meanings: 
		/// Value Meaning Less than zero This object is less than the other parameter.Zero 
		/// This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		public int CompareTo(Post other)
		{
			return other.DateCreated.CompareTo(this.DateCreated);
		}

		#endregion

		#region Events

        /// <summary>
        /// Occurs before a new comment is added.
        /// </summary>
        public static event EventHandler<CancelEventArgs> AddingComment;
        /// <summary>
        /// Raises the event in a safe way
        /// </summary>
        protected virtual void OnAddingComment(Comment comment, CancelEventArgs e)
        {
            if (AddingComment != null)
            {
                AddingComment(comment, e);
            }
        }

        /// <summary>
        /// Occurs when a comment is added.
        /// </summary>
        public static event EventHandler<EventArgs> CommentAdded;
        /// <summary>
        /// Raises the event in a safe way
        /// </summary>
        protected virtual void OnCommentAdded(Comment comment)
        {
            if (CommentAdded != null)
            {
                CommentAdded(comment, new EventArgs());
            }
        }

        /// <summary>
        /// Occurs before a new comment is updated.
        /// </summary>
        public static event EventHandler<CancelEventArgs> UpdatingComment;
        /// <summary>
        /// Raises the event in a safe way
        /// </summary>
        protected virtual void OnUpdatingComment(Comment comment, CancelEventArgs e)
        {
            if (UpdatingComment != null)
            {
                UpdatingComment(comment, e);
            }
        }

        /// <summary>
        /// Occurs when a comment is updated.
        /// </summary>
        public static event EventHandler<EventArgs> CommentUpdated;
        /// <summary>
        /// Raises the event in a safe way
        /// </summary>
        protected virtual void OnCommentUpdated(Comment comment)
        {
            if (CommentUpdated != null)
            {
                CommentUpdated(comment, new EventArgs());
            }
        }

		/// <summary>
		/// Occurs before comment is removed.
		/// </summary>
		public static event EventHandler<CancelEventArgs> RemovingComment;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnRemovingComment(Comment comment, CancelEventArgs e)
		{
			if (RemovingComment != null)
			{
				RemovingComment(comment, e);
			}
		}

		/// <summary>
		/// Occurs when a comment has been removed.
		/// </summary>
		public static event EventHandler<EventArgs> CommentRemoved;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnCommentRemoved(Comment comment)
		{
			if (CommentRemoved != null)
			{
				CommentRemoved(comment, new EventArgs());
			}
		}

		/// <summary>
		/// Occurs when a visitor rates the post.
		/// </summary>
		public static event EventHandler<EventArgs> Rated;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		protected virtual void OnRated(Post post)
		{
			if (Rated != null)
			{
				Rated(post, new EventArgs());
			}
		}

		/// <summary>
		/// Occurs when the post is being served to the output stream.
		/// </summary>
		public static event EventHandler<ServingEventArgs> Serving;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		public static void OnServing(Post post, ServingEventArgs arg)
		{
			if (Serving != null)
			{
				Serving(post, arg);
			}
		}

		/// <summary>
		/// Raises the Serving event
		/// </summary>
		public void OnServing(ServingEventArgs eventArgs)
		{
			if (Serving != null)
			{
				Serving(this, eventArgs);
			}
		}

		#endregion

	}
}
