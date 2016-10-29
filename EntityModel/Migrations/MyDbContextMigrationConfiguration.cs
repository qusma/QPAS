// -----------------------------------------------------------------------
// <copyright file="MyDbContextConfiguration.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Configuration;
using System.Data.Entity.Migrations;
using System.Data.Entity.SqlServer;
using MySql.Data.Entity;

namespace EntityModel.Migrations
{
    public class MyDbContextMigrationConfiguration : DbMigrationsConfiguration<DBContext>
    {
        public MyDbContextMigrationConfiguration()
        {
            //The people who write the migration generators are imbeciles of the highest order
            //explicit migrations are impossible if we want to support both mysql and sql server
            //so we're forced to go with a combo of explicit + automatic instead. This is garbage.
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;

            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
        }
    }
}