// -----------------------------------------------------------------------
// <copyright file="DbCreationTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using QPAS;

namespace QPASTest
{
    [TestFixture]
    public class DbCreationTest
    {
        [Test]
        public void DbIsCreatedSuccessfully()
        {
            var options = new DbContextOptionsBuilder<QpasDbContext>()
                            .UseSqlite("Data Source=:memory:;")
                            .Options;

            using (var ctx = new QpasDbContext(options))
            {
                ctx.Database.OpenConnection();
                ctx.Database.Migrate();
                Seed.DoSeed(ctx);
                ctx.Database.CloseConnection();
            }
        }
    }
}