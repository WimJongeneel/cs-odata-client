using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Hoppinger.OdataClient.QueryBuilder.Expands
{
    public class ExpandedRelations<Relations>
        where Relations : new()
    {
        private readonly List<Expand> expands;
        private readonly Relations relations = new Relations();

        public ExpandedRelations()
        {
            expands = new List<Expand>();
        }

        private ExpandedRelations(List<Expand> expands)
        {
            this.expands = expands;
        }

        public ExpandedRelations<Relations> Add<I, Result, R>(Func<Relations, ICollectionQuery<I>> inner, Func<Result, List<I>, R> merger)
        {
            var query = inner(relations);
            var expand = new Expand
                {
                    Merger = (a,b) => merger(a, Utils.CastItems<I>(b)),
                    Deserialize = a => Utils.ToDynamics(query.Deserialize((JArray)a)),
                    ToInnerQuery = query.ToInnerQuery,
                    Modelname = query.ModelName()
                };
            return Append(expand);
        }

        public ExpandedRelations<Relations> Add<I, Result, R>(Func<Relations, IObjectQuery<I>> inner, Func<Result, I, R> merger)
        {
            var query = inner(relations);
            var expand = new Expand
                {
                    Merger = (a,b) => merger(a, b),
                    Deserialize = a => query.Deserialize((JObject)a),
                    ToInnerQuery = query.ToInnerQuery,
                    Modelname = query.ModelName()
                };
            return Append(expand);
        }

        private ExpandedRelations<Relations> Append(Expand expand)
        {
            if(this.expands.Any(e => e.Modelname == expand.Modelname)) throw new Exception("Relation already expanded: " + expand.Modelname);
            var expands = this.expands.Append(expand).ToList();
            return new ExpandedRelations<Relations>(expands);
        }

        public List<Expand> ToList() => expands;
    }
}