﻿
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
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading;
using MySql.Data.Entity;
using System.Runtime.CompilerServices;

namespace WillhabenScrapper
{

    [Table("Flats")]
    public class Flat { 
        [Key] public string id { get; set; }
        public string href { get; set; }
        public float price { get; set; }
        public int aream2 { get; set; }
        public int zimmerCount { get; set; }
        public string street { get; set; }
        [Index] public int district { get; set; }
        public string category { get; set; }
        public string rentorbuy { get; set; }
        public bool wasContacted { get; set; }
        [Index] public DateTime updatedInDb { get; set; }
        public DateTime addedOnWh { get; set; }
        [Index] public DateTime? deletedFromWh { get; set; }
        public string Title { get; set; }
        public string AdTextContent { get; set; } = null;
        
        public int BauJahr { get; set; } = 1700;
        public bool IsNeubau { get; set; } = false;
        public string Lat { get; set; } = "0";
        public string Long { get; set; } = "0";
    }

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class WhContext : DbContext
    {

        public WhContext() : base()
        {
            //this.Database.Log = Console.WriteLine;

        }

        public DbSet<Flat> Flats{ get; set; }

   
    }

    class Program
    {
        private static int areaId = 900;

        const string whStringFormat = "https://www.willhaben.at/iad/immobilien/{0}/neueste-immobilienanzeigen?areaId={2}&periode={1}&rows=100&page=";
        const string flatBuyUrl = "eigentumswohnung";
        const string flatRentUrl = "mietwohnungen";
        const string gewerbeBuyUrl = "gewerbeimmobilien-kaufen";
        const string gewerbeRentUrl = "gewerbeimmobilien-mieten";
        
        private WhContext ctx;
        //private WhContext ctx2;
        private int DistrictForArea;
        private int progressCounter = 0;
        
        private int totalCount = 0;

        public Program()
        {
            this.ctx = new WhContext();
            //this.ctx2 = new WhContext();

            ctx.Database.CommandTimeout = 3000;

            //areaId = 601;

            this.DistrictForArea = 1230;
            if (areaId == 601)
                DistrictForArea = 8020;
        }

        static void Main(string[] args)
        {

            Console.WriteLine( "Starting" );

            if (args.Count() > 0 && int.TryParse(args[0], out int areaId))
                Program.areaId = areaId;

            
            new Program().ScrapLists();
            GC.Collect(2);
            new Program().ScrapEachItem();
            GC.Collect(2);
            new Program().RefreshOutdated();

            Console.WriteLine("Finished");
            Console.ReadKey();
        }

        private void RefreshOutdated()
        {
            

        }

        private void ScrapEachItem(int? take = null)
        {
            var dt = DateTime.Now.Subtract(TimeSpan.FromDays(7));

            var col = ctx.Flats
                .Where(x => 
                    (x.AdTextContent == null && x.deletedFromWh == null) ||
                    (x.updatedInDb < dt && x.deletedFromWh == null) 
                 )
                .Select(x => x.href)
                .ToArray();

            this.totalCount = col.Count();

            Console.WriteLine("Records matching scraping " + totalCount);

            ScrapEachItem(col);


        }

        private void ScrapEachItem(IEnumerable<string> col)
        {
            


            var iter = col.GetEnumerator();
            

            int ProIter = 10;
            int maxThreads = 10;
            decimal totalScrappersRequired = Math.Ceiling((decimal)totalCount / ProIter);

            List<Task> awaiters = new List<Task>((int) totalScrappersRequired);
      
            var semaphore = new SemaphoreSlim(maxThreads, maxThreads);


            if (totalCount > 0)
            {
                

                for (int i = 0; i < totalScrappersRequired; i++)
                {

                    semaphore.Wait();

                    var scrapper = GetScrappe();
                    //scrappers.Add(scrapper);
                    //scrapper.Request(iter.Current, scrapper.Parse);

                    var w = scrapper.StartAsync();
                    awaiters.Add(w);

                    w.GetAwaiter().OnCompleted(() => {
                        semaphore.Release();
                        scrapper = null;
                    });

                    var inThisIter = 0;

                    while(inThisIter < ProIter && iter.MoveNext())
                    {
                        inThisIter++;
                        //Console.WriteLine("Starting with " + iter.Current.id + " - " + iter.Current.Title);
                        scrapper.Request(iter.Current, scrapper.Parse);
                    }
                    
                }

                awaiters.ForEach(a => a.Wait());
                awaiters.Clear();

            }


            
            

            
        }

