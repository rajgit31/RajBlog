using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Data.Common;
using System.Web.Security;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Generic Db Role Provider
    /// </summary>
    public class DbRoleProvider : RoleProvider
    {
        private string connStringName;
        private string tablePrefix;
        private string parmPrefix;
        private string applicationName;

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

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Generic Database Membership Provider");
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

            if (config["applicationName"] == null)
            {
                // default to BlogEngine
                config["applicationName"] = "BlogEngine";
            }
            applicationName = config["applicationName"];
            config.Remove("applicationName");

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }
        }

        /// <summary>
        /// Check to see if user is in a role
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public override bool IsUserInRole(string username, string roleName)
        {
            bool roleFound = false;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT r.roleID " +
                                        "FROM " + tablePrefix + "Roles r " +
                                        "INNER JOIN " + tablePrefix + "UserRoles ur " +
                                        "ON r.RoleID = ur.RoleID " +
                                        "INNER JOIN " + tablePrefix + "Users u " +
                                        "ON ur.UserID = u.UserID " +
                                        "WHERE u.UserName = @name AND r.role = @role";
                    if (parmPrefix != "@")
                        sqlQuery = sqlQuery.Replace("@", parmPrefix);
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);
                    DbParameter dpRole = provider.CreateParameter();
                    dpRole.ParameterName = parmPrefix + "role";
                    dpRole.Value = roleName;
                    cmd.Parameters.Add(dpRole);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                            roleFound = true;
                    }
                }
            }
            return roleFound;
        }

        /// <summary>
        /// Return an array of roles that user is in
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public override string[] GetRolesForUser(string username)
        {
            List<string> roles = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT r.role " +
                                        "FROM " + tablePrefix + "Roles r " +
                                        "INNER JOIN " + tablePrefix + "UserRoles ur " +
                                        "ON r.RoleID = ur.RoleID " +
                                        "INNER JOIN " + tablePrefix + "Users u " +
                                        "ON ur.UserID = u.UserID " +
                                        "WHERE u.UserName = " + parmPrefix + "name";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = username;
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                roles.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return roles.ToArray();
        }

        /// <summary>
        /// Adds a new role to the database
        /// </summary>
        /// <param name="roleName"></param>
        public override void CreateRole(string roleName)
        {
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "INSERT INTO " + tablePrefix + "Roles (role) VALUES (" + parmPrefix + "role)";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpRole = provider.CreateParameter();
                    dpRole.ParameterName = parmPrefix + "role";
                    dpRole.Value = roleName;
                    cmd.Parameters.Add(dpRole);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Removes a role from database
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="throwOnPopulatedRole"></param>
        /// <returns></returns>
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            bool success = false;
            if (roleName != "Administrators")
            {
                string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
                string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
                DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

                using (DbConnection conn = provider.CreateConnection())
                {
                    conn.ConnectionString = connString;

                    using (DbCommand cmd = conn.CreateCommand())
                    {
                        string sqlQuery = "DELETE FROM " + tablePrefix + "Roles WHERE Role = " + parmPrefix + "role";
                        cmd.CommandText = sqlQuery;
                        cmd.CommandType = CommandType.Text;

                        conn.Open();

                        DbParameter dpRole = provider.CreateParameter();
                        dpRole.ParameterName = parmPrefix + "role";
                        dpRole.Value = roleName;
                        cmd.Parameters.Add(dpRole);

                        cmd.ExecuteNonQuery();

                        success = true;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Checks to see if role exists
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public override bool RoleExists(string roleName)
        {
            bool roleFound = false;
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT roleID FROM " + tablePrefix + "Roles " +
                                        "WHERE role = " + parmPrefix + "role";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpRole = provider.CreateParameter();
                    dpRole.ParameterName = parmPrefix + "role";
                    dpRole.Value = roleName;
                    cmd.Parameters.Add(dpRole);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                            roleFound = true;
                    }
                }
            }
            return roleFound;
        }

        /// <summary>
        /// Adds all users in user array to all roles in role array
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
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
                    cmd.CommandType = CommandType.Text;
                    foreach (string user in usernames)
                    {
                        cmd.CommandText = "SELECT UserID FROM " + tablePrefix + "Users " +
                                            "WHERE UserName = " + parmPrefix + "user";
                        DbParameter dpUser = provider.CreateParameter();
                        dpUser.ParameterName = parmPrefix + "user";
                        dpUser.Value = user;
                        cmd.Parameters.Add(dpUser);

                        int userID = Int32.Parse(cmd.ExecuteScalar().ToString());

                        foreach (string role in roleNames)
                        {
                            cmd.CommandText = "SELECT RoleID FROM " + tablePrefix + "Roles " +
                                            "WHERE Role = " + parmPrefix + "role";
                            DbParameter dpRole = provider.CreateParameter();
                            dpRole.ParameterName = parmPrefix + "role";
                            dpRole.Value = role;
                            cmd.Parameters.Add(dpRole);

                            int roleID = Int32.Parse(cmd.ExecuteScalar().ToString());

                            cmd.CommandText = "INSERT INTO " + tablePrefix + "UserRoles (UserID, RoleID) " +
                                                "VALUES (" + parmPrefix + "uID, " + parmPrefix + "rID)";
                            DbParameter dpUserID = provider.CreateParameter();
                            dpUserID.ParameterName = parmPrefix + "uID";
                            dpUserID.Value = userID;
                            cmd.Parameters.Add(dpUserID);
                            DbParameter dpRoleID = provider.CreateParameter();
                            dpRoleID.ParameterName = parmPrefix + "rID";
                            dpRoleID.Value = roleID;
                            cmd.Parameters.Add(dpRoleID);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes all users in user array from all roles in role array
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
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
                    cmd.CommandType = CommandType.Text;
                    foreach (string user in usernames)
                    {
                        cmd.CommandText = "SELECT UserID FROM " + tablePrefix + "Users " +
                                            "WHERE UserName = " + parmPrefix + "user";
                        DbParameter dpUser = provider.CreateParameter();
                        dpUser.ParameterName = parmPrefix + "user";
                        dpUser.Value = user;
                        cmd.Parameters.Add(dpUser);
                        int userID;
                        try
                        {
                            userID = Int32.Parse(cmd.ExecuteScalar().ToString());
                        }
                        catch
                        {
                            userID = 0;
                        }

                        if (userID > 0)
                        {
                            foreach (string role in roleNames)
                            {
                                cmd.CommandText = "SELECT RoleID FROM " + tablePrefix + "Roles " +
                                                  "WHERE Role = " + parmPrefix + "role";
                                DbParameter dpRole = provider.CreateParameter();
                                dpRole.ParameterName = parmPrefix + "role";
                                dpRole.Value = role;
                                cmd.Parameters.Add(dpRole);

                                int roleID = Int32.Parse(cmd.ExecuteScalar().ToString());

                                cmd.CommandText = "DELETE FROM " + tablePrefix + "UserRoles " +
                                                  "WHERE UserID = " + parmPrefix + "uID AND RoleID = " + parmPrefix +
                                                  "rID";
                                DbParameter dpUserID = provider.CreateParameter();
                                dpUserID.ParameterName = parmPrefix + "uID";
                                dpUserID.Value = userID;
                                cmd.Parameters.Add(dpUserID);
                                DbParameter dpRoleID = provider.CreateParameter();
                                dpRoleID.ParameterName = parmPrefix + "rID";
                                dpRoleID.Value = roleID;
                                cmd.Parameters.Add(dpRoleID);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns array of users in selected role
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public override string[] GetUsersInRole(string roleName)
        {
            List<string> users = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT u.UserName " +
                                        "FROM " + tablePrefix + "Users u " +
                                        "INNER JOIN " + tablePrefix + "UserRoles ur " +
                                        "ON u.UserID = ur.UserID " +
                                        "INNER JOIN " + tablePrefix + "Roles r " +
                                        "ON ur.RoleID = r.RoleID " +
                                        "WHERE r.Role  = " + parmPrefix + "role";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpRole = provider.CreateParameter();
                    dpRole.ParameterName = parmPrefix + "role";
                    dpRole.Value = roleName;
                    cmd.Parameters.Add(dpRole);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                users.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return users.ToArray();
        }

        /// <summary>
        /// Returns array of all roles in database
        /// </summary>
        /// <returns></returns>
        public override string[] GetAllRoles()
        {
            List<string> roles = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT role FROM " + tablePrefix + "Roles";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                roles.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return roles.ToArray();
        }

        /// <summary>
        /// Returns all users in selected role with names that match usernameToMatch
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="usernameToMatch"></param>
        /// <returns></returns>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            List<string> users = new List<string>();
            string connString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            string providerName = ConfigurationManager.ConnectionStrings[connStringName].ProviderName;
            DbProviderFactory provider = DbProviderFactories.GetFactory(providerName);

            using (DbConnection conn = provider.CreateConnection())
            {
                conn.ConnectionString = connString;

                using (DbCommand cmd = conn.CreateCommand())
                {
                    string sqlQuery = "SELECT u.UserName " +
                                        "FROM " + tablePrefix + "Users u " +
                                        "INNER JOIN " + tablePrefix + "UserRoles ur " +
                                        "ON u.UserID = ur.UserID " +
                                        "INNER JOIN " + tablePrefix + "Roles r " +
                                        "ON ur.RoleID = r.RoleID " +
                                        "WHERE r.Role  = " + parmPrefix + "role AND u.UserName LIKE " + parmPrefix + "name";
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;

                    conn.Open();

                    DbParameter dpRole = provider.CreateParameter();
                    dpRole.ParameterName = parmPrefix + "role";
                    dpRole.Value = roleName;
                    cmd.Parameters.Add(dpRole);
                    DbParameter dpName = provider.CreateParameter();
                    dpName.ParameterName = parmPrefix + "name";
                    dpName.Value = usernameToMatch + "%";
                    cmd.Parameters.Add(dpName);

                    using (DbDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                                users.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return users.ToArray();
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
    }
}
