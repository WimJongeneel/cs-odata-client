using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Hoppinger.OdataClient.QueryBuilder.Expands;

namespace Hoppinger.OdataClient.Deserializer
{
    public static class ObjectDeserializer
    {
         public static Result Deserialize<SelectFields, Result>(JObject jObject, string select, Func<SelectFields, dynamic> selector, List<Expand> expands)
            where SelectFields : new()
        {
            var type = typeof(SelectFields);
            var fields = new SelectFields();

            foreach(var prop in select.Split(','))
                type.GetProperty(prop).SetValue(fields, jObject.GetValue(prop).ToObject(type.GetProperty(prop).PropertyType));

            var item = selector(fields);
            
            foreach(var e in expands)
            {
                var rel = jObject.GetValue(e.Modelname);
                if(rel is null) 
                {   
                    item = e.Merger(item, null);
                }
                else
                {
                    var parsed = e.Deserialize(rel);
                    item = e.Merger(item, parsed);
                }
                
            }
            
            return item;
        }
    }
}
