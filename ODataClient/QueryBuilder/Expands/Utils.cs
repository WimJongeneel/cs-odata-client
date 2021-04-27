using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Hoppinger.OdataClient.QueryBuilder.Expands
{
    public class Utils
    {
        public static List<dynamic> ToDynamics<I>(List<I> l)
        {
            var l1 = new List<dynamic>();
            foreach(var i in l) l1.Add(i);
            return l1;
        }

        public static List<I> CastItems<I>(List<dynamic> l)
        {
            var l1 = new List<I>();
            foreach(var i in l) l1.Add(i);
            return l1;
        }
    }
}