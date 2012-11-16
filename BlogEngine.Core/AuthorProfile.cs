#region Using

using System;
using System.Collections.Generic;
using BlogEngine.Core.Providers;

#endregion

namespace BlogEngine.Core
{
	public class AuthorProfile : BusinessBase<AuthorProfile, string>
	{

		#region Constructors

		public AuthorProfile()
		{

		}

		public AuthorProfile(string username)
		{
			base.Id = username;
		}

		#endregion

		#region Properties

		private static object _SyncRoot = new object();
		private static List<AuthorProfile> _Profiles;
		/// <summary>
		/// Gets an unsorted list of all pages.
		/// </summary>
		public static List<AuthorProfile> Profiles
		{
			get
			{
				if (_Profiles == null)
				{
					lock (_SyncRoot)
					{
						if (_Profiles == null)
						{
							_Profiles = BlogService.FillProfiles();
						}
					}
				}

				return _Profiles;
			}
		}

		public string UserName
		{
			get { return Id; }
		}


		public string FullName
		{
			get { return (FirstName + " " + MiddleName + " " + LastName).Replace("  ", " "); }
		}

		private bool _IsPrivate;

		public bool IsPrivate
		{
			get { return _IsPrivate; }
			set
			{
				if (value != _IsPrivate) MarkChanged("IsPrivate");
				_IsPrivate = value;
			}
		}

		private string _FirstName;

		public string FirstName
		{
			get { return _FirstName; }
			set
			{
				if (value != _FirstName) MarkChanged("FirstName");
				_FirstName = value;
			}
		}

		private string _MiddleName;

		public string MiddleName
		{
			get { return _MiddleName; }
			set
			{
				if (value != _MiddleName) MarkChanged("MiddleName");
				_MiddleName = value;
			}
		}

		private string _LastName;

		public string LastName
		{
			get { return _LastName; }
			set
			{
				if (value != _LastName) MarkChanged("LastName");
				_LastName = value;
			}
		}

		private string _DisplayName;

		public string DisplayName
		{
			get { return _DisplayName; }
			set
			{
				if (value != _DisplayName) MarkChanged("DisplayName");
				_DisplayName = value;
			}
		}

		private string _PhotoUrl;
		public string PhotoURL
		{
			get { return _PhotoUrl; }
			set
			{
				if (value != _PhotoUrl) MarkChanged("PhotoURL");
				_PhotoUrl = value;
			}
		}

		private DateTime _Birthday;

		public DateTime Birthday
		{
			get { return _Birthday; }
			set
			{
				if (value != _Birthday) MarkChanged("Birthday");
				_Birthday = value;
			}
		}

		//private string _Address1;
		//public string Address1
		//{
		//  get { return _Address1; }
		//  set { _Address1 = value; }
		//}

		//private string _Address2;

		//public string Address2
		//{
		//  get { return _Address2; }
		//  set { _Address2 = value; }
		//}

		private string _CityTown;

		public string CityTown
		{
			get { return _CityTown; }
			set
			{
				if (value != _CityTown) MarkChanged("CityTown");
				_CityTown = value;
			}
		}

		private string _RegionState;

		public string RegionState
		{
			get { return _RegionState; }
			set
			{
				if (value != _RegionState) MarkChanged("RegionState");
				_RegionState = value;
			}
		}

		private string _Country;

		public string Country
		{
			get { return _Country; }
			set
			{
				if (value != _Country) MarkChanged("Country");
				_Country = value;
			}
		}

		private string _PhoneMain;

		public string PhoneMain
		{
			get { return _PhoneMain; }
			set
			{
				if (value != _PhoneMain) MarkChanged("PhoneMain");
				_PhoneMain = value;
			}
		}

		private string _PhoneFax;

		public string PhoneFax
		{
			get { return _PhoneFax; }
			set
			{
				if (value != _PhoneFax) MarkChanged("PhoneFax");
				_PhoneFax = value;
			}
		}

		private string _PhoneMobile;

		public string PhoneMobile
		{
			get { return _PhoneMobile; }
			set
			{
				if (value != _PhoneMobile) MarkChanged("PhoneMobile");
				_PhoneMobile = value;
			}
		}

		private string _EmailAddress;

		public string EmailAddress
		{
			get { return _EmailAddress; }
			set
			{
				if (value != _EmailAddress) MarkChanged("EmailAddress");
				_EmailAddress = value;
			}
		}

		private string _Company;

		public string Company
		{
			get { return _Company; }
			set
			{
				if (value != _Company) MarkChanged("Company");
				_Company = value;
			}
		}


		private string _AboutMe;

		public string AboutMe
		{
			get { return _AboutMe; }
			set
			{
				if (value != _AboutMe) MarkChanged("AboutMe");
				_AboutMe = value;
			}
		}

		public string RelativeLink
		{
			get { return Utils.RelativeWebRoot + "author/" + Id + ".aspx"; ; }
		}


		#endregion

		#region Methods

		public static AuthorProfile GetProfile(string username)
		{
			return Profiles.Find(delegate(AuthorProfile p)
			{
				return p.UserName.Equals(username, StringComparison.OrdinalIgnoreCase);
			});
		}

		public override string ToString()
		{
			return FullName;
		}

		#endregion

		#region BusinessBaes overrides

		protected override void ValidationRules()
		{
			base.AddRule("Id", "Id must be set to the username of the user who the profile belongs to", string.IsNullOrEmpty(Id));
		}

		protected override AuthorProfile DataSelect(string id)
		{
			return BlogService.SelectProfile(id);
		}

		protected override void DataUpdate()
		{
			BlogService.UpdateProfile(this);
		}

		protected override void DataInsert()
		{
			BlogService.InsertProfile(this);

			if (IsNew)
				Profiles.Add(this);
		}

		protected override void DataDelete()
		{
			BlogService.DeleteProfile(this);
			if (Profiles.Contains(this))
				Profiles.Remove(this);
		}

		#endregion

	}
}