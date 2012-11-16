#region Using

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Web.Configuration;
using System.Web;
using System.IO;
using BlogEngine.Core;
using BlogEngine.Core.DataStore;

#endregion

namespace BlogEngine.Core.Providers
{
	/// <summary>
	/// The proxy class for communication between
	/// the business objects and the providers.
	/// </summary>
	public static class BlogService
	{

		#region Provider model

		private static BlogProvider _provider;
		private static BlogProviderCollection _providers;
		private static object _lock = new object();

		/// <summary>
		/// Gets the current provider.
		/// </summary>
		public static BlogProvider Provider
		{
			get { LoadProviders(); return _provider; }
		}

		/// <summary>
		/// Gets a collection of all registered providers.
		/// </summary>
		public static BlogProviderCollection Providers
		{
			get { LoadProviders(); return _providers; }
		}

		/// <summary>
		/// Load the providers from the web.config.
		/// </summary>
		private static void LoadProviders()
		{
			// Avoid claiming lock if providers are already loaded
			if (_provider == null)
			{
				lock (_lock)
				{
					// Do this again to make sure _provider is still null
					if (_provider == null)
					{
						// Get a reference to the <blogProvider> section
						BlogProviderSection section = (BlogProviderSection)WebConfigurationManager.GetSection("BlogEngine/blogProvider");

						// Load registered providers and point _provider
						// to the default provider
						_providers = new BlogProviderCollection();
						ProvidersHelper.InstantiateProviders(section.Providers, _providers, typeof(BlogProvider));
						_provider = _providers[section.DefaultProvider];

						if (_provider == null)
							throw new ProviderException("Unable to load default BlogProvider");
					}
				}
			}
		}

		#endregion

		#region Posts

		/// <summary>
		/// Returns a Post based on the specified id.
		/// </summary>
		public static Post SelectPost(Guid id)
		{
			LoadProviders();
			return _provider.SelectPost(id);
		}

		///// <summary>
		///// Returns the content of a post.
		///// </summary>
		//public static string SelectPostContent(Guid id)
		//{
		//  LoadProviders();
		//  return _provider.SelectPostContent(id);
		//}

		/// <summary>
		/// Persists a new Post in the current provider.
		/// </summary>
		public static void InsertPost(Post post)
		{
			LoadProviders();
			_provider.InsertPost(post);
		}

		/// <summary>
		/// Updates an exsiting Post.
		/// </summary>
		public static void UpdatePost(Post post)
		{
			LoadProviders();
			_provider.UpdatePost(post);
		}

