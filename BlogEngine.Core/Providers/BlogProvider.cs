#region Using

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// A base class for all custom providers to inherit from.
    /// </summary>
    public abstract partial class BlogProvider : ProviderBase
    {
        // Post
        /// <summary>
        /// Retrieves a Post from the provider based on the specified id.
        /// </summary>
        public abstract Post SelectPost(Guid id);
        ///// <summary>
        ///// Retrieves the content of the post in order to lazy load.
        ///// </summary>
        //public abstract string SelectPostContent(Guid id);
        /// <summary>
        /// Inserts a new Post into the data store specified by the provider.
        /// </summary>
        public abstract void InsertPost(Post post);
        /// <summary>
        /// Updates an existing Post in the data store specified by the provider.
        /// </summary>
        public abstract void UpdatePost(Post post);
        /// <summary>
        /// Deletes a Post from the data store specified by the provider.
        /// </summary>
        public abstract void DeletePost(Post post);
        /// <summary>
        /// Retrieves all Posts from the provider and returns them in a List.
        /// </summary>
        public abstract List<Post> FillPosts();

        // Page
        /// <summary>
        /// Retrieves a Page from the provider based on the specified id.
        /// </summary>
        public abstract Page SelectPage(Guid id);
        /// <summary>
        /// Inserts a new Page into the data store specified by the provider.
        /// </summary>
        public abstract void InsertPage(Page page);
        /// <summary>
        /// Updates an existing Page in the data store specified by the provider.
        /// </summary>
        public abstract void UpdatePage(Page page);
        /// <summary>
        /// Deletes a Page from the data store specified by the provider.
        /// </summary>
        public abstract void DeletePage(Page page);
        /// <summary>
        /// Retrieves all Pages from the provider and returns them in a List.
        /// </summary>
        public abstract List<Page> FillPages();

				// Profile
				/// <summary>
				/// Retrieves a Page from the provider based on the specified id.
				/// </summary>
			public abstract AuthorProfile SelectProfile(string id);
				/// <summary>
				/// Inserts a new Page into the data store specified by the provider.
				/// </summary>
			public abstract void InsertProfile(AuthorProfile profile);
				/// <summary>
				/// Updates an existing Page in the data store specified by the provider.
				/// </summary>
			public abstract void UpdateProfile(AuthorProfile profile);
				/// <summary>
				/// Deletes a Page from the data store specified by the provider.
				/// </summary>
			public abstract void DeleteProfile(AuthorProfile profile);
				/// <summary>
				/// Retrieves all Pages from the provider and returns them in a List.
				/// </summary>
			public abstract List<AuthorProfile> FillProfiles();

        /// <summary>
        /// Retrieves a Category from the provider based on the specified id.
        /// </summary>
        public abstract Category SelectCategory(Guid id);
        /// <summary>
        /// Inserts a new Category into the data store specified by the provider.
        /// </summary>
        public abstract void InsertCategory(Category category);
        /// <summary>
        /// Updates an existing Category in the data store specified by the provider.
        /// </summary>
        public abstract void UpdateCategory(Category category);
        /// <summary>
        /// Deletes a Category from the data store specified by the provider.
        /// </summary>
        public abstract void DeleteCategory(Category category);
        /// <summary>
        /// Retrieves all Categories from the provider and returns them in a List.
        /// </summary>
        public abstract List<Category> FillCategories();

        // Settings
        /// <summary>
        /// Loads the settings from the provider.
        /// </summary>
        public abstract StringDictionary LoadSettings();
        /// <summary>
        /// Saves the settings to the provider.
        /// </summary>
        public abstract void SaveSettings(StringDictionary settings);

        //Ping services
        /// <summary>
        /// Loads the ping services.
        /// </summary>
        /// <returns></returns>
        public abstract StringCollection LoadPingServices();
        /// <summary>
        /// Saves the ping services.
        /// </summary>
        /// <param name="services">The services.</param>
        public abstract void SavePingServices(StringCollection services);

        //Stop words
        /// <summary>
        /// Loads the stop words used in the search feature.
        /// </summary>
        public abstract StringCollection LoadStopWords();
              
        // Data Store
        /// <summary>
        /// Loads settings from data store
        /// </summary>
        /// <param name="exType">Extension Type</param>
        /// <param name="exId">Extensio Id</param>
        /// <returns>Settings as stream</returns>
        public abstract object LoadFromDataStore(DataStore.ExtensionType exType, string exId);
        /// <summary>
        /// Saves settings to data store
        /// </summary>
        /// <param name="exType">Extension Type</param>
        /// <param name="exId">Extension Id</param>
        /// <param name="settings">Settings object</param>
        public abstract void SaveToDataStore(DataStore.ExtensionType exType, string exId, object settings);
        /// <summary>
        /// Removes settings from data store
        /// </summary>
        /// <param name="exType">Extension Type</param>
        /// <param name="exId">Extension Id</param>
        public abstract void RemoveFromDataStore(DataStore.ExtensionType exType, string exId);

        /// <summary>
        /// Retrieves a BlogRoll from the provider based on the specified id.
        /// </summary>
        public abstract BlogRollItem SelectBlogRollItem(Guid Id);
        /// <summary>
        /// Inserts a new BlogRoll into the data store specified by the provider.
        /// </summary>
        public abstract void InsertBlogRollItem(BlogRollItem blogRollItem);
        /// <summary>
        /// Updates an existing BlogRoll in the data store specified by the provider.
        /// </summary>
        public abstract void UpdateBlogRollItem(BlogRollItem blogRollItem);
        /// <summary>
        /// Deletes a BlogRoll from the data store specified by the provider.
        /// </summary>
        public abstract void DeleteBlogRollItem(BlogRollItem blogRollItem);
        /// <summary>
        /// Retrieves all BlogRolls from the provider and returns them in a list.
        /// </summary>
        public abstract List<BlogRollItem> FillBlogRoll();

        /// <summary>
        /// Retrieves a Referrer from the provider based on the specified id.
        /// </summary>
        public abstract Referrer SelectReferrer(Guid Id);
        /// <summary>
        /// Inserts a new Referrer into the data store specified by the provider.
        /// </summary>
        public abstract void InsertReferrer(Referrer referrer);
        /// <summary>
        /// Updates an existing Referrer in the data store specified by the provider.
        /// </summary>
        public abstract void UpdateReferrer(Referrer referrer);
        /// <summary>
        /// Deletes a Referrer from the data store specified by the provider.
        /// </summary>
        public abstract List<Referrer> FillReferrers();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract string StorageLocation();
    }

    /// <summary>
    /// A collection of all registered providers.
    /// </summary>
    public class BlogProviderCollection : ProviderCollection
    {
        /// <summary>
        /// Gets a provider by its name.
        /// </summary>
        public new BlogProvider this[string name]
        {
            get { return (BlogProvider)base[name]; }
        }

        /// <summary>
        /// Add a provider to the collection.
        /// </summary>
        public override void Add(ProviderBase provider)
        {
            if (provider == null)
                throw new ArgumentNullException("provider");

            if (!(provider is BlogProvider))
                throw new ArgumentException
                    ("Invalid provider type", "provider");

            base.Add(provider);
        }
    }

}
