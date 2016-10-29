// -----------------------------------------------------------------------
// <copyright file="DBUtils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using QPAS.Properties;

namespace QPAS
{
    public static class DBUtils
    {public static void SetConnectionString()
        {
            if (Settings.Default.databaseType == "MySql")
            {
                SetMySqlConnectionString("qpasEntities", "qpas");
            }
            else if (Settings.Default.databaseType == "SqlServer")
            {
                SetSqlServerConnectionString("qpasEntities", "qpas");
            }

            ConfigurationManager.RefreshSection("connectionStrings");
        }

        public static void SetDbConfiguration()
        {
            if (Settings.Default.databaseType == "MySql")
            {
                DbConfiguration.SetConfiguration(new MySqlEFConfiguration());
            }
        }

        private static void SetSqlServerConnectionString(string stringName, string dbName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[stringName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = GetSqlServerConnectionString(
                dbName,
                Settings.Default.sqlServerHost,
                Settings.Default.sqlServerUsername,
                Unprotect(Settings.Default.sqlServerPassword),
                useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication);
            conSettings.ProviderName = "System.Data.SqlClient";

            config.Save();
        }

        private static void SetMySqlConnectionString(string stringName, string dbName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[stringName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = string.Format("User Id={0};Password={1};Host={2};Database={3};Persist Security Info=True;Convert Zero Datetime=True",
                Settings.Default.mySqlUsername,
                Unprotect(Settings.Default.mySqlPassword),
                Settings.Default.mySqlHost,
                dbName);
            conSettings.ProviderName = "MySql.Data.MySqlClient";

            config.Save();
        }


        /// <summary>
        /// Used for decrypting db passwords.
        /// </summary>
        public static string Unprotect(string encryptedString)
        {
            byte[] buffer;
            try
            {
                buffer = ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                //if it's empty or incorrectly formatted, we get an exception. Just return an empty string.
                return "";
            }
            return Encoding.Unicode.GetString(buffer);
        }

        /// <summary>
        /// Used for encrypting db passwords.
        /// </summary>
        public static string Protect(string unprotectedString)
        {
            byte[] buffer = ProtectedData.Protect(Encoding.Unicode.GetBytes(unprotectedString), null, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(buffer);
        }

        public static SqlConnection CreateSqlServerConnection(string database = "qpas", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = GetSqlServerConnectionString(database, server, username, password, noDB, useWindowsAuthentication);
            return new SqlConnection(connectionString);
        }

        private static string GetSqlServerConnectionString(string database = "qpas", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = String.Format(
                "Data Source={0};",
                server ?? Settings.Default.sqlServerHost);

            if (!noDB)
            {
                connectionString += String.Format("Initial Catalog={0};", database);
            }

            if (!useWindowsAuthentication) //user/pass authentication
            {
                if (password == null)
                {
                    try
                    {
                        password = Unprotect(Settings.Default.sqlServerPassword);
                    }
                    catch
                    {
                        password = "";
                    }
                }
                connectionString += string.Format("User ID={0};Password={1};", username, password);
            }
            else //windows authentication
            {
                connectionString += "Integrated Security=True;";
            }
            return connectionString;
        }

        public static MySqlConnection CreateMySqlConnection(string database = "qpas", string server = null, string username = null, string password = null, bool noDB = false)
        {
            if (password == null)
            {
                try
                {
                    password = Unprotect(Settings.Default.mySqlPassword);
                }
                catch
                {
                    password = "";
                }
            }

            string connectionString = String.Format(
                "server={0};" +
                "user id={1};" +
                "Password={2};",
                server ?? Settings.Default.mySqlHost,
                username ?? Settings.Default.mySqlUsername,
                password
                );

            if (!noDB)
            {
                connectionString += String.Format("database={0};", database);
            }

            connectionString +=
                "allow user variables=true;" +
                "persist security info=true;" +
                "Convert Zero Datetime=True";

            return new MySqlConnection(connectionString);
        }

        public static void CheckDBConnection()
        {
            //if no db type has been selected, we gotta show that window no matter what
            if (Settings.Default.databaseType != "MySql" &&
                Settings.Default.databaseType != "SqlServer" &&
                Settings.Default.databaseType != "Sqlite")
            {
                var dbDetailsWindow = new DBPicker();
                dbDetailsWindow.ShowDialog();
            }

            //try to establish a database connection. If not possible, prompt the user to enter details
            if (Settings.Default.databaseType == "MySql") //MySQL/MariaDB
            {
                var connection = CreateMySqlConnection(noDB: true);

                connection.Open();
                connection.Close();
            }
            else if (Settings.Default.databaseType == "SqlServer") //SQL Server
            {
                var connection = CreateSqlServerConnection(noDB: true);
                connection.Open();
                connection.Close();
            }
        }
    }
}