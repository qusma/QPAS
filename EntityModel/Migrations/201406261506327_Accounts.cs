namespace EntityModel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Accounts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Accounts",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        AccountId = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.AccountId, unique: true);
            
            AddColumn("dbo.CashTransactions", "AccountID", c => c.Int());
            AddColumn("dbo.FXTransactions", "AccountID", c => c.Int());
            AddColumn("dbo.Orders", "AccountID", c => c.Int());
            AddColumn("dbo.Executions", "AccountID", c => c.Int());
            AddColumn("dbo.DividendAccruals", "AccountID", c => c.Int());
            AddColumn("dbo.EquitySummaries", "AccountID", c => c.Int());
            AddColumn("dbo.FXPositions", "AccountID", c => c.Int());
            AddColumn("dbo.OpenPositions", "AccountID", c => c.Int());
            AddColumn("dbo.PriorPositions", "AccountID", c => c.Int());
            CreateIndex("dbo.CashTransactions", "AccountID");
            CreateIndex("dbo.FXTransactions", "AccountID");
            CreateIndex("dbo.Orders", "AccountID");
            CreateIndex("dbo.Executions", "AccountID");
            CreateIndex("dbo.DividendAccruals", "AccountID");
            CreateIndex("dbo.EquitySummaries", "AccountID");
            CreateIndex("dbo.FXPositions", "AccountID");
            CreateIndex("dbo.OpenPositions", "AccountID");
            CreateIndex("dbo.PriorPositions", "AccountID");
            AddForeignKey("dbo.CashTransactions", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.FXTransactions", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.Orders", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.Executions", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.DividendAccruals", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.EquitySummaries", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.FXPositions", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.OpenPositions", "AccountID", "dbo.Accounts", "ID");
            AddForeignKey("dbo.PriorPositions", "AccountID", "dbo.Accounts", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PriorPositions", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.OpenPositions", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.FXPositions", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.EquitySummaries", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.DividendAccruals", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.Executions", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.Orders", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.FXTransactions", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.CashTransactions", "AccountID", "dbo.Accounts");
            DropIndex("dbo.PriorPositions", new[] { "AccountID" });
            DropIndex("dbo.OpenPositions", new[] { "AccountID" });
            DropIndex("dbo.FXPositions", new[] { "AccountID" });
            DropIndex("dbo.EquitySummaries", new[] { "AccountID" });
            DropIndex("dbo.DividendAccruals", new[] { "AccountID" });
            DropIndex("dbo.Executions", new[] { "AccountID" });
            DropIndex("dbo.Orders", new[] { "AccountID" });
            DropIndex("dbo.FXTransactions", new[] { "AccountID" });
            DropIndex("dbo.CashTransactions", new[] { "AccountID" });
            DropIndex("dbo.Accounts", new[] { "AccountId" });
            DropColumn("dbo.PriorPositions", "AccountID");
            DropColumn("dbo.OpenPositions", "AccountID");
            DropColumn("dbo.FXPositions", "AccountID");
            DropColumn("dbo.EquitySummaries", "AccountID");
            DropColumn("dbo.DividendAccruals", "AccountID");
            DropColumn("dbo.Executions", "AccountID");
            DropColumn("dbo.Orders", "AccountID");
            DropColumn("dbo.FXTransactions", "AccountID");
            DropColumn("dbo.CashTransactions", "AccountID");
            DropTable("dbo.Accounts");
        }
    }
}
