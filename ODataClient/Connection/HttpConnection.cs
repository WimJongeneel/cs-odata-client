using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Hoppinger.OdataClient.Connection
{
    public static class HttpConnection
    {
        public static async Task<T> GetODataValue<T>(IHttpClientFactory httpClientFactory, string url)
            where T : JToken
        {
            using(var client = httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                var res = await client.GetAsync(url);

                res.EnsureSuccessStatusCode();

                var content = await res.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                if(json.GetValue("odata.error") is JObject j && j.GetValue("message") is JObject m)
                    throw new Exception(m.GetValue("value").ToString());
                
                return (T)json.GetValue("value");
            }
        }
    }
}