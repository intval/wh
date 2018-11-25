using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace WillhabenScrapper
{



        
    [Table("Flats")]
        public class Flat
    {
        [Key] public string id { get; set; }
        public string href { get; set; }
        public float price { get; set; }
        public int aream2 { get; set; }
        public int zimmerCount { get; set; }
        public string street { get; set; }
        public int district { get; set; }
        public string category { get; set; }
        public string rentorbuy { get; set; }
    }
    
}