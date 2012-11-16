using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Data.Common;
using System.IO;
using BlogEngine.Core.DataStore;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Generic Database BlogProvider
    /// </summary>
    public partial class DbBlogProvider: BlogProvider
    {
        private string connStringName;
        private string tablePrefix;
        private string parmPrefix;

        /// <summary>
        /// Initializes the provider
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <param name="config">Configuration settings</param>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (String.IsNullOrEmpty(name))
            {
                name = "DbBlogProvider";
            }

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Generic Database Blog Provider");
            }

            base.Initialize(name, config);

            if (config["connectionStringName"] == null)
            {
                // default to BlogEngine
                config["connectionStringName"] = "BlogEngine";
            }
            connStringName = config["connectionStringName"];
            config.Remove("connectionStringName");

            if (config["tablePrefix"] == null)
            {
                // default
                config["tablePrefix"] = "be_";
            }
            tablePrefix = config["tablePrefix"];
            config.Remove("tablePrefix");

            if (config["parmPrefix"] == null)
            {
                // default
                config["parmPrefix"] = "@";
            }
            parmPrefix = config["parmPrefix"];
            config.Remove("parmPrefix");

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }
        }

        /// <summary>
        /// Returns a Post based on Id
        /// </summary>
        /// <param name="id">PostID</param>
        /// <returns>post</returns>
        public override Post SelectPost(Guid id)
        {
            Post post = new Post();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT PostID, Title, Description, PostContent, DateCreated, " +
                                "DateModified, Author, IsPublished, IsCommentEnabled, Raters, Rating, Slug " +
                                "FROM " + tablePrefix + "Posts " +
                                "WHERE PostID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    
                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = id.ToString();
                    cmd.Parameters.Add(dpID);

                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();

                            post.Id = rdr.GetGuid(0);
                            post.Title = rdr.GetString(1);
                            post.Content = rdr.GetString(3);
														post.Description = rdr.IsDBNull(2) ? String.Empty : rdr.GetString(2);
                            if (!rdr.IsDBNull(4))
                                post.DateCreated = rdr.GetDateTime(4);
                            if (!rdr.IsDBNull(5))
                                post.DateModified = rdr.GetDateTime(5);
                            if (!rdr.IsDBNull(6))
                                post.Author = rdr.GetString(6);
                            if (!rdr.IsDBNull(7))
                                post.IsPublished = rdr.GetBoolean(7);
                            if (!rdr.IsDBNull(8))
                                post.IsCommentsEnabled = rdr.GetBoolean(8);
                            if (!rdr.IsDBNull(9))
                                post.Raters = rdr.GetInt32(9);
                            if (!rdr.IsDBNull(10))
                                post.Rating = rdr.GetFloat(10);
                            if (!rdr.IsDBNull(11))
                                post.Slug = rdr.GetString(11);
                            else
                                post.Slug = "";
                        }
                    }

                    // Tags
                    sqlQuery = "SELECT Tag " +
                                "FROM " + tablePrefix + "PostTag " +
                                "WHERE PostID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                post.Tags.Add(rdr.GetString(0));
                        }
                    }
                    post.Tags.MarkOld();

                    // Categories
                    sqlQuery = "SELECT CategoryID " +
                                "FROM " + tablePrefix + "PostCategory " +
                                "WHERE PostID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            Guid key = rdr.GetGuid(0);
                            if (Category.GetCategory(key) != null)
                                post.Categories.Add(Category.GetCategory(key));
                        }
                    }

                    // Comments
					sqlQuery = "SELECT PostCommentID, CommentDate, Author, Email, Website, Comment, Country, Ip, IsApproved, ParentCommentID, ModeratedBy, Avatar " +
                                "FROM " + tablePrefix + "PostComment " +
                                "WHERE PostID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            Comment comment = new Comment();
                            comment.Id = rdr.GetGuid(0);
                            comment.IsApproved = true;
                            comment.Author = rdr.GetString(2);
                            if (!rdr.IsDBNull(4))
                            {
                                Uri website;
                                if (Uri.TryCreate(rdr.GetString(4), UriKind.Absolute, out website))
                                    comment.Website = website;
                            }
                            comment.Email = rdr.GetString(3);
                            comment.Content = rdr.GetString(5);
                            comment.DateCreated = rdr.GetDateTime(1);
                            comment.Parent = post;

                            if (!rdr.IsDBNull(6))
                                comment.Country = rdr.GetString(6);
                            if (!rdr.IsDBNull(7))
                                comment.IP = rdr.GetString(7);
                            if (!rdr.IsDBNull(8))
                                comment.IsApproved = rdr.GetBoolean(8);
                            else
                                comment.IsApproved = true;

							comment.ParentId = rdr.GetGuid(9);

                            if (!rdr.IsDBNull(10))
                                comment.ModeratedBy = rdr.GetString(10);

                            if (!rdr.IsDBNull(11))
                                comment.Avatar = rdr.GetString(11);

                            post.Comments.Add(comment);
                        }
                    }
                    post.Comments.Sort();

                    // Email Notification
                    sqlQuery = "SELECT NotifyAddress " +
                                "FROM " + tablePrefix + "PostNotify " +
                                "WHERE PostID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                post.NotificationEmails.Add(rdr.GetString(0));
                        }
                    }
                }
            }

            return post;
        }

        /// <summary>
        /// Adds a new post to database
        /// </summary>
        /// <param name="post">new post</param>
        public override void InsertPost(Post post)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + 
                        "Posts (PostID, Title, Description, PostContent, DateCreated, " +
                        "DateModified, Author, IsPublished, IsCommentEnabled, Raters, Rating, Slug)" +
                        "VALUES (@id, @title, @desc, @content, @created, @modified, " +
                        "@author, @published, @commentEnabled, @raters, @rating, @slug)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "title";
                    dpTitle.Value = post.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "desc";
                    dpDesc.Value = post.Description ?? "";
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpContent = provider.CreateParameter();
                    dpContent.ParameterName = parmPrefix + "content";
                    dpContent.Value = post.Content;
                    cmd.Parameters.Add(dpContent);

                    DbParameter dpCreated = provider.CreateParameter();
                    dpCreated.ParameterName = parmPrefix + "created";
                    dpCreated.Value = post.DateCreated.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpCreated);

                    DbParameter dpModified = provider.CreateParameter();
                    dpModified.ParameterName = parmPrefix + "modified";
                    if (post.DateModified == new DateTime())
                        dpModified.Value = DateTime.Now;
                    else
                        dpModified.Value = post.DateModified.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpModified);

                    DbParameter dpAuthor = provider.CreateParameter();
                    dpAuthor.ParameterName = parmPrefix + "author";
                    dpAuthor.Value = post.Author ?? "";
                    cmd.Parameters.Add(dpAuthor);

                    DbParameter dpPublished = provider.CreateParameter();
                    dpPublished.ParameterName = parmPrefix + "published";
                    dpPublished.Value = post.IsPublished;
                    cmd.Parameters.Add(dpPublished);

                    DbParameter dpCommentEnabled = provider.CreateParameter();
                    dpCommentEnabled.ParameterName = parmPrefix + "commentEnabled";
                    dpCommentEnabled.Value = post.IsCommentsEnabled;
                    cmd.Parameters.Add(dpCommentEnabled);

                    DbParameter dpRaters = provider.CreateParameter();
                    dpRaters.ParameterName = parmPrefix + "raters";
                    dpRaters.Value = post.Raters;
                    cmd.Parameters.Add(dpRaters);

                    DbParameter dpRating = provider.CreateParameter();
                    dpRating.ParameterName = parmPrefix + "rating";
                    dpRating.Value = post.Rating;
                    cmd.Parameters.Add(dpRating);

                    DbParameter dpSlug = provider.CreateParameter();
                    dpSlug.ParameterName = parmPrefix + "slug";
                    dpSlug.Value = post.Slug ?? "";
                    cmd.Parameters.Add(dpSlug);

                    cmd.ExecuteNonQuery();
                }

                // Tags
                UpdateTags(post, conn, provider);

                // Categories
                UpdateCategories(post, conn, provider);

                // Comments
                UpdateComments(post, conn, provider);

                // Email Notification
                UpdateNotify(post, conn, provider);
            }
        }

        /// <summary>
        /// Saves and existing post in the database
        /// </summary>
        /// <param name="post">post to be saved</param>
        public override void UpdatePost(Post post)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "UPDATE " + tablePrefix + "Posts " +
                                  "SET Title = @title, Description = @desc, PostContent = @content, " +
                                  "DateCreated = @created, DateModified = @modified, Author = @Author, " +
                                  "IsPublished = @published, IsCommentEnabled = @commentEnabled, " +
                                  "Raters = @raters, Rating = @rating, Slug = @slug " +
                                  "WHERE PostID = @id";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "title";
                    dpTitle.Value = post.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "desc";
                    dpDesc.Value = post.Description ?? "";
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpContent = provider.CreateParameter();
                    dpContent.ParameterName = parmPrefix + "content";
                    dpContent.Value = post.Content;
                    cmd.Parameters.Add(dpContent);

                    DbParameter dpCreated = provider.CreateParameter();
                    dpCreated.ParameterName = parmPrefix + "created";
                    dpCreated.Value = post.DateCreated.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpCreated);

                    DbParameter dpModified = provider.CreateParameter();
                    dpModified.ParameterName = parmPrefix + "modified";
                    if (post.DateModified == new DateTime())
                        dpModified.Value = DateTime.Now;
                    else
                        dpModified.Value = post.DateModified.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpModified);

                    DbParameter dpAuthor = provider.CreateParameter();
                    dpAuthor.ParameterName = parmPrefix + "author";
                    dpAuthor.Value = post.Author ?? "";
                    cmd.Parameters.Add(dpAuthor);

                    DbParameter dpPublished = provider.CreateParameter();
                    dpPublished.ParameterName = parmPrefix + "published";
                    dpPublished.Value = post.IsPublished;
                    cmd.Parameters.Add(dpPublished);

                    DbParameter dpCommentEnabled = provider.CreateParameter();
                    dpCommentEnabled.ParameterName = parmPrefix + "commentEnabled";
                    dpCommentEnabled.Value = post.IsCommentsEnabled;
                    cmd.Parameters.Add(dpCommentEnabled);

                    DbParameter dpRaters = provider.CreateParameter();
                    dpRaters.ParameterName = parmPrefix + "raters";
                    dpRaters.Value = post.Raters;
                    cmd.Parameters.Add(dpRaters);

                    DbParameter dpRating = provider.CreateParameter();
                    dpRating.ParameterName = parmPrefix + "rating";
                    dpRating.Value = post.Rating;
                    cmd.Parameters.Add(dpRating);

                    DbParameter dpSlug = provider.CreateParameter();
                    dpSlug.ParameterName = parmPrefix + "slug";
                    dpSlug.Value = post.Slug ?? "";
                    cmd.Parameters.Add(dpSlug);

                    cmd.ExecuteNonQuery();
                }

                // Tags
                UpdateTags(post, conn, provider);

                // Categories
                UpdateCategories(post, conn, provider);

                // Comments
                UpdateComments(post, conn, provider);

                // Email Notification
                UpdateNotify(post, conn, provider);
            }
        }

        /// <summary>
        /// Deletes a post in the database
        /// </summary>
        /// <param name="post">post to delete</param>
        public override void DeletePost(Post post)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "PostTag WHERE PostID = @id;" +
                                      "DELETE FROM " + tablePrefix + "PostCategory WHERE PostID = @id;" +
                                      "DELETE FROM " + tablePrefix + "PostNotify WHERE PostID = @id;" +
                                      "DELETE FROM " + tablePrefix + "PostComment WHERE PostID = @id;" +
                                      "DELETE FROM " + tablePrefix + "Posts WHERE PostID = @id;";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets all post from the database
        /// </summary>
        /// <returns>List of posts</returns>
        public override List<Post> FillPosts()
        {
            List<Post> posts = new List<Post>();
            List<string> postIDs = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT PostID FROM " + tablePrefix + "Posts ";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            postIDs.Add(rdr.GetGuid(0).ToString());
                        }
                    }
                }
            }

            foreach (string id in postIDs)
            {
                posts.Add(Post.Load(new Guid(id)));
            }

            posts.Sort();
            return posts;
        }

        /// <summary>
        /// Returns a page for given ID
        /// </summary>
        /// <param name="id">ID of page to return</param>
        /// <returns>selected page</returns>
        public override Page SelectPage(Guid id)
        {
            Page page = new Page();

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT PageID, Title, Description, PageContent, DateCreated, " +
                                        "   DateModified, Keywords, IsPublished, IsFrontPage, Parent, ShowInList, Slug " +
                                        "FROM " + tablePrefix + "Pages " +
                                        "WHERE PageID = " + parmPrefix + "id";

                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = id.ToString();
                    cmd.Parameters.Add(dpID);

                    conn.Open();
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();

                            page.Id = rdr.GetGuid(0);
                            page.Title = rdr.IsDBNull(1) ? String.Empty : rdr.GetString(1);
														page.Content = rdr.IsDBNull(3) ? String.Empty : rdr.GetString(3);
														page.Description = rdr.IsDBNull(2) ? String.Empty : rdr.GetString(2);
                            if (!rdr.IsDBNull(4))
                                page.DateCreated = rdr.GetDateTime(4);
                            if (!rdr.IsDBNull(5))
                                page.DateModified = rdr.GetDateTime(5);
                            if (!rdr.IsDBNull(6))
                                page.Keywords = rdr.GetString(6);
                            if (!rdr.IsDBNull(7))
                                page.IsPublished = rdr.GetBoolean(7);
                            if (!rdr.IsDBNull(8))
                                page.IsFrontPage = rdr.GetBoolean(8);
                            if (!rdr.IsDBNull(9))
                                page.Parent = rdr.GetGuid(9);
                            if (!rdr.IsDBNull(10))
                                page.ShowInList = rdr.GetBoolean(10);
                            if (!rdr.IsDBNull(11))
                                page.Slug = rdr.GetString(11);
                        }
                    }
                }
            }

            return page;
        }

        /// <summary>
        /// Adds a page to the database
        /// </summary>
        /// <param name="page">page to be added</param>
        public override void InsertPage(Page page)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "Pages (PageID, Title, Description, PageContent, " +
                                     "DateCreated, DateModified, Keywords, IsPublished, IsFrontPage, Parent, ShowInList, Slug) " +
                                     "VALUES (@id, @title, @desc, @content, @created, @modified, @keywords, @ispublished, @isfrontpage, @parent, @showinlist, @slug)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = page.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "title";
                    dpTitle.Value = page.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "desc";
                    dpDesc.Value = page.Description;
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpContent = provider.CreateParameter();
                    dpContent.ParameterName = parmPrefix + "content";
                    dpContent.Value = page.Content;
                    cmd.Parameters.Add(dpContent);

                    DbParameter dpCreated = provider.CreateParameter();
                    dpCreated.ParameterName = parmPrefix + "created";
                    dpCreated.Value = page.DateCreated.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpCreated);

                    DbParameter dpModified = provider.CreateParameter();
                    dpModified.ParameterName = parmPrefix + "modified";
                    if (page.DateModified == new DateTime())
                        dpModified.Value = DateTime.Now;
                    else
                        dpModified.Value = page.DateModified.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpModified);

                    DbParameter dpKeywords = provider.CreateParameter();
                    dpKeywords.ParameterName = parmPrefix + "keywords";
                    dpKeywords.Value = page.Keywords;
                    cmd.Parameters.Add(dpKeywords);

                    DbParameter dpPublished = provider.CreateParameter();
                    dpPublished.ParameterName = parmPrefix + "ispublished";
                    dpPublished.Value = page.IsPublished;
                    cmd.Parameters.Add(dpPublished);

                    DbParameter dpFrontPage = provider.CreateParameter();
                    dpFrontPage.ParameterName = parmPrefix + "isfrontpage";
                    dpFrontPage.Value = page.IsFrontPage;
                    cmd.Parameters.Add(dpFrontPage);

                    DbParameter dpParent = provider.CreateParameter();
                    dpParent.ParameterName = parmPrefix + "parent";
                    dpParent.Value = page.Parent.ToString();
                    cmd.Parameters.Add(dpParent);

                    DbParameter dpShowInList = provider.CreateParameter();
                    dpShowInList.ParameterName = parmPrefix + "showinlist";
                    dpShowInList.Value = page.ShowInList;
                    cmd.Parameters.Add(dpShowInList);

                    DbParameter dpSlug = provider.CreateParameter();
                    dpSlug.ParameterName = parmPrefix + "slug";
                    dpSlug.Value = page.Slug;
                    cmd.Parameters.Add(dpSlug);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Saves an existing page in the database
        /// </summary>
        /// <param name="page">page to be saved</param>
        public override void UpdatePage(Page page)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "UPDATE " + tablePrefix + "Pages " +
                                        "SET Title = @title, Description = @desc, PageContent = @content, " +
                                        "DateCreated = @created, DateModified = @modified, Keywords = @keywords, " +
                                        "IsPublished = @ispublished, IsFrontPage = @isfrontpage, Parent = @parent, ShowInList = @showinlist, Slug = @slug " +
                                        "WHERE PageID = @id";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = page.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "title";
                    dpTitle.Value = page.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "desc";
                    dpDesc.Value = page.Description;
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpContent = provider.CreateParameter();
                    dpContent.ParameterName = parmPrefix + "content";
                    dpContent.Value = page.Content;
                    cmd.Parameters.Add(dpContent);

                    DbParameter dpCreated = provider.CreateParameter();
                    dpCreated.ParameterName = parmPrefix + "created";
                    dpCreated.Value = page.DateCreated.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpCreated);

                    DbParameter dpModified = provider.CreateParameter();
                    dpModified.ParameterName = parmPrefix + "modified";
                    if (page.DateModified == new DateTime())
                        dpModified.Value = DateTime.Now;
                    else
                        dpModified.Value = page.DateModified.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpModified);

                    DbParameter dpKeywords = provider.CreateParameter();
                    dpKeywords.ParameterName = parmPrefix + "keywords";
                    dpKeywords.Value = page.Keywords;
                    cmd.Parameters.Add(dpKeywords);

                    DbParameter dpPublished = provider.CreateParameter();
                    dpPublished.ParameterName = parmPrefix + "ispublished";
                    dpPublished.Value = page.IsPublished;
                    cmd.Parameters.Add(dpPublished);

                    DbParameter dpFrontPage = provider.CreateParameter();
                    dpFrontPage.ParameterName = parmPrefix + "isfrontpage";
                    dpFrontPage.Value = page.IsFrontPage;
                    cmd.Parameters.Add(dpFrontPage);

                    DbParameter dpParent = provider.CreateParameter();
                    dpParent.ParameterName = parmPrefix + "parent";
                    dpParent.Value = page.Parent.ToString();
                    cmd.Parameters.Add(dpParent);

                    DbParameter dpShowInList = provider.CreateParameter();
                    dpShowInList.ParameterName = parmPrefix + "showinlist";
                    dpShowInList.Value = page.ShowInList;
                    cmd.Parameters.Add(dpShowInList);

                    DbParameter dpSlug = provider.CreateParameter();
                    dpSlug.ParameterName = parmPrefix + "slug";
                    dpSlug.Value = page.Slug;
                    cmd.Parameters.Add(dpSlug);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a page from the database
        /// </summary>
        /// <param name="page">page to be deleted</param>
        public override void DeletePage(Page page)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "Pages " +
                        "WHERE PageID = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "id";
                    dpID.Value = page.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets all pages in database
        /// </summary>
        /// <returns>List of pages</returns>
        public override List<Page> FillPages()
        {
            List<Page> pages = new List<Page>();
            List<string> pageIDs = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT PageID FROM " + tablePrefix + "Pages ";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            pageIDs.Add(rdr.GetGuid(0).ToString());
                        }
                    }
                }
            }

            foreach (string id in pageIDs)
            {
                pages.Add(Page.Load(new Guid(id)));
            }

            return pages;
        }

        /// <summary>
        /// Returns a category 
        /// </summary>
        /// <param name="id">Id of category to return</param>
        /// <returns></returns>
        public override Category SelectCategory(Guid id)
        {
            List<Category> categories = Category.Categories;

            Category category = new Category();

            foreach (Category cat in categories)
            {
                if (cat.Id == id)
                    category = cat;
            }
            category.MarkOld();
            return category;
        }

        /// <summary>
        /// Adds a new category to the database
        /// </summary>
        /// <param name="category">category to add</param>
        public override void InsertCategory(Category category)
        {
            List<Category> categories = Category.Categories;
            categories.Add(category);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "Categories (CategoryID, CategoryName, description, ParentID) " +
                                        "VALUES (@catid, @catname, @description, @parentid)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "catid";
                    dpID.Value = category.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "catname";
                    dpTitle.Value = category.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "description";
                    dpDesc.Value = category.Description;
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpParent = provider.CreateParameter();
                    dpParent.ParameterName = parmPrefix + "parentid";
                    if (category.Parent == null)
                        dpParent.Value = DBNull.Value;
                    else 
                        dpParent.Value = category.Parent.ToString();
                    cmd.Parameters.Add(dpParent);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Saves an existing category to the database
        /// </summary>
        /// <param name="category">category to be saved</param>
        public override void UpdateCategory(Category category)
        {
            List<Category> categories = Category.Categories;
            categories.Remove(category);
            categories.Add(category);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "UPDATE " + tablePrefix + "Categories " +
                                  "SET CategoryName = @catname, " +
                                  "Description = @description, ParentID = @parentid " +
                                  "WHERE CategoryID = @catid";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "catid";
                    dpID.Value = category.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    DbParameter dpTitle = provider.CreateParameter();
                    dpTitle.ParameterName = parmPrefix + "catname";
                    dpTitle.Value = category.Title;
                    cmd.Parameters.Add(dpTitle);

                    DbParameter dpDesc = provider.CreateParameter();
                    dpDesc.ParameterName = parmPrefix + "description";
                    dpDesc.Value = category.Description;
                    cmd.Parameters.Add(dpDesc);

                    DbParameter dpParent = provider.CreateParameter();
                    dpParent.ParameterName = parmPrefix + "parentid";
                    if (category.Parent == null)
                        dpParent.Value = DBNull.Value;
                    else
                        dpParent.Value = category.Parent.ToString();
                    cmd.Parameters.Add(dpParent); 
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a category from the database
        /// </summary>
        /// <param name="category">category to be removed</param>
        public override void DeleteCategory(Category category)
        {
            List<Category> categories = Category.Categories;
            categories.Remove(category);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "PostCategory " + 
                        "WHERE CategoryID = " + parmPrefix + "catid;" +
                        "DELETE FROM " + tablePrefix + "Categories " +
                        "WHERE CategoryID = " + parmPrefix + "catid";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "catid";
                    dpID.Value = category.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets all categories in database
        /// </summary>
        /// <returns>List of categories</returns>
        public override List<Category> FillCategories()
        {
            List<Category> categories = new List<Category>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT CategoryID, CategoryName, description, ParentID " +
                        "FROM " + tablePrefix + "Categories ";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Category cat = new Category();
                                cat.Title = rdr.GetString(1);
                                if (rdr.IsDBNull(2))
                                    cat.Description = "";
                                else
                                    cat.Description = rdr.GetString(2);
                                if (rdr.IsDBNull(3))
                                    cat.Parent = null;
                                else
                                    cat.Parent = new Guid(rdr.GetGuid(3).ToString());
                                cat.Id = new Guid(rdr.GetGuid(0).ToString());
                                categories.Add(cat);
                                cat.MarkOld();
                            }
                        }
                    }
                }
            }

            return categories;
        }

        /// <summary>
        /// Gets a BlogRoll based on a Guid.
        /// </summary>
        /// <param name="id">The BlogRoll's Guid.</param>
        /// <returns>A matching BlogRoll</returns>
        public override BlogRollItem SelectBlogRollItem(Guid id)
        {
            BlogRollItem blogRoll = BlogRollItem.BlogRolls.Find(br => br.Id == id);
            if (blogRoll == null)
            {
                blogRoll = new BlogRollItem();
            }
            blogRoll.MarkOld();
            return blogRoll;
        }

        /// <summary>
        /// Adds a new BlogRoll to the database.
        /// </summary>
        /// <param name="blogRoll">BlogRoll to add.</param>
        public override void InsertBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRolls = BlogRollItem.BlogRolls;
            blogRolls.Add(blogRollItem);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "BlogRollItems (BlogRollId, Title, Description, BlogUrl, FeedUrl, Xfn, SortIndex) " +
                                        "VALUES (@BlogRollId, @Title, @Description, @BlogUrl, @FeedUrl, @Xfn, @SortIndex)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    addBlogRollParametersToCommand(blogRollItem, provider, cmd);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void addBlogRollParametersToCommand(BlogRollItem blogRollItem, DbProviderFactory provider, DbCommand cmd)
        {
            DbParameter dpID = provider.CreateParameter();
            dpID.ParameterName = parmPrefix + "BlogRollId";
            dpID.Value = blogRollItem.Id.ToString();
            cmd.Parameters.Add(dpID);

            DbParameter dpTitle = provider.CreateParameter();
            dpTitle.ParameterName = parmPrefix + "Title";
            dpTitle.Value = blogRollItem.Title;
            cmd.Parameters.Add(dpTitle);

            DbParameter dpDesc = provider.CreateParameter();
            dpDesc.ParameterName = parmPrefix + "Description";
            dpDesc.Value = blogRollItem.Description;
            cmd.Parameters.Add(dpDesc);

            DbParameter dpBlogUrl = provider.CreateParameter();
            dpBlogUrl.ParameterName = "BlogUrl";
            dpBlogUrl.Value = blogRollItem.BlogUrl != null ? (object)blogRollItem.BlogUrl.ToString() : DBNull.Value;
            cmd.Parameters.Add(dpBlogUrl);

            DbParameter dpFeedUrl = provider.CreateParameter();
            dpFeedUrl.ParameterName = "FeedUrl";
            dpFeedUrl.Value = blogRollItem.FeedUrl != null ? (object)blogRollItem.FeedUrl.ToString() : DBNull.Value;
            cmd.Parameters.Add(dpFeedUrl);

            DbParameter dpXfn = provider.CreateParameter();
            dpXfn.ParameterName = "Xfn";
            dpXfn.Value = blogRollItem.Xfn;
            cmd.Parameters.Add(dpXfn);

            DbParameter dpSortIndex = provider.CreateParameter();
            dpSortIndex.ParameterName = "SortIndex";
            dpSortIndex.Value = blogRollItem.SortIndex;
            cmd.Parameters.Add(dpSortIndex);
        }

        /// <summary>
        /// Saves an existing BlogRoll to the database
        /// </summary>
        /// <param name="blogRollItem">BlogRoll to be saved</param>
        public override void UpdateBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRolls = BlogRollItem.BlogRolls;
            blogRolls.Remove(blogRollItem);
            blogRolls.Add(blogRollItem);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "UPDATE " + tablePrefix + "BlogRollItems " +
                                      "SET Title = @Title, " +
                                      "Description = @Description, BlogUrl = @BlogUrl, " +
                                      "FeedUrl = @FeedUrl, Xfn = @Xfn, SortIndex = @SortIndex " +
                                      "WHERE BlogRollId = @BlogRollId";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    addBlogRollParametersToCommand(blogRollItem, provider, cmd);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a BlogRoll from the database
        /// </summary>
        /// <param name="blogRoll">BlogRoll to be removed</param>
        public override void DeleteBlogRollItem(BlogRollItem blogRollItem)
        {
            List<BlogRollItem> blogRolls = BlogRollItem.BlogRolls;
            blogRolls.Remove(blogRollItem);
            blogRolls.Add(blogRollItem);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "BlogRollItems " +
                                      "WHERE BlogRollId = " + parmPrefix + "BlogRollId";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "BlogRollId";
                    dpID.Value = blogRollItem.Id.ToString();
                    cmd.Parameters.Add(dpID);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets all BlogRolls in database
        /// </summary>
        /// <returns>List of BlogRolls</returns>
        public override List<BlogRollItem> FillBlogRoll()
        {
            List<BlogRollItem> blogRoll = new List<BlogRollItem>();

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT BlogRollId, Title, Description, BlogUrl, FeedUrl, Xfn, SortIndex " +
                        "FROM " + tablePrefix + "BlogRollItems ";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                BlogRollItem br = new BlogRollItem()
                                {
                                    Id = rdr.GetGuid(0),
                                    Title = rdr.GetString(1),
                                    Description = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                                    BlogUrl = rdr.IsDBNull(3) ? null : new Uri(rdr.GetString(3)),
                                    FeedUrl = rdr.IsDBNull(4) ? null : new Uri(rdr.GetString(4)),
                                    Xfn = rdr.IsDBNull(5) ? string.Empty : rdr.GetString(5),
                                    SortIndex = rdr.GetInt32(6)
                                };

                                blogRoll.Add(br);
                                br.MarkOld();
                            }
                        }
                    }
                }
            }

            return blogRoll;
        }


        /// <summary>
        /// Gets a Referrer based on an Id.
        /// </summary>
        /// <param name="id">The Referrers's Guid.</param>
        /// <returns>A matching Referrer</returns>
        public override Referrer SelectReferrer(Guid Id)
        {
            Referrer refer = Referrer.Referrers.Find(r => r.Id.Equals(Id));
            if (refer == null)
            {
                refer = new Referrer();
            }
            refer.MarkOld();
            return refer;
        }

        /// <summary>
        /// Adds a new Referrer to the database.
        /// </summary>
        /// <param name="referrer">Referrer to add.</param>
        public override void InsertReferrer(Referrer referrer)
        {
            List<Referrer> referrers = Referrer.Referrers;
            referrers.Add(referrer);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "Referrers (ReferrerId, ReferralDay, ReferrerUrl, ReferralCount, Url, IsSpam) " +
                        "VALUES (@ReferrerId, @ReferralDay, @ReferrerUrl, @ReferralCount, @Url, @IsSpam)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);

                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    addReferrersParametersToCommand(referrer, provider, cmd);

                    cmd.ExecuteNonQuery();

                }
            }
        }

        private void addReferrersParametersToCommand(Referrer referrer, DbProviderFactory provider, DbCommand cmd)
        {
            DbParameter dpId = provider.CreateParameter();
            dpId.ParameterName = "ReferrerId";
            dpId.Value = referrer.Id.ToString();
            cmd.Parameters.Add(dpId);

            DbParameter dpDay = provider.CreateParameter();
            dpDay.ParameterName = parmPrefix + "ReferralDay";
            dpDay.Value = referrer.Day;
            cmd.Parameters.Add(dpDay);

            DbParameter dpReferrer = provider.CreateParameter();
            dpReferrer.ParameterName = parmPrefix + "ReferrerUrl";
            dpReferrer.Value = referrer.ReferrerUrl != null ? (object)referrer.ReferrerUrl.ToString() : DBNull.Value;
            cmd.Parameters.Add(dpReferrer);

            DbParameter dpCount = provider.CreateParameter();
            dpCount.ParameterName = parmPrefix + "ReferralCount";
            dpCount.Value = referrer.Count;
            cmd.Parameters.Add(dpCount);

            DbParameter dpUrl = provider.CreateParameter();
            dpUrl.ParameterName = "Url";
            dpUrl.Value = referrer.Url != null ? (object)referrer.Url.ToString() : DBNull.Value;
            cmd.Parameters.Add(dpUrl);

            DbParameter dpIsSpam = provider.CreateParameter();
            dpIsSpam.ParameterName = "IsSpam";
            dpIsSpam.Value = referrer.PossibleSpam;
            cmd.Parameters.Add(dpIsSpam);
        }

        /// <summary>
        /// Saves an existing Referrer to the database.
        /// </summary>
        /// <param name="referrer">Referrer to be saved.</param>
        public override void UpdateReferrer(Referrer referrer)
        {
            List<Referrer> referrers = Referrer.Referrers;
            referrers.Remove(referrer);
            referrers.Add(referrer);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "UPDATE " + tablePrefix + "Referrers " +
                                      "SET ReferralDay = @ReferralDay, " +
                                      "ReferrerUrl = @ReferrerUrl, " +
                                      "ReferralCount = @ReferralCount, " +
                                      "Url = @Url, " +
                                      "IsSpam = @IsSpam " +
                                      "WHERE ReferrerId = @ReferrerId";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    addReferrersParametersToCommand(referrer, provider, cmd);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets all Referrers from the database.
        /// </summary>
        /// <returns>List of Referrers.</returns>
        public override List<Referrer> FillReferrers()
        {
            deleteOldReferrers();

            List<Referrer> referrers = new List<Referrer>();

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT ReferrerId, ReferralDay, ReferrerUrl, ReferralCount, Url, IsSpam " +
                        "FROM " + tablePrefix + "Referrers ";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Referrer refer = new Referrer()
                                {
                                    Id = rdr.GetGuid(0),
                                    Day = rdr.GetDateTime(1),
                                    ReferrerUrl = new Uri(rdr.GetString(2)),
                                    Count = rdr.GetInt32(3),
                                    Url = rdr.IsDBNull(4) ? null : new Uri(rdr.GetString(4)),
                                    PossibleSpam = rdr.IsDBNull(5) ? false : rdr.GetBoolean(5)
                                };

                                referrers.Add(refer);
                                refer.MarkOld();
                            }
                        }
                    }
                }
            }

            return referrers;
        }

        private void deleteOldReferrers()
        {
            DateTime cutoff = DateTime.Today.AddDays(-BlogSettings.Instance.NumberOfReferrerDays);

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "Referrers " +
                                      "WHERE ReferralDay < " + parmPrefix + "ReferralDay";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpDay = provider.CreateParameter();
                    dpDay.ParameterName = parmPrefix + "ReferralDay";
                    dpDay.Value = cutoff;
                    cmd.Parameters.Add(dpDay);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the settings from the database
        /// </summary>
        /// <returns>dictionary of settings</returns>
        public override StringDictionary LoadSettings()
        {
            StringDictionary dic = new StringDictionary();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT SettingName, SettingValue FROM " + tablePrefix + "Settings";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string name = rdr.GetString(0);
                            string value = rdr.GetString(1);

                            dic.Add(name, value);
                        }
                    }
                }
            }

            return dic;
        }

        /// <summary>
        /// Saves the settings to the database
        /// </summary>
        /// <param name="settings">dictionary of settings</param>
        public override void SaveSettings(StringDictionary settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "Settings";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    cmd.ExecuteNonQuery();

                    foreach (string key in settings.Keys)
                    {
                        sqlQuery = "INSERT INTO " + tablePrefix + "Settings (SettingName, SettingValue) " +
                                   "VALUES (" + parmPrefix + "name, " + parmPrefix + "value)";
                        cmd.CommandText = sqlQuery;
                        cmd.Parameters.Clear();

                        DbParameter dpName = provider.CreateParameter();
                        dpName.ParameterName = parmPrefix + "name";
                        dpName.Value = key;
                        cmd.Parameters.Add(dpName);

                        DbParameter dpValue = provider.CreateParameter();
                        dpValue.ParameterName = parmPrefix + "value";
                        dpValue.Value = settings[key];
                        cmd.Parameters.Add(dpValue);

                        cmd.ExecuteNonQuery();
                    }

                }
            }
        }

        /// <summary>
        /// Gets the PingServices from the database
        /// </summary>
        /// <returns>collection of PingServices</returns>
        public override StringCollection LoadPingServices()
        {
            StringCollection col = new StringCollection();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT Link FROM " + tablePrefix + "PingService";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    
                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!col.Contains(rdr.GetString(0)))
                                col.Add(rdr.GetString(0));
                        }
                    }
                }
            }

            return col;
        }

        /// <summary>
        /// Saves the PingServices to the database
        /// </summary>
        /// <param name="services">collection of PingServices</param>
        public override void SavePingServices(StringCollection services)
        {
            if (services == null)
                throw new ArgumentNullException("services");

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "PingService";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    cmd.ExecuteNonQuery();

                    foreach (string service in services)
                    {
                        sqlQuery = "INSERT INTO " + tablePrefix + "PingService (Link) " +
                                    "VALUES (" + parmPrefix + "link)";
                        cmd.CommandText = sqlQuery;
                        cmd.Parameters.Clear();

                        DbParameter dpLink = provider.CreateParameter();
                        dpLink.ParameterName = parmPrefix + "link";
                        dpLink.Value = service;
                        cmd.Parameters.Add(dpLink);

                        cmd.ExecuteNonQuery();
                    }

                }
            }
        }

        /// <summary>
        /// Get stopwords from the database
        /// </summary>
        /// <returns>collection of stopwords</returns>
        public override StringCollection LoadStopWords()
        {
            StringCollection col = new StringCollection();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT StopWord FROM " + tablePrefix + "StopWords";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!col.Contains(rdr.GetString(0)))
                                col.Add(rdr.GetString(0));
                        }
                    }
                }
            }

            return col;
        }

        /// <summary>
        /// Load user data from DataStore
        /// </summary>
        /// <param name="exType">type of info</param>
        /// <param name="exId">id of info</param>
        /// <returns>stream of detail data</returns>
        public override object LoadFromDataStore(ExtensionType exType, string exId)
        {
            //MemoryStream stream;
            object o = null;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT Settings FROM " + tablePrefix + "DataStoreSettings " +
                                        "WHERE ExtensionType = " + parmPrefix + "etype AND ExtensionId = " + parmPrefix + "eid";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    DbParameter dpeType = provider.CreateParameter();
                    dpeType.ParameterName = parmPrefix + "etype";
                    dpeType.Value = exType.GetHashCode();
                    cmd.Parameters.Add(dpeType);
                    DbParameter dpeId = provider.CreateParameter();
                    dpeId.ParameterName = parmPrefix + "eid";
                    dpeId.Value = exId;
                    cmd.Parameters.Add(dpeId);

                    o = cmd.ExecuteScalar();
                }
            }
            return o;
        }

        /// <summary>
        /// Save to DataStore
        /// </summary>
        /// <param name="exType">type of info</param>
        /// <param name="exId">id of info</param>
        /// <param name="settings">data of info</param>
        public override void SaveToDataStore(ExtensionType exType, string exId, object settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            // Save
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            XmlSerializer xs = new XmlSerializer(settings.GetType());
            string objectXML = string.Empty;
            using (StringWriter sw = new StringWriter())
            {
              xs.Serialize(sw, settings);
              objectXML = sw.ToString();
            }
            
            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "DataStoreSettings " +
                                      "WHERE ExtensionType = @type AND ExtensionId = @id; ";

                    
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "type";
                    dpID.Value = exType.GetHashCode();
                    cmd.Parameters.Add(dpID);
                    DbParameter dpType = provider.CreateParameter();
                    dpType.ParameterName = parmPrefix + "id";
                    dpType.Value = exId;
                    cmd.Parameters.Add(dpType);

                    cmd.ExecuteNonQuery();

                    sqlQuery = "INSERT INTO " + tablePrefix + "DataStoreSettings " +
                        "(ExtensionType, ExtensionId, Settings) " +
                        "VALUES (@type, @id, @file)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpFile = provider.CreateParameter();
                    dpFile.ParameterName = parmPrefix + "file";
                    dpFile.Value = objectXML; // settings.ToString(); // file;
                    cmd.Parameters.Add(dpFile);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes an item from the dataStore
        /// </summary>
        /// <param name="exType">type of item</param>
        /// <param name="exId">id of item</param>
        public override void RemoveFromDataStore(ExtensionType exType, string exId)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "DataStoreSettings " +
                        "WHERE ExtensionType = " + parmPrefix + "type AND ExtensionId = " + parmPrefix + "id";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpID = provider.CreateParameter();
                    dpID.ParameterName = parmPrefix + "type";
                    dpID.Value = exType;
                    cmd.Parameters.Add(dpID);
                    DbParameter dpType = provider.CreateParameter();
                    dpType.ParameterName = parmPrefix + "id";
                    dpType.Value = exId;
                    cmd.Parameters.Add(dpType);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Storage location on web server
        /// </summary>
        /// <returns>string with virtual path to storage</returns>
        public override string StorageLocation()
        {
            if (String.IsNullOrEmpty(System.Web.Configuration.WebConfigurationManager.AppSettings["StorageLocation"]))
                return @"~/app_data/";
            return System.Web.Configuration.WebConfigurationManager.AppSettings["StorageLocation"];
        }

        private void UpdateTags(Post post, DbConnection conn, DbProviderFactory provider)
        {
            string sqlQuery = "DELETE FROM " + tablePrefix + "PostTag WHERE PostID = " + parmPrefix + "id";
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sqlQuery;
                cmd.CommandType = CommandType.Text;
                DbParameter dpID = provider.CreateParameter();
                dpID.ParameterName = parmPrefix + "id";
                dpID.Value = post.Id.ToString();
                cmd.Parameters.Add(dpID);
                cmd.ExecuteNonQuery();

                foreach (string tag in post.Tags)
                {
                    cmd.CommandText = "INSERT INTO " + tablePrefix + "PostTag (PostID, Tag) " +
                        "VALUES (" + parmPrefix + "id, " + parmPrefix + "tag)";
                    cmd.Parameters.Clear();
                    DbParameter dpPostID = provider.CreateParameter();
                    dpPostID.ParameterName = parmPrefix + "id";
                    dpPostID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpPostID);
                    DbParameter dpTag = provider.CreateParameter();
                    dpTag.ParameterName = parmPrefix + "tag";
                    dpTag.Value = tag;
                    cmd.Parameters.Add(dpTag);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateCategories(Post post, DbConnection conn, DbProviderFactory provider)
        {
            string sqlQuery = "DELETE FROM " + tablePrefix + "PostCategory WHERE PostID = " + parmPrefix + "id";
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sqlQuery;
                cmd.CommandType = CommandType.Text;
                DbParameter dpID = provider.CreateParameter();
                dpID.ParameterName = parmPrefix + "id";
                dpID.Value = post.Id.ToString();
                cmd.Parameters.Add(dpID);
                cmd.ExecuteNonQuery();

                foreach (Category cat in post.Categories)
                {
                    cmd.CommandText = "INSERT INTO " + tablePrefix + "PostCategory (PostID, CategoryID) " +
                        "VALUES (" + parmPrefix + "id, " + parmPrefix + "cat)";
                    cmd.Parameters.Clear();
                    DbParameter dpPostID = provider.CreateParameter();
                    dpPostID.ParameterName = parmPrefix + "id";
                    dpPostID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpPostID);
                    DbParameter dpCat = provider.CreateParameter();
                    dpCat.ParameterName = parmPrefix + "cat";
                    dpCat.Value = cat.Id.ToString();
                    cmd.Parameters.Add(dpCat);

                    cmd.ExecuteNonQuery();
                }
            }

        }

        private void UpdateComments(Post post, DbConnection conn, DbProviderFactory provider)
        {
            string sqlQuery = "DELETE FROM " + tablePrefix + "PostComment WHERE PostID = " + parmPrefix + "id";
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sqlQuery;
                cmd.CommandType = CommandType.Text;
                DbParameter dpID = provider.CreateParameter();
                dpID.ParameterName = parmPrefix + "id";
                dpID.Value = post.Id.ToString();
                cmd.Parameters.Add(dpID);
                cmd.ExecuteNonQuery();

                foreach (Comment comment in post.Comments)
                {
					sqlQuery = "INSERT INTO " + tablePrefix + "PostComment (PostCommentID, ParentCommentID, PostID, CommentDate, Author, Email, Website, Comment, Country, Ip, IsApproved, ModeratedBy, Avatar) " +
                                        "VALUES (@postcommentid, @parentid, @id, @date, @author, @email, @website, @comment, @country, @ip, @isapproved, @moderatedby, @avatar)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.Parameters.Clear();
                    DbParameter dpCommentID = provider.CreateParameter();
                    dpCommentID.ParameterName = parmPrefix + "postcommentid";
                    dpCommentID.Value = comment.Id.ToString();
                    cmd.Parameters.Add(dpCommentID);
					
					DbParameter dpParentID = provider.CreateParameter();
					dpParentID.ParameterName = parmPrefix + "parentid";
					dpParentID.Value = comment.ParentId.ToString();
					cmd.Parameters.Add(dpParentID);

					DbParameter dpPostID = provider.CreateParameter();
                    dpPostID.ParameterName = parmPrefix + "id";
                    dpPostID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpPostID);

                    DbParameter dpCommentDate = provider.CreateParameter();
                    dpCommentDate.ParameterName = parmPrefix + "date";
                    dpCommentDate.Value = comment.DateCreated.AddHours(-BlogSettings.Instance.Timezone);
                    cmd.Parameters.Add(dpCommentDate);

                    DbParameter dpAuthor = provider.CreateParameter();
                    dpAuthor.ParameterName = parmPrefix + "author";
                    dpAuthor.Value = comment.Author;
                    cmd.Parameters.Add(dpAuthor);

                    DbParameter dpEmail = provider.CreateParameter();
                    dpEmail.ParameterName = parmPrefix + "email";
                    dpEmail.Value = comment.Email ?? "";
                    cmd.Parameters.Add(dpEmail);

                    DbParameter dpWebsite = provider.CreateParameter();
                    dpWebsite.ParameterName = parmPrefix + "website";
                    if (comment.Website == null)
                        dpWebsite.Value = string.Empty;
                    else
                        dpWebsite.Value = comment.Website.ToString();
                    cmd.Parameters.Add(dpWebsite);

                    DbParameter dpContent = provider.CreateParameter();
                    dpContent.ParameterName = parmPrefix + "comment";
                    dpContent.Value = comment.Content;
                    cmd.Parameters.Add(dpContent);

                    DbParameter dpCountry = provider.CreateParameter();
                    dpCountry.ParameterName = parmPrefix + "country";
                    dpCountry.Value = comment.Country ?? string.Empty;
                    cmd.Parameters.Add(dpCountry);

                    DbParameter dpIP = provider.CreateParameter();
                    dpIP.ParameterName = parmPrefix + "ip";
                    dpIP.Value = comment.IP ?? string.Empty;
                    cmd.Parameters.Add(dpIP);

                    DbParameter dpIsApproved = provider.CreateParameter();
                    dpIsApproved.ParameterName = parmPrefix + "isapproved";
                    dpIsApproved.Value = comment.IsApproved;
                    cmd.Parameters.Add(dpIsApproved);

                    DbParameter dpModeratedBy = provider.CreateParameter();
                    dpModeratedBy.ParameterName = parmPrefix + "moderatedby";
                    dpModeratedBy.Value = comment.ModeratedBy ?? string.Empty;
                    cmd.Parameters.Add(dpModeratedBy);

                    DbParameter dpAvatar = provider.CreateParameter();
                    dpAvatar.ParameterName = parmPrefix + "avatar";
                    dpAvatar.Value = comment.Avatar ?? string.Empty;
                    cmd.Parameters.Add(dpAvatar);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateNotify(Post post, DbConnection conn, DbProviderFactory provider)
        {
            string sqlQuery = "DELETE FROM " + tablePrefix + "PostNotify WHERE PostID = " + parmPrefix + "id";
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sqlQuery;
                cmd.CommandType = CommandType.Text;
                DbParameter dpID = provider.CreateParameter();
                dpID.ParameterName = parmPrefix + "id";
                dpID.Value = post.Id.ToString();
                cmd.Parameters.Add(dpID);
                cmd.ExecuteNonQuery();

                foreach (string email in post.NotificationEmails)
                {
                    cmd.CommandText = "INSERT INTO " + tablePrefix + "PostNotify (PostID, NotifyAddress) " +
                        "VALUES (" + parmPrefix + "id, " + parmPrefix + "notify)";
                    cmd.Parameters.Clear();
                    DbParameter dpPostID = provider.CreateParameter();
                    dpPostID.ParameterName = parmPrefix + "id";
                    dpPostID.Value = post.Id.ToString();
                    cmd.Parameters.Add(dpPostID);
                    DbParameter dpNotify = provider.CreateParameter();
                    dpNotify.ParameterName = parmPrefix + "notify";
                    dpNotify.Value = email;
                    cmd.Parameters.Add(dpNotify);

                    cmd.ExecuteNonQuery();
                }
            }
				}

        /// <summary>
        /// Loads AuthorProfile from database
        /// </summary>
        /// <param name="id">username</param>
        /// <returns></returns>
		public override AuthorProfile SelectProfile(string id)
		{
            StringDictionary dic = new StringDictionary();
            AuthorProfile profile = new AuthorProfile(id);

            // Retrieve Profile data from Db
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT SettingName, SettingValue FROM " + tablePrefix + "Profiles " +
                                        "WHERE UserName = " + parmPrefix + "name";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = id;
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            dic.Add(rdr.GetString(0), rdr.GetString(1));
                        }
                    }
                }
            }

            // Load profile with data from dictionary
            if (dic.ContainsKey("DisplayName"))
                profile.DisplayName = dic["DisplayName"];
            if (dic.ContainsKey("FirstName"))
                profile.FirstName = dic["FirstName"];
            if (dic.ContainsKey("MiddleName"))
                profile.MiddleName = dic["MiddleName"];
            if (dic.ContainsKey("LastName"))
                profile.LastName = dic["LastName"];
            if (dic.ContainsKey("CityTown"))
                profile.CityTown = dic["CityTown"];
            if (dic.ContainsKey("RegionState"))
                profile.RegionState = dic["RegionState"];
            if (dic.ContainsKey("Country"))
                profile.Country = dic["Country"];
            if (dic.ContainsKey("Birthday"))
            {
                DateTime date;
                if (DateTime.TryParse(dic["Birthday"], out date))
                    profile.Birthday = date;
            }
            if (dic.ContainsKey("AboutMe"))
                profile.AboutMe = dic["AboutMe"];
            if (dic.ContainsKey("PhotoURL"))
                profile.PhotoURL = dic["PhotoURL"];
            if (dic.ContainsKey("Company"))
                profile.Company = dic["Company"];
            if (dic.ContainsKey("EmailAddress"))
                profile.EmailAddress = dic["EmailAddress"];
            if (dic.ContainsKey("PhoneMain"))
                profile.PhoneMain = dic["PhoneMain"];
            if (dic.ContainsKey("PhoneMobile"))
                profile.PhoneMobile = dic["PhoneMobile"];
            if (dic.ContainsKey("PhoneFax"))
                profile.PhoneFax = dic["PhoneFax"];
            if (dic.ContainsKey("IsPrivate"))
                profile.IsPrivate = dic["IsPrivate"] == "true";

		    return profile;
		}

        /// <summary>
        /// Adds AuthorProfile to database
        /// </summary>
        /// <param name="profile"></param>
		public override void InsertProfile(AuthorProfile profile)
		{
			UpdateProfile(profile);
		}

        /// <summary>
        /// Updates AuthorProfile to database
        /// </summary>
        /// <param name="profile"></param>
		public override void UpdateProfile(AuthorProfile profile)
		{
			// Remove Profile
            DeleteProfile(profile);

            // Create Profile Dictionary
            StringDictionary dic = new StringDictionary();

            if (!String.IsNullOrEmpty(profile.DisplayName))
                dic.Add("DisplayName", profile.DisplayName);
            if (!String.IsNullOrEmpty(profile.FirstName))
                dic.Add("FirstName", profile.FirstName);
            if (!String.IsNullOrEmpty(profile.MiddleName))
                dic.Add("MiddleName", profile.MiddleName);
            if (!String.IsNullOrEmpty(profile.LastName))
                dic.Add("LastName", profile.LastName);
            if (!String.IsNullOrEmpty(profile.CityTown))
                dic.Add("CityTown", profile.CityTown);
            if (!String.IsNullOrEmpty(profile.RegionState))
                dic.Add("RegionState", profile.RegionState);
            if (!String.IsNullOrEmpty(profile.Country))
                dic.Add("Country", profile.Country);
            if (!String.IsNullOrEmpty(profile.AboutMe))
                dic.Add("AboutMe", profile.AboutMe);
            if (!String.IsNullOrEmpty(profile.PhotoURL))
                dic.Add("PhotoURL", profile.PhotoURL);
            if (!String.IsNullOrEmpty(profile.Company))
                dic.Add("Company", profile.Company);
            if (!String.IsNullOrEmpty(profile.EmailAddress))
                dic.Add("EmailAddress", profile.EmailAddress);
            if (!String.IsNullOrEmpty(profile.PhoneMain))
                dic.Add("PhoneMain", profile.PhoneMain);
            if (!String.IsNullOrEmpty(profile.PhoneMobile))
                dic.Add("PhoneMobile", profile.PhoneMobile);
            if (!String.IsNullOrEmpty(profile.PhoneFax))
                dic.Add("PhoneFax", profile.PhoneFax);
            if (profile.Birthday != DateTime.MinValue)
                dic.Add("Birthday", profile.Birthday.ToString("yyyy-MM-dd"));
            
            dic.Add("IsPrivate", profile.IsPrivate.ToString());
            
            // Save Profile Dictionary
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();

                using (DbCommand cmd = conn.CreateCommand())
                {
                    foreach (string key in dic.Keys)
                    {
                        string sqlQuery = "INSERT INTO " + tablePrefix + "Profiles (UserName, SettingName, SettingValue) " +
                                          "VALUES (@user, @name, @value)";
                        if (parmPrefix != "@")
                            sqlQuery = sqlQuery.Replace("@", parmPrefix);
                        cmd.CommandText = sqlQuery;
                        cmd.Parameters.Clear();

                        DbParameter dpUser = provider.CreateParameter();
                        dpUser.ParameterName = parmPrefix + "user";
                        dpUser.Value = profile.Id;
                        cmd.Parameters.Add(dpUser);

                        DbParameter dpName = provider.CreateParameter();
                        dpName.ParameterName = parmPrefix + "name";
                        dpName.Value = key;
                        cmd.Parameters.Add(dpName);

                        DbParameter dpValue = provider.CreateParameter();
                        dpValue.ParameterName = parmPrefix + "value";
                        dpValue.Value = dic[key];
                        cmd.Parameters.Add(dpValue);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
		}

        /// <summary>
        /// Remove AuthorProfile from database
        /// </summary>
        /// <param name="profile"></param>
		public override void DeleteProfile(AuthorProfile profile)
		{
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "DELETE FROM " + tablePrefix + "Profiles " +
                                      "WHERE UserName = " + parmPrefix + "name";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = profile.Id;
                    cmd.Parameters.Add(dpName);

                    cmd.ExecuteNonQuery();
                }
            }
		}

        /// <summary>
        /// Return collection for AuthorProfiles from database
        /// </summary>
        /// <returns></returns>
		public override List<AuthorProfile> FillProfiles()
		{
            List<AuthorProfile> profiles = new List<AuthorProfile>();
            List<string> profileNames = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT UserName FROM " + tablePrefix + "Profiles " +
                                      "GROUP BY UserName";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            profileNames.Add(rdr.GetString(0));
                        }
                    }
                }
            }

		    foreach (string name in profileNames)
		    {
		        profiles.Add(BusinessBase<AuthorProfile, string>.Load(name));
		    }

		    return profiles;
		}
    }
}
