using System;
using Newtonsoft.Json.Linq;

namespace Hoppinger.OdataClient.QueryBuilder.Expands
{
    public class Expand
    {
        public Func<dynamic, dynamic, dynamic> Merger { get; set; }
        public Func<JToken, dynamic> Deserialize  { get; set; }
        public Func<string> ToInnerQuery { get; set; }
        public string Modelname { get; set; }
    }
}