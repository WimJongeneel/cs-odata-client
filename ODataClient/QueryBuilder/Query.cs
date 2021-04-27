using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Hoppinger.OdataClient.Compilation;

namespace Hoppinger.OdataClient.QueryBuilder
{
    public interface ICollectionQuery<R> 
    {
        string ModelName();
        List<R> Deserialize(JArray data);
        string ToInnerQuery();
    }

    public interface IObjectQuery<R> 
    {
        string ModelName();
        R Deserialize(JObject data);
        string ToInnerQuery();
    }


    public class Query<SelectFields, FilterFields, Relations, OrderbyFields> 
        where Relations : new () 
        where SelectFields : new ()
    {
        private readonly string model;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string baseurl;

        public Query(string baseurl, string model, IHttpClientFactory httpClientFactory)
        {
            this.baseurl = baseurl;
            this.model = model;
            this.httpClientFactory = httpClientFactory;
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> Select<Result>(Expression<Func<SelectFields, Result>> selector)
        {
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                baseurl,
                model,
                SelectExpression.Compile<SelectFields>(selector),
                selector.Compile(),
                httpClientFactory
            );
        }
    }

    public class Query<SelectFields, Relations> 
        where Relations : new () 
        where SelectFields : new ()
    {
        private readonly string model;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string baseurl;

        public Query(string baseurl, string model, IHttpClientFactory httpClientFactory)
        {
            this.baseurl = baseurl;
            this.model = model;
            this.httpClientFactory = httpClientFactory;
        }

        public SelectedObjectQuery<SelectFields, Relations, Result> Select<Result>(Expression<Func<SelectFields, Result>> selector)
        {
            return new SelectedObjectQuery<SelectFields, Relations, Result>(
                baseurl,
                model,
                SelectExpression.Compile<SelectFields>(selector),
                selector.Compile(),
                httpClientFactory
            );
        }
    }  
}