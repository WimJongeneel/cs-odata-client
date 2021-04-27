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
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("Id eq 1", expr);

            predicate = x => x.CreatedAt.Day == 2;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("day(CreatedAt) eq 2", expr);          
        }

        [Fact]
        public void FunctionsTests()
        {
            predicate = x => x.Foo.StartsWith("Fool");
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("startswith(Foo, 'Fool')", expr);

            predicate = x => x.Foo.Contains("Fool");
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("substringof('Fool', Foo)", expr);

            predicate = x => x.Foo.Trim().ToUpper() == "X";
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("toupper(trim(Foo)) eq 'X'", expr);

            predicate = x => x.Foo.Trim().ToUpper().Substring(1) == "X";
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("substring(toupper(trim(Foo)), 1) eq 'X'", expr);

            predicate = x => x.Foo.Replace("a", "a" + "b").Trim() == "q";
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("trim(replace(Foo, 'a', 'ab')) eq 'q'", expr);

            predicate = x => Math.Round(x.CreatedAt.Day / 2.0) == 1;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("round((day(CreatedAt) div 2)) eq 1", expr);

            predicate = x => x.CreatedAt.Day / x.CreatedAt.Year > 2;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("(day(CreatedAt) div year(CreatedAt)) gt 2", expr);

            predicate = x => x.Foo + "henk" + "X" != "";
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("concat(concat(Foo, 'henk'), 'X') ne ''", expr);
        }

        [Fact]
        public void UnaryOpsTests()
        {
            predicate = x => !x.Isbaz;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("not Isbaz", expr);
        }

        [Fact]
        public void BinaryOpsTests()
        {
            predicate = x => x.Isbaz && false;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("Isbaz and false", expr);

            predicate = x => !x.Isbaz && false;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("not Isbaz and false", expr);

            predicate = x => !(x.Isbaz && false);
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("not (Isbaz and false)", expr);

            predicate = x => !(!x.Isbaz && !false) || x.Foo == "X";
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("not (not Isbaz and true) or (Foo eq 'X')", expr);

            predicate = x => x.Id / 2 * 3 + 5 == 0;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("(((Id div 2) mul 3) add 5) eq 0", expr);

            predicate = x => x.Id / (2 * 3) + 5 == 0;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("((Id div 6) add 5) eq 0", expr);

            predicate = x => x.Id / (2 * x.Id) + 5 == 0;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("((Id div (2 mul Id)) add 5) eq 0", expr);

            predicate = x => (x.Id + 2) / (2 * x.Id) + (5 + 2) == 0;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("(((Id add 2) div (2 mul Id)) add 7) eq 0", expr);

             predicate = x => (x.Id + 2) / (2 * x.Id) + 5 + 2 == 0;
            expr = FilterExpression.Compile(predicate);
            Assert.Equal("((((Id add 2) div (2 mul Id)) add 5) add 2) eq 0", expr);
        }
    }
}
