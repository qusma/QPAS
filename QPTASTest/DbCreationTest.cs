// -----------------------------------------------------------------------
// <copyright file="DbCreationTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Reflection;
using EntityModel;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using MySql.Data.Entity;
using QPAS;

namespace qpasTest
{
    [TestFixture]
    public class DbCreationTest
    {
        private readonly string _mySqlPassword = "Password12!";
        private readonly string _mySqlUsername = "root";
        private readonly string _mySqlHost = "127.0.0.1";

        private readonly string _sqlServerPassword = "Password12!";
        private readonly string _sqlServerHost = "(local)\\SQL2016";
        private readonly string _sqlServerUsername = "sa";
        private readonly bool _useWindowsAuthentication = false;

        [Test]
        public void MySqlDbIsCreatedSuccessfully()
        {
            using (var conn = new MySqlConnection(GetMySqlConnString(_mySqlUsername, _mySqlPassword, _mySqlHost)))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("", conn))
                {
                    cmd.CommandText = @"DROP DATABASE IF EXISTS qpas_test;
                                        CREATE DATABASE qpas_test;";
                    cmd.ExecuteNonQuery();
                }
            }

            SetConnectionString("qpasEntities", GetMySqlConnString(_mySqlUsername, _mySqlPassword, _mySqlHost, "qpas_test"), "MySql.Data.MySqlClient");

            ConfigurationManager.RefreshSection("connectionStrings");

            DbConfiguration.SetConfiguration(new MySqlEFConfiguration());

            using (var ctx = new DBContext())
            {
                ctx.Database.Initialize(true);
                Seed.DoSeed();
            }
        }

        private static string GetMySqlConnString(string username, string password, string host, string db = null)
        {
            string connStr = string.Format("User Id={0};Password={1};Host={2};Persist Security Info=True;",
               username,
               password,
               host);

            if (!string.IsNullOrEmpty(db))
            {
                connStr += $"Database={db};";
            }

            connStr +=
                "allow user variables=true;" +
                "persist security info=true;" +
                "Convert Zero Datetime=True";

            return connStr;
        }

        [Test]
        public void SqlServerDbIsCreatedSuccessfully()
        {
            using (var conn = new SqlConnection(GetSqlServerConnString("master", _sqlServerHost, _sqlServerUsername, _sqlServerPassword, false, _useWindowsAuthentication)))
            {
                conn.Open();
                using (var cmd = new SqlCommand("", conn))
                {
                    cmd.CommandText = @"IF EXISTS(SELECT name FROM sys.databases WHERE name = 'qpas_test')
                                            DROP DATABASE qpas_test";
                    cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE DATABASE qpas_test";
                    cmd.ExecuteNonQuery();
                }
            }

            SetConnectionString("qpasEntities", GetSqlServerConnString("qpas_test", _sqlServerHost, _sqlServerUsername, _sqlServerPassword, false, _useWindowsAuthentication), "System.Data.SqlClient");

            ConfigurationManager.RefreshSection("connectionStrings");

            using (var ctx = new DBContext())
            {
                ctx.Database.Initialize(true);
                Seed.DoSeed();
            }
        }

        internal static string GetSqlServerConnString(string database, string server, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = $"Data Source={server};";

            if (!noDB)
            {
                connectionString += $"Initial Catalog={database};";
            }

            if (!useWindowsAuthentication) //user/pass authentication
            {
                connectionString += $"User ID={username};Password={password};";
            }
            else //windows authentication
            {
                connectionString += "Integrated Security=True;";
            }

            return connectionString;
        }

        private static void SetConnectionString(string connName, string connStr, string providerName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[connName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = connStr;
            conSettings.ProviderName = providerName;

            config.Save();
        }
    }
}
