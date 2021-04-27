using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Hoppinger.OdataClient.Compilation;
using Hoppinger.OdataClient.Connection;
using Hoppinger.OdataClient.Deserializer;
using Hoppinger.OdataClient.QueryBuilder.Expands;

namespace Hoppinger.OdataClient.QueryBuilder
{
    public class SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> : ICollectionQuery<Result>
        where Relations : new () 
        where SelectFields : new ()
    {
        private readonly QueryDescriptior queryDescriptior;
        private readonly Func<SelectFields, dynamic> selector;
        private readonly ExpandedRelations<Relations> expands;
        private readonly IHttpClientFactory httpClientFactory;

        public SelectedCollectionQuery(string baseurl, string model, string select, Func<SelectFields, Result> selector, IHttpClientFactory httpClientFactory)
        {
            this.selector = x => selector(x);
            queryDescriptior = new QueryDescriptior(baseurl, model).Select(select);
            expands = new ExpandedRelations<Relations>();
            this.httpClientFactory = httpClientFactory;
        }

        private SelectedCollectionQuery(QueryDescriptior queryDescriptior, Func<SelectFields, dynamic> selector, ExpandedRelations<Relations> expands, IHttpClientFactory httpClientFactory)
        {
            this.queryDescriptior = queryDescriptior;
            this.selector = selector;
            this.expands = expands;
            this.httpClientFactory = httpClientFactory;
        }

        public string ModelName() => queryDescriptior.model;

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> Filter(Expression<Func<FilterFields, bool>> predicate)
        {
            var newFilter = FilterExpression.Compile(predicate);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.Filter(newFilter), selector, expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> Orderby(Expression<Func<OrderbyFields, object>> keySelector)
        {
            var orderby = OrderbyExpression.Compile<OrderbyFields>(keySelector);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.Orderby(orderby), selector, expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> OrderbyThen(Expression<Func<OrderbyFields, object>> keySelector)
        {
            var orderby = OrderbyExpression.Compile<OrderbyFields>(keySelector);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.OrderbyThen(orderby), selector, expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> OrderbyDescending(Expression<Func<OrderbyFields, object>> keySelector)
        {
            var orderby = OrderbyExpression.Compile<OrderbyFields>(keySelector);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.Orderby($"{orderby} desc"), selector, expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> OrderbyDescendingThen(Expression<Func<OrderbyFields, object>> keySelector)
        {
            var orderby = OrderbyExpression.Compile<OrderbyFields>(keySelector);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.OrderbyThen($"{orderby} desc"), selector, expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> Top(int top) =>
            new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.Top(top), selector, expands, httpClientFactory
            );

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result> Skip(int skip) =>
            new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, Result>(
                queryDescriptior.Skip(skip), selector, expands, httpClientFactory
            );


        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, R> Expand<I, R>(Func<Relations, ICollectionQuery<I>> inner, Func<Result, List<I>, R> merger)
        {
            var expands = this.expands.Add(inner, merger);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, R>(
                queryDescriptior, selector: selector, expands: expands, httpClientFactory
            );
        }

        public SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, R> Expand<I, R>(Func<Relations, IObjectQuery<I>> inner, Func<Result, I, R> merger)
        {
            var expands = this.expands.Add(inner, merger);
            return new SelectedCollectionQuery<SelectFields, FilterFields, OrderbyFields, Relations, R>(
                queryDescriptior, selector: selector, expands: expands, httpClientFactory
            );
        }

        public async Task<IEnumerable<Result>> ExecuteAsync()
        {
            var url = queryDescriptior.baseurl.TrimEnd('/') + "/" + ToQuery();
            var json = await HttpConnection.GetODataValue<JArray>(httpClientFactory, url);
            return Deserialize(json);
        }

        public List<Result> Deserialize(JArray data) =>
            CollectionDeserializer.Deserialize<SelectFields, Result>(data, queryDescriptior.select, selector, expands.ToList());

        public string ToInnerQuery() => queryDescriptior.ToQuery(";", expands.ToList());

        public string ToQuery() => $"{queryDescriptior.model}?{queryDescriptior.ToQuery("&", expands.ToList())}";
    }
}