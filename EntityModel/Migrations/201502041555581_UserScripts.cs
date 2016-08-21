namespace EntityModel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserScripts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserScripts",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(maxLength: 255),
                        ReferencedAssembliesAsString = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            AddColumn("dbo.Orders", "OrderReference", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Orders", "OrderReference");
            DropTable("dbo.UserScripts");
        }
    }
}
