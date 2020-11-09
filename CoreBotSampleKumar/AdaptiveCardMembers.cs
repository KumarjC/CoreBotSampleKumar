using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace CoreBotSampleKumar
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Item
    {
        public string type { get; set; }
        public string text { get; set; }
        public bool isSubtle { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string spacing { get; set; }
        public string url { get; set; }
        public string horizontalAlignment { get; set; }
        public string weight { get; set; }
    }

    public class Column
    {
        public string type { get; set; }
        public object width { get; set; }
        public List<Item> items { get; set; }
    }

    public class Body
    {
        public string type { get; set; }
        public string text { get; set; }
        public string weight { get; set; }
        public bool isSubtle { get; set; }
        public bool? separator { get; set; }
        public string spacing { get; set; }
        public List<Column> columns { get; set; }
    }

    public class Root
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }
        public string version { get; set; }
        public string type { get; set; }
        public string speak { get; set; }
        public List<Body> body { get; set; }
    }


}
