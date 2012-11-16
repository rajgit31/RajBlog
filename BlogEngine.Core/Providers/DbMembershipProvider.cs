using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Data.Common;
using System.Web.Security;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Generic Db Membership Provider
    /// </summary>
    public class DbMembershipProvider : MembershipProvider
    {
        private string connStringName;
        private string tablePrefix;
        private string parmPrefix;
        private string applicationName;
        private MembershipPasswordFormat passwordFormat;

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
                name = "DbMembershipProvider";
            }

            if (Type.GetType("Mono.Runtime") != null)
            {
                // Mono dies with a "Unrecognized attribute: description" if a description is part of the config.
                if (!string.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "Generic Database Membership Provider");
                }
            }

            base.Initialize(name, config);

            // Connection String
            if (config["connectionStringName"] == null)
            {
                config["connectionStringName"] = "BlogEngine";
            }
            connStringName = config["connectionStringName"];
            config.Remove("connectionStringName");

            // Table Prefix
            if (config["tablePrefix"] == null)
            {
                config["tablePrefix"] = "be_";
            }
            tablePrefix = config["tablePrefix"];
            config.Remove("tablePrefix");

            // Parameter character
            if (config["parmPrefix"] == null)
            {
                config["parmPrefix"] = "@";
            }
            parmPrefix = config["parmPrefix"];
            config.Remove("parmPrefix");

            // Application Name
            if (config["applicationName"] == null)
            {
                config["applicationName"] = "BlogEngine";
            }
            applicationName = config["applicationName"];
            config.Remove("applicationName");

            //Password Format
            if (config["passwordFormat"] == null)
            {
                config["passwordFormat"] = "Hashed";
                passwordFormat = MembershipPasswordFormat.Hashed;
            }
            else if (String.Compare(config["passwordFormat"], "clear", true) == 0)
            {
                passwordFormat = MembershipPasswordFormat.Clear;
            }
            else
            {
                passwordFormat = MembershipPasswordFormat.Hashed;
            }
            config.Remove("passwordFormat");

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }
        }

        /// <summary>
        /// Add new user to database
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="passwordQuestion"></param>
        /// <param name="passwordAnswer"></param>
        /// <param name="isApproved"></param>
        /// <param name="providerUserKey"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public override MembershipUser CreateUser(string username, string password, string email,
                                                  string passwordQuestion, string passwordAnswer, bool isApproved,
                                                  object providerUserKey, out MembershipCreateStatus status)
        {
            MembershipUser user;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "Users (userName, password, emailAddress, lastLoginTime) VALUES (@name, @pwd, @email, @login)";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);
                    DbParameter dpPwd = provider.CreateParameter();
                    dpPwd.ParameterName = parmPrefix + "pwd";
                    if (passwordFormat == MembershipPasswordFormat.Hashed)
                        dpPwd.Value = Utils.HashPassword(password);
                    else
                        dpPwd.Value = password;
                    cmd.Parameters.Add(dpPwd);
                    DbParameter dpEmail = provider.CreateParameter();
                    dpEmail.ParameterName = parmPrefix + "email";
                    dpEmail.Value = email;
                    cmd.Parameters.Add(dpEmail);
                    DbParameter dpLogin = provider.CreateParameter();
                    dpLogin.ParameterName = parmPrefix + "login";
                    dpLogin.Value = DateTime.Now;
                    cmd.Parameters.Add(dpLogin);

                    cmd.ExecuteNonQuery();
                }
            }
            user = GetMembershipUser(username, email, DateTime.Now);
            status = MembershipCreateStatus.Success;

            return user;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="newPasswordQuestion"></param>
        /// <param name="newPasswordAnswer"></param>
        /// <returns></returns>
        public override bool ChangePasswordQuestionAndAnswer(string username, string password,
                                                             string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="username"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Change the password if the old password matches what is stored
        /// </summary>
        /// <param name="username"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            bool oldPasswordCorrect = false;
            bool success = false;

            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    // Check Old Password
                    cmd.CommandText = "SELECT password FROM " + tablePrefix + "Users WHERE userName = " + parmPrefix + "name";
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();
                            string actualPassword = rdr.GetString(0);
                            if (actualPassword == "")
                            {
                                // This is a special case used for resetting.
                                if (oldPassword.ToLower() == "admin")
                                    oldPasswordCorrect = true;
                            }
                            else
                            {
                                if (passwordFormat == MembershipPasswordFormat.Hashed)
                                {
                                    if (actualPassword == Utils.HashPassword(oldPassword))
                                        oldPasswordCorrect = true;
                                }
                                else if (actualPassword == oldPassword)
                                    oldPasswordCorrect = true;

                            }
                        }
                    }

                    // Update New Password
                    if (oldPasswordCorrect)
                    {
                        cmd.CommandText = "UPDATE " + tablePrefix + "Users SET password = " + parmPrefix + "pwd WHERE userName = " + parmPrefix + "name";

                        DbParameter dpPwd = provider.CreateParameter();
                        dpPwd.ParameterName = parmPrefix + "pwd";
                        if (passwordFormat == MembershipPasswordFormat.Hashed)
                            dpPwd.Value = Utils.HashPassword(newPassword);
                        else 
                            dpPwd.Value = newPassword;
                        cmd.Parameters.Add(dpPwd);
                        
                        cmd.ExecuteNonQuery();
                        success = true;
                    }

                }
            }
            return success;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="username"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update User Data (not password)
        /// </summary>
        /// <param name="user"></param>
        public override void UpdateUser(MembershipUser user)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE " + tablePrefix + "Users SET emailAddress = " + parmPrefix + "email WHERE userName = " + parmPrefix + "name";
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = user.UserName;
                    cmd.Parameters.Add(dpName);
                    DbParameter dpEmail = provider.CreateParameter();
                    dpEmail.ParameterName = parmPrefix + "email";
                    dpEmail.Value = user.Email;
                    cmd.Parameters.Add(dpEmail);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Check username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override bool ValidateUser(string username, string password)
        {
            bool validated = false;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT password FROM " + tablePrefix + "Users " +
                                        "WHERE UserName = " + parmPrefix + "name";
                    cmd.CommandType = CommandType.Text;
                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();
                            string storedPwd = rdr.GetString(0);

                            if (storedPwd == "")
                            {
                                // This is a special case used for resetting.
                                if (password.ToLower() == "admin")
                                    validated = true;
                            }
                            else
                            {
                                if (passwordFormat == MembershipPasswordFormat.Hashed)
                                {
                                    if (storedPwd == Utils.HashPassword(password))
                                        validated = true;
                                }
                                else if (storedPwd == password)
                                    validated = true;

                            }
                        }
                    }
                }
            }

            return validated;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get User by providerUserKey
        /// </summary>
        /// <param name="providerUserKey"></param>
        /// <param name="userIsOnline"></param>
        /// <returns></returns>
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return GetUser(providerUserKey.ToString(), userIsOnline);
        }

        /// <summary>
        /// Get User by username
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userIsOnline"></param>
        /// <returns></returns>
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            MembershipUser user = null;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT username, EmailAddress, lastLoginTime " +
                                        "FROM " + tablePrefix + "Users " + 
                                        "WHERE UserName = " + parmPrefix + "name";
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();
                            user = GetMembershipUser(username, rdr.GetString(1), rdr.GetDateTime(2));
                        }
                    }
                }
            }
            return user;
        }

        /// <summary>
        /// Retrieve UserName for given email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override string GetUserNameByEmail(string email)
        {
            if (email == null)
                throw new ArgumentNullException("email");

            string userName = null;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT userName FROM " + tablePrefix + "Users " +
                                    "WHERE emailAddress = " + parmPrefix + "email";
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpEmail = provider.CreateParameter();
                    dpEmail.ParameterName = parmPrefix + "email";
                    dpEmail.Value = email;
                    cmd.Parameters.Add(dpEmail);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();
                            userName = rdr.GetString(0);
                        }

                    }
                }
            }

            return userName;
        }

        /// <summary>
        /// Delete user from database
        /// </summary>
        /// <param name="username"></param>
        /// <param name="deleteAllRelatedData"></param>
        /// <returns></returns>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            bool success;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM " + tablePrefix + "Users WHERE userName = " + parmPrefix + "name";
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        success = true;
                    }
                    catch (Exception)
                    {
                        success = false;
                    }
                }
            }
            
            return success;
        }

        /// <summary>
        /// Return all users in MembershipUserCollection
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection users = new MembershipUserCollection();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT username, EmailAddress, lastLoginTime " +
                                        "FROM " + tablePrefix + "Users";
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            users.Add(GetMembershipUser(rdr.GetString(0), rdr.GetString(1), rdr.GetDateTime(2)));
                        }
                    }
                }
            }
            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <returns></returns>
        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="usernameToMatch"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize,
                                                                 out int totalRecords)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="emailToMatch"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize,
                                                                  out int totalRecords)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Can password be retrieved via email?
        /// </summary>
        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }

        /// <summary>
        /// Hardcoded to false
        /// </summary>
        public override bool EnablePasswordReset
        {
            get { return false; }
        }

        /// <summary>
        /// Hardcoded to false
        /// </summary>
        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }

        /// <summary>
        /// Returns the application name as set in the web.config
        /// otherwise returns BlogEngine.  Set will throw an error.
        /// </summary>
        public override string ApplicationName
        {
            get { return applicationName; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Hardcoded to 5
        /// </summary>
        public override int MaxInvalidPasswordAttempts
        {
            get { return 5; }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Hardcoded to false
        /// </summary>
        public override bool RequiresUniqueEmail
        {
            get { return false; }
        }

        /// <summary>
        /// Password format (Clear or Hashed)
        /// </summary>
        public override MembershipPasswordFormat PasswordFormat
        {
            get { return passwordFormat; }
        }

        /// <summary>
        /// Hardcoded to 4
        /// </summary>
        public override int MinRequiredPasswordLength
        {
            get { return 4; }
        }

        /// <summary>
        /// Hardcoded to 0
        /// </summary>
        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return 0; }
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        private MembershipUser GetMembershipUser(string userName, string email, DateTime lastLogin)
        {
            MembershipUser user = new MembershipUser(
                                    Name,                       // Provider name
                                    userName,                   // Username
                                    userName,                   // providerUserKey
                                    email,                      // Email
                                    String.Empty,               // passwordQuestion
                                    String.Empty,               // Comment
                                    true,                       // isApproved
                                    false,                      // isLockedOut
                                    DateTime.Now,               // creationDate
                                    lastLogin,                  // lastLoginDate
                                    DateTime.Now,               // lastActivityDate
                                    DateTime.Now,               // lastPasswordChangedDate
                                    new DateTime(1980, 1, 1)    // lastLockoutDate
                                );
            return user;
        }
    }
}
