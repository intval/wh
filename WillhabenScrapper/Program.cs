
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

using IronWebScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace WillhabenScrapper
{

    public class WhContext : DbContext
    {

        public WhContext() : base()
        {
        }

        public DbSet<Flat> Flats{ get; set; }
   
    }

    class Program
    {
        const string whStringFormat = "https://www.willhaben.at/iad/immobilien/{0}/neueste-immobilienanzeigen?areaId=900&periode=120&rows=100&page=";
        const string flatBuyUrl = "eigentumswohnung";
        const string flatRentUrl = "mietwohnungen";
        const string gewerbeBuyUrl = "gewerbeimmobilien-kaufen";
        const string gewerbeRentUrl = "gewerbeimmobilien-mieten";
        private string url;
        private WhContext ctx;

        static void Main(string[] args)
        {
            new Program().Run();

        }

        private void Run()
        {
            
            ctx = new WhContext();
            var a = ctx.Flats.First();
            
            
            var scrapper = new WhScraper(string.Format(whStringFormat, gewerbeRentUrl), 100);
            scrapper.ObeyRobotsDotTxt = false;
            scrapper.OnData = StoreInDb;
            scrapper.Start();
            
            Console.ReadKey();

            
        }

        private void StoreInDb( Flat e)
        {
            Console.WriteLine($"flat with ID {e.id} on {e.aream2} with {e.zimmerCount} rooms for {e.price} on {e.street} in {e.district}");

            lock(ctx)
            if (null == ctx.Flats.Find(e.id))
            {
                ctx.Flats.Add(e);
                ctx.SaveChanges();
            }

        }


        class WhScraper : WebScraper
        {

            public WhScraper(string baseUrl, int pagesToCrawl = 100) : base()
            {
                this.url = baseUrl;
                this.pagesToCrawl = pagesToCrawl;
            }

            public Action<Flat> OnData;
            private string url;
            private int pagesToCrawl;

            public override void Init()
            {
                this.LoggingLevel = WebScraper.LogLevel.All;

                this.RateLimitPerHost = new TimeSpan(0, 0, 1);
                
                for(var i = 1; i <= pagesToCrawl; i++)
                    this.Request(url + i, Parse);

                this.ObeyRobotsDotTxt = false;
            }
            public override void Parse(Response response)
            {
                foreach (var realEstate in response.Css(".isRealestate"))
                {
                    var size = realEstate.Css(".info > .desc-left");
                    var price = realEstate.Css("script");
                    var header = realEstate.Css(".header > a");
                    

                    var address = realEstate.Css(".address-lg");


                    var id = header.First().Attributes["data-ad-link"];
                    var href = header.First().Attributes["href"];

                    var p = Regex.Matches(price.First().TextContentClean, @"'([A-Z0-9a-z=\+]*)'\)\)")[0].Groups[1].Value;
                    var p2 = Encoding.UTF8.GetString(Convert.FromBase64String(p));
                    var p3 = Regex.Match(p2, @"([\d\.]+)").Value.Replace(".", "").Replace(",", "");

                    if (string.IsNullOrEmpty(p3))
                        p3 = "99999999";

                    var p4 = float.Parse(p3);

                    var areaMatches = Regex.Matches(size.First().TextContentClean, @"(\d+) m");
                    
                        var m2 = (areaMatches.Count > 0) ? int.Parse(areaMatches[0].Groups[1].Value) : 1;
                     

                    var roomsMatch = Regex.Matches(size.First().TextContentClean, @"m.*(\d+) Zimmer");
                    var zimmerCount = (roomsMatch.Count > 0) ? int.Parse(roomsMatch[0].Groups[1].Value) : -1;

                    /* Renngasse 10, 1010 Wien, 01.Bezirk, Innere Stadt */
                    /* 1220 Wien, 22. Bezirk, Donaustadt */
                    var addr = Regex.Matches(address.First().TextContentClean, @"(.*),?.*1(\d\d)0 Wien");

                    var street = "";
                    var district = 1230;

                    if(addr.Count > 0)
                    {
                        street = addr[0].Groups[1].Value;
                        district = int.Parse("1" + addr[0].Groups[2].Value + "0");
                    }

                    


                    if (OnData != null)
                    {
                        OnData(new Flat
                        {
                            id = id,
                            aream2 = m2,
                            district = district,
                            href = href,
                            price = p4,
                            street = street,
                            zimmerCount = zimmerCount,
                            category = UrlToCategory(response.RequestlUrl),
                            rentorbuy = UrlToRentOrBuy(response.RequestlUrl)
                        });
                    }

                }
                
            }

            private string UrlToRentOrBuy(string requestlUrl)
            {
                if (requestlUrl.ToLower().Contains(flatBuyUrl) || requestlUrl.ToLower().Contains(gewerbeBuyUrl))
                    return "buy";

                if (requestlUrl.ToLower().Contains(flatRentUrl) || requestlUrl.ToLower().Contains(gewerbeRentUrl))
                    return "rent";

                return null;
            }

            private string UrlToCategory(string requestlUrl)
            {
                if (requestlUrl.ToLower().Contains(flatBuyUrl) || requestlUrl.ToLower().Contains(flatRentUrl))
                    return "wohnung";

                if (requestlUrl.ToLower().Contains("/gewerbeimmobilien-"))
                    return "gewerbe";

                return null;
            }
        }
    }
}
