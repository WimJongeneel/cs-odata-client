using System;
using Xunit;
using Hoppinger.OdataClient.Compilation;
using System.Linq.Expressions;

namespace ODataClientTests
{
    public class Bar 
    {
        public int Id { get; set; }
        public DateTime CreatedAt  { get; set; }
        public string Foo { get; set; }
        public bool Isbaz { get; set; }
    }
    public class FilterExpressionTests
    {
        Expression<Func<Bar, bool>> predicate;
        string expr;

        [Fact]
        public void EqualsTests()
        {
            predicate = x => x.Id == 1;
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("Id eq 1", expr);

            predicate = x => x.CreatedAt.Day == 2;
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("day(CreatedAt) eq 2", expr);          
        }

        [Fact]
        public void FunctionsTests()
        {
            predicate = x => x.Foo.StartsWith("Fool");
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("startswith(Foo, 'Fool')", expr);

            predicate = x => x.Foo.Contains("Fool");
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("substringof('Fool', Foo)", expr);

            predicate = x => x.Foo.Trim().ToUpper() == "X";
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("toupper(trim(Foo)) eq 'X'", expr);

            predicate = x => x.Foo.Trim().ToUpper().Substring(1) == "X";
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("substring(toupper(trim(Foo)), 1) eq 'X'", expr);

            predicate = x => x.Foo.Replace("a", "a" + "b").Trim() == "q";
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("trim(replace(Foo, 'a', 'ab')) eq 'q'", expr);

            predicate = x => Math.Round(x.CreatedAt.Day / 2.0) == 1;
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("round(day(CreatedAt) div 2) eq 1", expr);

            predicate = x => x.CreatedAt.Day / x.CreatedAt.Year > 2;
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("day(CreatedAt) div year(CreatedAt) gt 2", expr);

            predicate = x => x.Foo + "henk" + "X" != "";
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("concat(concat(Foo, 'henk'), 'X') ne ''", expr);
        }

        [Fact]
        public void UnaryOpsTests()
        {
            predicate = x => !x.Isbaz;
            expr = FilterExpression.Compile(predicate.Body);
            Assert.Equal("not Isbaz", expr);
        }
    }
}
