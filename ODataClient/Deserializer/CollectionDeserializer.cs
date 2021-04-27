using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Hoppinger.OdataClient.QueryBuilder.Expands;

namespace Hoppinger.OdataClient.Deserializer
{
    public static class CollectionDeserializer
    {
        public static List<Result> Deserialize<SelectFields, Result>(JArray data, string select, Func<SelectFields, dynamic> selector, List<Expand> expands)
            where SelectFields : new()
        {
            var items = new List<Result>();
            var type = typeof(SelectFields);

            foreach(var o in data)
                if(o is JObject jObject)
                    items.Add(ObjectDeserializer.Deserialize<SelectFields, Result>(jObject, select, selector, expands));
            
            return items;
        }
    }
}
