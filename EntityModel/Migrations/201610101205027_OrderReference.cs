namespace EntityModel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrderReference : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Executions", "OrderReference", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Executions", "OrderReference");
        }
    }
}
