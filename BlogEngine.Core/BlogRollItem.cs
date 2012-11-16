#region Using

using System;
using System.Collections.Generic;
using System.Text;
using BlogEngine.Core.Providers;

#endregion

namespace BlogEngine.Core
{
    /// <summary>
    /// BlogRolls are links to outside blogs.
    /// </summary>
    [Serializable]
    public class BlogRollItem : BusinessBase<BlogRollItem, Guid>, IComparable<BlogRollItem>
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlogRollItem"/> class.
        /// </summary>
        public BlogRollItem()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlogRollItem"/> class.
        /// </summary>
        /// <param name="Title">The title of the BlogRollItem.</param>
        /// <param name="Description">The description of the BlogRollItem.</param>
        /// <param name="BlogUrl">The <see cref="Uri"/> of the BlogRollItem.</param>
        public BlogRollItem(string Title, string Description, Uri BlogUrl)
        {
            this.Id = Guid.NewGuid();
            this.Title = Title;
            this.Description = Description;
            this.BlogUrl = BlogUrl;
        }

        #endregion

        #region Properties

        private string _Title;
        /// <summary>
        /// Gets or sets the Title of the object.
        /// </summary>
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                if (_Title != value)
                {
                    MarkChanged("Title");
                }
                _Title = value;
            }
        }

        private string _Description;
        /// <summary>
        /// Gets or sets the Description of the object.
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                if (_Description != value)
                {
                    MarkChanged("Description");
                }
                _Description = value;
            }
        }

        private Uri _FeedUrl;
        /// <summary>
        /// Gets or sets the FeedUrl of the object.
        /// </summary>
        public Uri FeedUrl
        {
            get
            {
                return _FeedUrl;
            }
            set
            {
                if (_FeedUrl == null || !_FeedUrl.Equals(value))
                {
                    MarkChanged("FeedUrl");
                }
                _FeedUrl = value;
            }
        }

        private Uri _BlogUrl;
        /// <summary>
        /// Gets or sets the BlogUrl of the object.
        /// </summary>
        public Uri BlogUrl
        {
            get
            {
                return _BlogUrl;
            }
            set
            {
                if (_BlogUrl == null || !_BlogUrl.Equals(value))
                {
                    MarkChanged("BlogUrl");
                }
                _BlogUrl = value;
            }
        }

        private string _Xfn;
        /// <summary>
        /// Gets or sets the Xfn of the object.
        /// </summary>
        public string Xfn
        {
            get
            {
                return _Xfn;
            }
            set
            {
                if (_Xfn != value)
                {
                    MarkChanged("Xfn");
                }
                _Xfn = value;
            }
        }

        private int _SortIndex;
        /// <summary>
        /// Gets or sets the SortIndex of the object.
        /// </summary>
        public int SortIndex
        {
            get
            {
                return _SortIndex;
            }
            set
            {
                if (_SortIndex != value)
                {
                    MarkChanged("SortIndex");
                }
                _SortIndex = value;
            }
        }

        /// <summary>
        /// Gets the BlogRollItem from the data store.
        /// </summary>
        public static BlogRollItem GetBlogRollItem(Guid id)
        {
            return BlogRolls.Find(br => br.Id == id);
        }

		private static object _SyncRoot = new object();
        private static List<BlogRollItem> _BlogRolls;
        /// <summary>
        /// Gets all of the BlogRollItems from the data store.
        /// </summary>
        public static List<BlogRollItem> BlogRolls
        {
            get
            {
                if (_BlogRolls == null || _BlogRolls.Count == 0)
                {
                    lock (_SyncRoot)
                    {
                        if (_BlogRolls == null || _BlogRolls.Count == 0)
                        {
                            _BlogRolls = BlogService.FillBlogRolls();
                            _BlogRolls.Sort();
                        }
                    }
                }

                return _BlogRolls;
            }
        }

        #endregion

        #region Base overrides

        /// <summary>
        /// Reinforces the business rules by adding additional rules to the
        /// broken rules collection.
        /// </summary>
        protected override void ValidationRules()
        {
            AddRule("Title", "Title must be set", string.IsNullOrEmpty(Title));
            AddRule("BlogUrl", "BlogUrl must be set", BlogUrl == null);
        }

        /// <summary>
        /// Retrieves the object from the data store and populates it.
        /// </summary>
        /// <param name="id">The unique identifier of the object.</param>
        /// <returns>
        /// The object that was selected from the data store.
        /// </returns>
        protected override BlogRollItem DataSelect(Guid id)
        {
            return BlogService.SelectBlogRoll(id);
        }

        /// <summary>
        /// Updates the object in its data store.
        /// </summary>
        protected override void DataUpdate()
        {
            OnSaving(this, SaveAction.Update);
            if (IsChanged)
                BlogService.UpdateBlogRoll(this);
            OnSaved(this, SaveAction.Update);
        }

        /// <summary>
        /// Inserts a new object to the data store.
        /// </summary>
        protected override void DataInsert()
        {
            OnSaving(this, SaveAction.Insert);
            if (IsNew)
                BlogService.InsertBlogRoll(this);
            OnSaved(this, SaveAction.Insert);
        }

        /// <summary>
        /// Deletes the object from the data store.
        /// </summary>
        protected override void DataDelete()
        {
            OnSaving(this, SaveAction.Delete);
            if (IsDeleted)
                BlogService.DeleteBlogRoll(this);
            BlogRolls.Remove(this);
            OnSaved(this, SaveAction.Delete);

            Dispose();
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

        #endregion

        #region IComparable<BlogRoll> Members

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. 
        /// The return value has the following meanings: Value Meaning Less than zero This object is 
        /// less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
        /// </returns>
        public int CompareTo(BlogRollItem other)
        {
            return this.SortIndex.CompareTo(other.SortIndex);
        }

        #endregion
    }
}
