namespace WillhabenScrapper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Flats",
                c => new
                    {
                        id = c.String(nullable: false, maxLength: 128),
                        href = c.String(),
                        price = c.Single(nullable: false),
                        aream2 = c.Int(nullable: false),
                        zimmerCount = c.Int(nullable: false),
                        street = c.String(),
                        district = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Flats");
        }
    }
}
