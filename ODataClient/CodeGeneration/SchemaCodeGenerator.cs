using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hoppinger.OdataClient.CodeGeneration
{
    public enum RelationSettings
    {
        None, ToOne, All
    }

    public class SchemaCodeGeneratorOptions
    {
        // TODO: update deserialisation first
        // public bool AllowSelectOnRelations { get; set; }

        // TODO: only for to-1 relations, untill support for any() and all()
        public RelationSettings FilterOnRelations { get; set; }
        public RelationSettings OrderbyOnRelations { get; set; }

        public static SchemaCodeGeneratorOptions DEFAULT = new SchemaCodeGeneratorOptions 
        {
            // AllowSelectOnRelations = false,
            FilterOnRelations = RelationSettings.ToOne,
            OrderbyOnRelations = RelationSettings.ToOne
        };

        public static SchemaCodeGeneratorOptions SHAREPOINT = new SchemaCodeGeneratorOptions 
        {
            // AllowSelectOnRelations = true,
            FilterOnRelations = RelationSettings.ToOne,
            OrderbyOnRelations =  RelationSettings.ToOne
        };

        public static SchemaCodeGeneratorOptions NONE = new SchemaCodeGeneratorOptions 
        {
            // AllowSelectOnRelations = false,
            FilterOnRelations = RelationSettings.None,
            OrderbyOnRelations =  RelationSettings.None
        };
    }

    public class SchemaCodeGenerator
    {
        private readonly IHttpClientFactory httpClientFactory;

        public SchemaCodeGenerator(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<string> Generate(string  url, SchemaCodeGeneratorOptions options = null)
        {
            var result = new StringBuilder();
            
            result.AppendLine("using Hoppinger.OdataClient.QueryBuilder;");
            result.AppendLine("using System;");
            result.AppendLine("using System.Net.Http;");
            result.AppendLine("");
            result.AppendLine("namespace OdataClient");
            result.AppendLine("{");
            
            using(var client = httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/xml");
                var stream = await client.GetStreamAsync(url.TrimEnd('/') + "/$metadata");

                var elem = XElement.Load(stream);

                var schemas = elem.Descendants().First().Descendants().Where(d => d.Name.LocalName == "Schema");

                foreach(var s in schemas) BuildSchema(s, result, options ?? SchemaCodeGeneratorOptions.DEFAULT, url);
            }
            
            result.AppendLine("}");

            return result.ToString();
        }

        private void BuildSchema(XElement schema, StringBuilder result, SchemaCodeGeneratorOptions options, string url)
        {
            var entities = schema.Descendants().Where(d => d.Name.LocalName == "EntityType");

            foreach(var e in entities)
            {
                BuildEntity(e, result, options);
            }

            var containers = schema.Descendants().Where(d => d.Name.LocalName == "EntityContainer");

            foreach(var c in containers)
            {
                BuildContainer(c, result, options, url);
            }
        }

        private void BuildEntity(XElement entity, StringBuilder result, SchemaCodeGeneratorOptions options)
        {
            BuildEntityClass(entity, result);

            BuildRelationsClass(entity, result);
           
            if(options.OrderbyOnRelations != RelationSettings.None) BuildOrderbyClass(entity, result, options);
            
            if(options.FilterOnRelations != RelationSettings.None) BuildFilterClass(entity, result, options);
        }

        private void BuildEntityClass(XElement entity, StringBuilder result)
        {
            var name = entity.Attribute("Name").Value;

            result.AppendLine($"\tpublic class {name}");
            result.AppendLine("\t{");

            AppendProperties(entity, result);

            result.AppendLine("\t}");
            result.AppendLine("");
        }

        private void BuildRelationsClass(XElement entity, StringBuilder result)
        {
            var relations = entity.Descendants().Where(d => d.Name.LocalName == "NavigationProperty");
            var name = entity.Attribute("Name").Value;

            result.AppendLine($"\tpublic class {name}Relations");
            result.AppendLine("\t{");

            foreach(var r in relations)
            {
                var rname = r.Attribute("Name").Value;
                var rtype = r.Attribute("Type").Value;
                if(rtype.StartsWith("Collection(")){
                    rtype = GetCollecionType(rtype);
                    result.AppendLine($"\t\tpublic Query<{rtype}, {rtype}, {rtype}Relations, {rtype}> {rname} {{ get => new Query<{rtype}, {rtype}, {rtype}Relations, {rtype}>(null, \"{rname}\", null); }}");
                }
                else
                {
                    rtype = rtype.Split('.').Last();
                   
                    result.AppendLine($"\t\tpublic Query<{rtype}, {rtype}Relations> {rname} {{ get => new Query<{rtype}, {rtype}Relations>(null,\"{rname}\", null); }}");
                }
            }

            result.AppendLine("\t}");
            result.AppendLine("");
        }

        private void BuildOrderbyClass(XElement entity, StringBuilder result, SchemaCodeGeneratorOptions options)
        {
            var name = entity.Attribute("Name").Value;
 
            result.AppendLine($"\tpublic class {name}Orderby");
            result.AppendLine("\t{");

            AppendProperties(entity, result);

            var relations = entity.Descendants()
                .Where(d => d.Name.LocalName == "NavigationProperty")
                .Where(d => options.OrderbyOnRelations == RelationSettings.All || !d.Attribute("Type").Value.StartsWith("Collection("));

            foreach(var r in relations)
            {
                var rname = r.Attribute("Name").Value;
                var rtype = r.Attribute("Type").Value;
                if(rtype.StartsWith("Collection(")) rtype = String.Join("", rtype.Skip("Collection(".Length).SkipLast(1));
                rtype = rtype.Split('.').Last();

                result.AppendLine($"\t\tpublic {rtype}Orderby {rname} {{ get; set;}}");
            }

            result.AppendLine("\t}");
            result.AppendLine("");
        }

        private void AppendProperties(XElement entity, StringBuilder result)
        {
            var properties = entity.Descendants().Where(d => d.Name.LocalName == "Property");
            
            foreach(var p in properties)
            {
                var pname = p.Attribute("Name").Value;
                var type = EDM_TYPES[p.Attribute("Type").Value];
                
                result.AppendLine($"\t\tpublic {type} {pname} {{ get; set; }}");
            }
        }

        private void BuildFilterClass(XElement entity, StringBuilder result, SchemaCodeGeneratorOptions options)
        {
            var name = entity.Attribute("Name").Value;
            result.AppendLine($"\tpublic class {name}Filter");
            result.AppendLine("\t{");

            AppendProperties(entity, result);

            var relations = entity.Descendants()
                .Where(d => d.Name.LocalName == "NavigationProperty")
                .Where(d => options.FilterOnRelations == RelationSettings.All || !d.Attribute("Type").Value.StartsWith("Collection("));


            foreach(var r in relations)
            {
                var rname = r.Attribute("Name").Value;
                var rtype = r.Attribute("Type").Value;

                if(rtype.StartsWith("Collection(")){
                    rtype = GetCollecionType(rtype);
                    result.AppendLine($"\t\tpublic List<{rtype}Filter> {rname} {{ get; set; }}");
                }
                else
                {
                    rtype = rtype.Split('.').Last();
                    result.AppendLine($"\t\tpublic {rtype}Filter {rname} {{ get; set; }}");
                }
            }

            result.AppendLine("\t}");
            result.AppendLine("");
        }

        private void BuildContainer(XElement container, StringBuilder result, SchemaCodeGeneratorOptions options, string url)
        {
            var sets = container.Descendants().Where(d => d.Name.LocalName == "EntitySet");

            result.AppendLine($"\tpublic class ODataContext");
            result.AppendLine("\t{");
            result.AppendLine("\t\tprivate readonly IHttpClientFactory httpClientFactory;");
            result.AppendLine($"\t\tprivate readonly string baseurl = \"{url}\";");
            result.AppendLine("");
            result.AppendLine("\t\tpublic ODataContext(IHttpClientFactory httpClientFactory)");
            result.AppendLine("\t\t{");
            result.AppendLine("\t\t\tthis.httpClientFactory = httpClientFactory;");
            result.AppendLine("\t\t}");
            result.AppendLine("");

            foreach(var s in sets)
            {
                var name = s.Attribute("Name").Value;
                var type = s.Attribute("EntityType").Value.Split('.').Last();
                var orderby = options.OrderbyOnRelations != RelationSettings.None ? type + "Orderby" : type;
                var filter = options.FilterOnRelations != RelationSettings.None ? type + "Filter" : type;

                result.AppendLine($"\t\tpublic Query<{type}, {filter}, {type}Relations, {orderby}> {name} {{ get => new Query<{type}, {filter}, {type}Relations, {orderby}>(baseurl, \"{name}\", httpClientFactory); }}");
            }

            result.AppendLine("\t}");
        }

        private string GetCollecionType(string rtype) =>
            String.Join("", rtype.Skip("Collection(".Length).SkipLast(1)).Split('.').Last();

        private static Dictionary<string, string> EDM_TYPES = new Dictionary<string, string> {
            { "Edm.String", "string" },
            { "Edm.Binary", "string" },
            { "Edm.DateTimeOffset", "DateTimeOffset" },
            { "Edm.DateTime", "DateTime" },
            { "Edm.Int32", "int" },
            { "Edm.Int16", "int" },
            { "Edm.Single", "Single" },
            { "Edm.Decimal", "decimal" },
            { "Edm.Boolean", "bool" },
        };
    }
}