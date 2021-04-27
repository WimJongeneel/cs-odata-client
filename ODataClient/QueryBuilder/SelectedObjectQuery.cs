using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Hoppinger.OdataClient.Connection;
using Hoppinger.OdataClient.Deserializer;
using Hoppinger.OdataClient.QueryBuilder.Expands;

namespace Hoppinger.OdataClient.QueryBuilder
{
    public class SelectedObjectQuery<SelectFields, Relations, Result> : IObjectQuery<Result> 
        where Relations : new () 
        where SelectFields : new ()
    {
        private readonly QueryDescriptior queryDescriptior;
        private readonly Func<SelectFields, dynamic> selector;
        private readonly ExpandedRelations<Relations> expands;
        private readonly IHttpClientFactory httpClientFactory;

        public SelectedObjectQuery(string baseurl, string model, string select, Func<SelectFields, Result> selector, IHttpClientFactory httpClientFactory)
        {
            this.selector = x => selector(x);
            queryDescriptior = new QueryDescriptior(baseurl, model).Select(select);
            expands = new ExpandedRelations<Relations>();
            this.httpClientFactory = httpClientFactory;
        }

        private SelectedObjectQuery(QueryDescriptior queryDescriptior, Func<SelectFields, dynamic> selector, ExpandedRelations<Relations> expands, IHttpClientFactory httpClientFactory)
        {
            this.queryDescriptior = queryDescriptior;
            this.selector = selector;
            this.expands = expands;
            this.httpClientFactory = httpClientFactory;
        }

        public string ModelName() => queryDescriptior.model;

        public SelectedObjectQuery<SelectFields, Relations, R> Expand<I, R>(Func<Relations, ICollectionQuery<I>> inner, Func<Result, List<I>, R> merger)
        {
            var expands = this.expands.Add(inner, merger);
            return new SelectedObjectQuery<SelectFields, Relations, R>(queryDescriptior, selector, expands, httpClientFactory);
        }

        public SelectedObjectQuery<SelectFields, Relations, R> Expand<I, R>(Func<Relations, IObjectQuery<I>> inner, Func<Result, I, R> merger)
        {
            var expands = this.expands.Add(inner, merger);
            return new SelectedObjectQuery<SelectFields, Relations, R>(queryDescriptior, selector, expands, httpClientFactory);
        }

        public async Task<Result> ExecuteAsync()
        {
            var url = queryDescriptior.baseurl.TrimEnd('/') + "/" + ToQuery();
            var json = await HttpConnection.GetODataValue<JObject>(httpClientFactory, url);
            return Deserialize(json);
        }

        public Result Deserialize(JObject jObject) =>
            ObjectDeserializer.Deserialize<SelectFields, Result>(jObject, queryDescriptior.select, selector, expands.ToList());

        public string ToInnerQuery() => queryDescriptior.ToQuery(";", expands.ToList());

        public string ToQuery() => $"{queryDescriptior.model}?{queryDescriptior.ToQuery("&", expands.ToList())}";
    }
}