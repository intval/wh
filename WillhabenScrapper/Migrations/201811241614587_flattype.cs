namespace WillhabenScrapper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class flattype : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Flats", "category", c => c.String());
            AddColumn("dbo.Flats", "rentorbuy", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Flats", "rentorbuy");
            DropColumn("dbo.Flats", "category");
        }
    }
}
