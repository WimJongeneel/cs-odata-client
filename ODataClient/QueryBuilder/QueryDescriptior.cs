using System;
using System.Collections.Generic;
using System.Linq;
using Hoppinger.OdataClient.QueryBuilder.Expands;

namespace Hoppinger.OdataClient.QueryBuilder
{
    public class QueryDescriptior
    {
        public readonly string baseurl;
        public readonly string select;
        public readonly string filter;
        public readonly string orderby;
        public readonly int top;
        public readonly int skip;
        public readonly string model;

        public QueryDescriptior(
            string baseurl,
            string select,
            string filter,
            string orderby,
            int top,
            int skip,
            string model
        )
        {
            this.baseurl = baseurl;
            this.select = select;
            this.filter = filter;
            this.orderby = orderby;
            this.top = top;
            this.skip = skip;
            this.model = model;
        }

        public QueryDescriptior(string baseurl, string model)
        {
            this.baseurl = baseurl;
            this.model = model;
        }

        public QueryDescriptior Select(string select) => 
            new QueryDescriptior(baseurl: baseurl, select: select, filter: filter, orderby: orderby, top: top, skip: skip, model: model);

        public QueryDescriptior Filter(string filter)
        { 
            var _filter = String.IsNullOrEmpty(this.filter) ? filter : this.filter + " and " + filter;
            return new QueryDescriptior(baseurl: baseurl, select: select, filter: _filter, orderby: orderby, top: top, skip: skip, model: model);
        }

        public QueryDescriptior Orderby(string orderby) => 
            new QueryDescriptior(baseurl: baseurl, select: select, filter: filter, orderby: orderby, top: top, skip: skip, model: model);

        public QueryDescriptior Top(int top) => 
            new QueryDescriptior(baseurl: baseurl, select: select, filter: filter, orderby: orderby, top: top, skip: skip, model: model);

        public QueryDescriptior Skip(int skip) => 
            new QueryDescriptior(baseurl: baseurl, select: select, filter: filter, orderby: orderby, top: top, skip: skip, model: model);

        public string ToQuery(string d, List<Expand> expands)
        {
            var query = $"$select={select}";
            
            if(!String.IsNullOrEmpty(filter)) query += $"{d}$filter={filter}";
            if(!String.IsNullOrEmpty(orderby)) query += $"{d}$orderby={orderby}";
            if(top > 0) query += $"{d}$top={top}";
            if(skip > 0) query += $"{d}$skip={skip}";
            
            if(expands.Count() > 0) 
            {
                var es = expands.Select(e => $"{e.Modelname}({e.ToInnerQuery()})");
                query += $"{d}$expand={String.Join(',', es)}";
            }
            
            return query;
        }
    }
}