namespace WillhabenScrapper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Flats",
                c => new
                    {
                        id = c.String(nullable: false, maxLength: 20),
                        href = c.String(unicode: false, maxLength: 400),
                        price = c.Single(nullable: false),
                        aream2 = c.Int(nullable: false),
                        zimmerCount = c.Int(nullable: false),
                        street = c.String(unicode: false, maxLength: 40),
                        district = c.Int(nullable: false),
                        category = c.String(unicode: false, maxLength: 10),
                        rentorbuy = c.String(unicode: false, maxLength: 10),
                        wasContacted = c.Boolean(nullable: false),
                        updatedInDb = c.DateTime(nullable: false, precision: 0),
                        addedOnWh = c.DateTime(nullable: false, precision: 0),
                        deletedFromWh = c.DateTime(precision: 0),
                        Title = c.String(unicode: false, maxLength: 400),
                        AdTextContent = c.String(unicode: false, storeType: "longtext"),
                        BauJahr = c.Int(nullable: false),
                        IsNeubau = c.Boolean(nullable: false),
                        Lat = c.String(unicode: false),
                        Long = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.id)
                .Index(t => t.district)
                .Index(t => t.updatedInDb)
                .Index(t => t.href)
                .Index(t => t.deletedFromWh);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Flats", new[] { "deletedFromWh" });
            DropIndex("dbo.Flats", new[] { "updatedInDb" });
            DropIndex("dbo.Flats", new[] { "district" });
            DropTable("dbo.Flats");
        }
    }
}
