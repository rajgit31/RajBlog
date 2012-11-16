#region Using

using System;
using System.Collections.Generic;
using System.Text;
using BlogEngine.Core.Providers;

#endregion

namespace BlogEngine.Core
{
    /// <summary>
    /// Referrers are web sites that users followed to get to your blog.
    /// </summary>
    [Serializable]
    public class Referrer : BusinessBase<Referrer, Guid>, IComparable<Referrer>
    {

                #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Referrer"/> class.
        /// </summary>
        public Referrer()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Referrer"/> class.
        /// </summary>
        /// <param name="Referrer">The ReferrerUrl for the Referrer.</param>
        public Referrer(Uri Referrer)
            : this()
        {
            this.ReferrerUrl = Referrer;
        }

        #endregion

        #region Properties

        private Uri _Referrer;
        /// <summary>
        /// Gets or sets the referrer address of the object.
        /// </summary>
        public Uri ReferrerUrl
        {
            get
            {
                return _Referrer;
            }
            set
            {
                if (_Referrer == null || !_Referrer.Equals(value))
                {
                    MarkChanged("Referrer");
                }
                _Referrer = value;
            }
        }

        private Uri _Url;
        /// <summary>
        /// Gets or sets the referrer Url of the object.
        /// </summary>
        public Uri Url
        {
            get
            {
                return _Url;
            }
            set
            {
                if (_Url == null || !_Url.Equals(value))
                {
                    MarkChanged("Url");
                }
                _Url = value;
            }
        }

        private int _Count;
        /// <summary>
        /// Gets or sets the Count of the object.
        /// </summary>
        public int Count
        {
            get
            {
                return _Count;
            }
            set
            {
                if (_Count != value)
                {
                    MarkChanged("Count");
                }
                _Count = value;
            }
        }

        private DateTime _Day;
        /// <summary>
        /// Gets or sets the Day of the object.
        /// </summary>
        public DateTime Day
        {
            get
            {
                return _Day;
            }
            set
            {
                if (!_Day.Equals(value))
                {
                    MarkChanged("Day");
                }
                _Day = value;
            }
        }

        private bool _PossibleSpam;
        /// <summary>
        /// Gets or sets the PossibleSpam of the object.
        /// </summary>
        public bool PossibleSpam
        {
            get
            {
                return _PossibleSpam;
            }
            set
            {
                if (_PossibleSpam != value)
                {
                    MarkChanged("PossibleSpam");
                }
                _PossibleSpam = value;
            }
        }

        private static object _SyncRoot = new object();
        private static List<Referrer> _Referrers;
        /// <summary>
        /// Gets all of the Referrers from the data store.
        /// </summary>
        public static List<Referrer> Referrers
        {
            get
            {
                if (_Referrers == null || _Referrers.Count == 0)
                {
                    lock (_SyncRoot)
                    {
                        if (_Referrers == null || _Referrers.Count == 0)
                        {
                            _Referrers = BlogService.FillReferrers();
                            parseReferrers();
                        }
                    }
                }

                return _Referrers;
            }
        }

        private static void parseReferrers()
        {
            _referrersByDay = new Dictionary<DateTime, List<Referrer>>();
            foreach (Referrer refer in _Referrers)
            {
                if (_referrersByDay.ContainsKey(refer.Day))
                {
                    _referrersByDay[refer.Day].Add(refer);
                }
                else
                {
                    _referrersByDay.Add(refer.Day, new List<Referrer>() { refer });
                }
            }
        }

        private static void AddReferrer(Referrer referrer)
        {
            List<Referrer> day;
            if (ReferrersByDay.ContainsKey(referrer.Day))
            {
                day = ReferrersByDay[referrer.Day];
            }
            else
            {
                day = new List<Referrer>();
                ReferrersByDay.Add(referrer.Day, day);
            }

            if (!day.Contains(referrer))
            {
                day.Add(referrer);
            }
        }

        private static Dictionary<DateTime, List<Referrer>> _referrersByDay;
        /// <summary>
        /// An automatically maintained Dictionary of Referrers separated by Day.
        /// </summary>
        public static Dictionary<DateTime, List<Referrer>> ReferrersByDay
        {
            get
            {
                if ( Referrers == null)
                {
                    parseReferrers();
                }
                return _referrersByDay;
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
            AddRule("Referrer", "Referrer must be set", ReferrerUrl == null);
            AddRule("Day", "Day must be set", Day == DateTime.MinValue);
        }

        /// <summary>
        /// Retrieves the object from the data store and populates it.
        /// </summary>
        /// <param name="id">The unique identifier of the object.</param>
        /// <returns>
        /// The object that was selected from the data store.
        /// </returns>
        protected override Referrer DataSelect(Guid id)
        {
            return BlogService.SelectReferrer(id);
        }

        /// <summary>
        /// Updates the object in its data store.
        /// </summary>
        protected override void DataUpdate()
        {
            OnSaving(this, SaveAction.Update);
            if (IsChanged)
                BlogService.UpdateReferrer(this);
            OnSaved(this, SaveAction.Update);
        }

        /// <summary>
        /// Inserts a new object to the data store.
        /// </summary>
        protected override void DataInsert()
        {
            OnSaving(this, SaveAction.Insert);
            if (IsNew)
            {
                BlogService.InsertReferrer(this);
                AddReferrer(this);
            }
            OnSaved(this, SaveAction.Insert);

        }

        /// <summary>
        /// Deletes the object from the data store.
        /// </summary>
        protected override void DataDelete()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return this.ReferrerUrl.ToString();
        }

        #endregion

        #region IComparable<Referrer> Members

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. 
        /// The return value has the following meanings: Value Meaning Less than zero This object is 
        /// less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
        /// </returns>
        public int CompareTo(Referrer other)
        {
            string compareThis = string.Format("{0} {1}", this.ReferrerUrl.ToString(), this.Url.ToString());
            string compareOther = string.Format("{0} {1}", other.ReferrerUrl.ToString(), other.Url.ToString());
            return compareThis.CompareTo(compareOther);
        }

        #endregion
    }
}
