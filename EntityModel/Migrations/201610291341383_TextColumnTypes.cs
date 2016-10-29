namespace EntityModel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TextColumnTypes : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Trades", "Notes", c => c.String(unicode: false, storeType: "text"));
            AlterColumn("dbo.UserScripts", "Code", c => c.String(unicode: false, storeType: "text"));
            AlterColumn("dbo.UserScripts", "ReferencedAssembliesAsString", c => c.String(unicode: false, storeType: "text"));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.UserScripts", "ReferencedAssembliesAsString", c => c.String());
            AlterColumn("dbo.UserScripts", "Code", c => c.String());
            AlterColumn("dbo.Trades", "Notes", c => c.String());
        }
    }
}