        private WhItemScrapper GetScrappe()
        {
            return new WhItemScrapper
            {
                ObeyRobotsDotTxt = false,
                OnData = (data) =>
                {
                    

                    try
                    {
                        var ctx2 = new WhContext();


                        
                        {
                            

                                ctx2.Flats.Attach(data);
                            
                                


                            ctx2.Entry(data).Property(x => x.addedOnWh).IsModified = true;
                            ctx2.Entry(data).Property(x => x.AdTextContent).IsModified = true;
                            //ctx2.Entry(data).Property(x => x.AdTextContentDirty).IsModified = true;
                            ctx2.Entry(data).Property(x => x.IsNeubau).IsModified = true;
                            ctx2.Entry(data).Property(x => x.BauJahr).IsModified = true;
                            ctx2.Entry(data).Property(x => x.BauJahr).IsModified = true;
                            ctx2.Entry(data).Property(x => x.Lat).IsModified = true;
                            ctx2.Entry(data).Property(x => x.Title).IsModified = true;
                            ctx2.Entry(data).Property(x => x.updatedInDb).IsModified = true;
                            data.updatedInDb = DateTime.Now;

                            ctx2.SaveChanges();
                        }


                        Interlocked.Increment(ref progressCounter);
                        Console.WriteLine($"[{progressCounter}/{totalCount}] Scraped {data.id}");
                    }
                    catch (Exception e)
                    {
                        //File.AppendAllText("./log.txt", e.ToString());

                        if (e.InnerException != null) e = e.InnerException;
                        Console.WriteLine(e.Message);
                    }
                },
                OnDelete = (url) =>
                {
                    Task.Factory.StartNew(() =>
                    {

                        try
                        {
                            //lock (ctx2)
                            {
                                var ctxxx = new WhContext();
                                ctxxx.Database
                                .ExecuteSqlCommandAsync($"UPDATE Flats SET deletedFromWh = \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\" " +
                                    $"WHERE href = \"{url}\" AND deletedFromWh is NULL")

                                .GetAwaiter().OnCompleted(() =>
                                {

                                    Interlocked.Increment(ref progressCounter);
                                    Console.WriteLine($"[{progressCounter}/{totalCount}] Deleted {url}");
                                    ctxxx.Dispose();
                                    ctxxx = null;
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.InnerException != null) e = e.InnerException;
                            Console.WriteLine(e.Message);
                        }

                    });//.Wait();

                    

                    
                }
            };
        }

        private void ScrapLists()
        {

            var all = new[] { flatBuyUrl, flatRentUrl, gewerbeBuyUrl, gewerbeRentUrl};
            int period = 120; // last xxx days of

            var lastFlat = ctx.Flats.Where(x => x.district == DistrictForArea).OrderByDescending(x => x.updatedInDb).FirstOrDefault();
            if(lastFlat != null)
            {
                period = (int) DateTime.Now.Subtract(lastFlat.updatedInDb).TotalDays + 1;
            }

            foreach (var u in all)
            {
                Console.WriteLine("Starting with " + u);
                var scrapper = new WhScraper(string.Format(whStringFormat, u, period, areaId), 100);
                scrapper.ObeyRobotsDotTxt = false;
                scrapper.OnData = StoreInDb;
                scrapper.Start();
            }

            

            
        }

        private void StoreInDb( Flat e)
        {
            Console.WriteLine($"flat with ID {e.id} on {e.aream2} with {e.zimmerCount} rooms for {e.price} on {e.street} in {e.district}");

            using (var ctx2 = new WhContext())
            {
                if (!ctx2.Flats.Any(x => x.id == e.id))
                {
                    ctx2.Flats.Add(e);
                    ctx2.SaveChanges();

                    if (e.updatedInDb == DateTime.MinValue)
                        e.updatedInDb = DateTime.Now;

                    if (e.addedOnWh == DateTime.MinValue)
                        e.addedOnWh = DateTime.Now;
                }
                else
                {

                }
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
            private int currentCrawledPageNum = 1;

            public override void Init()
            {
                this.LoggingLevel = WebScraper.LogLevel.None;
                this.RateLimitPerHost = new TimeSpan(0, 0, 1);
                this.ObeyRobotsDotTxt = false;

                //for (var i = 1; i <= pagesToCrawl; i++)
                    this.Request(url + currentCrawledPageNum, Parse);

            }
            public override void Parse(Response response)
            {
                foreach (var realEstate in response.Css(".isRealestate"))
                {
                    var size = realEstate.Css(".info > .desc-left");
                    var price = realEstate.Css("script");
                    var header = realEstate.Css(".header > a");

                    var title = realEstate.Css("[itemprop=\"name\"]").First().TextContentClean ?? "";

                    var address = realEstate.Css(".address-lg");


                    var id = header.First().Attributes["data-ad-link"];
                    var href = header.First().Attributes["href"];

                    var p = Regex.Matches(price.First().TextContentClean, @"'([A-Z0-9a-z=\+/]*)'\)\)")[0].Groups[1].Value;
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
                    var addr = Regex.Matches(address.First().TextContentClean, @"(.*),?.*(\d\d\d\d) (Wien|Graz|Linz|[A-z]{2,12})");

                    var street = "";
                    var district = 0000;

                    if(addr.Count > 0)
                    {
                        street = addr[0].Groups[1].Value;
                        district = int.Parse(addr[0].Groups[2].Value);
                    }




                    OnData?.Invoke(new Flat
                    {
                        id = id,
                        aream2 = m2,
                        district = district,
                        href = href,
                        price = p4,
                        street = street,
                        zimmerCount = zimmerCount,
                        category = UrlToCategory(response.RequestlUrl),
                        rentorbuy = UrlToRentOrBuy(response.RequestlUrl),
                        Title = title
                    });

                }

                if(response.CssExists(".isRealestate"))
                {
                    this.Request(url + (++currentCrawledPageNum), Parse);
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

        class WhItemScrapper : WebScraper
        {
            public Action<Flat> OnData;
            public Action<string> OnDelete;

            public override void Init()
            {
                this.LoggingLevel = WebScraper.LogLevel.None;
                this.RateLimitPerHost = new TimeSpan(0, 0, 1);
                this.ObeyRobotsDotTxt = false;
            }

            public override void Parse(Response response)
            {
                if(response.FinalUrl.Contains("fromExpiredAdId") || response.TextContent.Contains("Achtung: Die Anzeige wartet auf Aktivierung."))
                {
                    // redirected due to delete
                    OnDelete(response.RequestlUrl);
                    return;
                }

                if(response.FinalUrl != response.RequestlUrl)
                {
                    Console.WriteLine($"Redirected {response.RequestlUrl} to {response.FinalUrl}");
                }

                var sb = new StringBuilder();
                int baujahr = 1700;
                bool neubau = false;

                foreach(var box in response.Css(".container.left > .box-block"))
                {
                    sb.Append("------");

                    var boxHead2 = box.Css("h2").FirstOrDefault() ?? box.Css(".box-heading").First();



                    var boxTitle = boxHead2.TextContentClean;
                    sb.Append(boxTitle);
                    sb.Append("------");
                    var boxContent = box.Css(".box-body").First().TextContentClean;
                    sb.Append(boxContent);

                    sb.AppendLine(); sb.AppendLine(); sb.AppendLine();

                    
                }

                var clean = response.Css("body").First().InnerTextClean;

                var bauMatch = Regex.Match(clean, "Baujahr.{0,8}?(\\d{2,4})", RegexOptions.IgnoreCase); 
                if(bauMatch.Success)
                {
                    baujahr = int.Parse(Regex.Match(bauMatch.Value, "\\d+").Value);
                }

                if(Regex.IsMatch(clean, "NEUBAU", RegexOptions.IgnoreCase))
                {
                    neubau = true;
                }

                var dateField = response.Css("#advert-info-dateTime").First().TextContentClean;
                var dateMatch = Regex.Match(dateField, "\\d{2}\\.\\d{2}\\.\\d{4} \\d{2}:\\d{2}"); // 01.12.2018 06:38

                var date = dateMatch.Value;
                var dtm = DateTime.ParseExact(date, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

                var Idm = Regex.Match(response.Css("#advert-info-whCode").First().TextContentClean, "\\d{4,20}");
                var id = Idm.Value;

                var lat = response.Css("meta[itemprop=\"latitude\"]").FirstOrDefault()?.Attributes["content"];
                var longi = response.Css("meta[itemprop=\"longitude\"]").FirstOrDefault()?.Attributes["content"];


                var headerBox = response.Css(".adHeadingLine").First().TextContentClean;

                string title = null;

                var p = Regex.Matches(headerBox, @"'([A-Z0-9a-z=\+/]*)'\)\)");
                if(p.Count > 0)
                {
                    var p12 = p[0].Groups[1].Value;
                    var p2 = Encoding.UTF8.GetString(Convert.FromBase64String(p12));
                    var p3 = Regex.Match(p2, "\\<h1 [^\\>]*?>([^<]*)<", RegexOptions.Singleline);
                    title = p3.Groups[1].Value.Trim();
                }
                else
                {
                    int i = 0; 
                }
                





                OnData(new Flat {
                    AdTextContent = sb.ToString(),
                    //AdTextContentDirty = response.TextContent,
                    BauJahr = baujahr,
                    IsNeubau = neubau,
                    addedOnWh = dtm,
                    id = id,
                    Lat = lat,
                    Long = longi,
                    Title = title
                });
            }
        }
    }

    public class WhPageScrapped
    {
        public string TextContentClean { get; set; }
        public string TextContentDirty { get; set; }
        public int BauJahr { get; set; }
        public bool IsNeubau { get; set; }
        public DateTime WhPublishDate { get; set; }
        public string Id { get; set; }
        public string Long { get; internal set; }
        public string Lat { get; internal set; }
        public string Title { get; internal set; }
    }
}