		/// <summary>
		/// Deletes the specified Post from the current provider.
		/// </summary>
		public static void DeletePost(Post post)
		{
			LoadProviders();
			_provider.DeletePost(post);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<Post> FillPosts()
		{
			LoadProviders();
			return _provider.FillPosts();
		}

		#endregion

		#region Pages

		/// <summary>
		/// Returns a Page based on the specified id.
		/// </summary>
		public static Page SelectPage(Guid id)
		{
			LoadProviders();
			return _provider.SelectPage(id);
		}

		/// <summary>
		/// Persists a new Page in the current provider.
		/// </summary>
		public static void InsertPage(Page page)
		{
			LoadProviders();
			_provider.InsertPage(page);
		}

		/// <summary>
		/// Updates an exsiting Page.
		/// </summary>
		public static void UpdatePage(Page page)
		{
			LoadProviders();
			_provider.UpdatePage(page);
		}

		/// <summary>
		/// Deletes the specified Page from the current provider.
		/// </summary>
		public static void DeletePage(Page page)
		{
			LoadProviders();
			_provider.DeletePage(page);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<Page> FillPages()
		{
			LoadProviders();
			return _provider.FillPages();
		}

		#endregion

		#region Author profiles

		/// <summary>
		/// Returns a Page based on the specified id.
		/// </summary>
		public static AuthorProfile SelectProfile(string id)
		{
			LoadProviders();
			return _provider.SelectProfile(id);
		}

		/// <summary>
		/// Persists a new Page in the current provider.
		/// </summary>
		public static void InsertProfile(AuthorProfile profile)
		{
			LoadProviders();
			_provider.InsertProfile(profile);
		}

		/// <summary>
		/// Updates an exsiting Page.
		/// </summary>
		public static void UpdateProfile(AuthorProfile profile)
		{
			LoadProviders();
			_provider.UpdateProfile(profile);
		}

		/// <summary>
		/// Deletes the specified Page from the current provider.
		/// </summary>
		public static void DeleteProfile(AuthorProfile profile)
		{
			LoadProviders();
			_provider.DeleteProfile(profile);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<AuthorProfile> FillProfiles()
		{
			LoadProviders();
			return _provider.FillProfiles();
		}

		#endregion

		#region Categories

		/// <summary>
		/// Returns a Category based on the specified id.
		/// </summary>
		public static Category SelectCategory(Guid id)
		{
			LoadProviders();
			return _provider.SelectCategory(id);
		}

		/// <summary>
		/// Persists a new Category in the current provider.
		/// </summary>
		public static void InsertCategory(Category category)
		{
			LoadProviders();
			_provider.InsertCategory(category);
		}

		/// <summary>
		/// Updates an exsiting Category.
		/// </summary>
		public static void UpdateCategory(Category category)
		{
			LoadProviders();
			_provider.UpdateCategory(category);
		}

		/// <summary>
		/// Deletes the specified Category from the current provider.
		/// </summary>
		public static void DeleteCategory(Category category)
		{
			LoadProviders();
			_provider.DeleteCategory(category);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<Category> FillCategories()
		{
			LoadProviders();
			return _provider.FillCategories();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Loads the settings from the provider and returns
		/// them in a StringDictionary for the BlogSettings class to use.
		/// </summary>
		public static System.Collections.Specialized.StringDictionary LoadSettings()
		{
			LoadProviders();
			return _provider.LoadSettings();
		}

		/// <summary>
		/// Save the settings to the current provider.
		/// </summary>
		public static void SaveSettings(System.Collections.Specialized.StringDictionary settings)
		{
			LoadProviders();
			_provider.SaveSettings(settings);
		}

		#endregion

		#region Ping services

		/// <summary>
		/// Loads the ping services.
		/// </summary>
		public static StringCollection LoadPingServices()
		{
			LoadProviders();
			return _provider.LoadPingServices();
		}

		/// <summary>
		/// Saves the ping services.
		/// </summary>
		/// <param name="services">The services.</param>
		public static void SavePingServices(StringCollection services)
		{
			LoadProviders();
			_provider.SavePingServices(services);
		}

		#endregion

		#region Stop words

		/// <summary>
		/// Loads the stop words from the data store.
		/// </summary>
		public static StringCollection LoadStopWords()
		{
			LoadProviders();
			return _provider.LoadStopWords();
		}

		#endregion

		#region Data Store
		/// <summary>
		/// Loads settings from data storage
		/// </summary>
		/// <param name="exType">Extension Type</param>
		/// <param name="exId">Extension ID</param>
		/// <returns>Settings as stream</returns>
		public static object LoadFromDataStore(ExtensionType exType, string exId)
		{
			LoadProviders();
			return _provider.LoadFromDataStore(exType, exId);
		}

		/// <summary>
		/// Saves settings to data store
		/// </summary>
		/// <param name="exType">Extension Type</param>
		/// <param name="exId">Extensio ID</param>
		/// <param name="settings">Settings object</param>
		public static void SaveToDataStore(ExtensionType exType, string exId, object settings)
		{
			LoadProviders();
			_provider.SaveToDataStore(exType, exId, settings);
		}

		/// <summary>
		/// Removes object from data store
		/// </summary>
		/// <param name="exType">Extension Type</param>
		/// <param name="exId">Extension Id</param>
		public static void RemoveFromDataStore(ExtensionType exType, string exId)
		{
			LoadProviders();
			_provider.RemoveFromDataStore(exType, exId);
		}

		///<summary>
		///</summary>
		///<returns></returns>
		public static string GetStorageLocation()
		{
			LoadProviders();
			return _provider.StorageLocation();
		}
		#endregion

        #region BlogRolls

        /// <summary>
        /// Returns a BlogRoll based on the specified id.
        /// </summary>
        public static BlogRollItem SelectBlogRoll(Guid id)
        {
            LoadProviders();
            return _provider.SelectBlogRollItem(id);
        }

        /// <summary>
        /// Persists a new BlogRoll in the current provider.
        /// </summary>
        public static void InsertBlogRoll(BlogRollItem blogRoll)
        {
            LoadProviders();
            _provider.InsertBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Updates an exsiting BlogRoll.
        /// </summary>
        public static void UpdateBlogRoll(BlogRollItem blogRoll)
        {
            LoadProviders();
            _provider.UpdateBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Deletes the specified BlogRoll from the current provider.
        /// </summary>
        public static void DeleteBlogRoll(BlogRollItem blogRoll)
        {
            LoadProviders();
            _provider.DeleteBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Returns a list of all BlogRolls in the current provider.
        /// </summary>
        /// <returns></returns>
        public static List<BlogRollItem> FillBlogRolls()
        {
            LoadProviders();
            return _provider.FillBlogRoll();
        }

        #endregion

        #region Referrers

        /// <summary>
        /// Returns a Referrer based on the specified id.
        /// </summary>
        public static Referrer SelectReferrer(Guid Id)
        {
            LoadProviders();
            return _provider.SelectReferrer(Id);
        }

        /// <summary>
        /// Persists a new Referrer in the current provider.
        /// </summary>
        public static void InsertReferrer(Referrer referrer)
        {
            LoadProviders();
            _provider.InsertReferrer(referrer);
        }

        /// <summary>
        /// Updates an existing Referrer.
        /// </summary>
        public static void UpdateReferrer(Referrer referrer)
        {
            LoadProviders();
            _provider.UpdateReferrer(referrer);
        }

        /// <summary>
        /// Returns a list of all Referrers in the current provider.
        /// </summary>
        /// <returns></returns>
        public static List<Referrer> FillReferrers()
        {
            LoadProviders();
            return _provider.FillReferrers();
        }

        #endregion

    }
}
