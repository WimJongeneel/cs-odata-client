using System;
using Xunit;
using Hoppinger.OdataClient.Compilation;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ODataClientTests
{
    public class SelectExpressionTests
    {
        Expression<Func<Bar, dynamic>> expr;

        [Fact]
        public void SelectTest()
        {
            expr = x => x;
            var s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Id,CreatedAt,Foo,Isbaz", s);

            expr = x => new {x.Foo};
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Foo", s);

            expr = x => new {x.Foo, x.Id};
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Foo,Id", s);

            expr = x => new { X = new {x.Id} };
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Id", s);

            expr = x => new { X = new {C = x.Foo.Substring(3, 0).Split('.', StringSplitOptions.None)} };
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Foo", s);

            expr = x => new { X = new List<string> { "Foo", x.Foo, x.CreatedAt.ToString() } };
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Foo,CreatedAt", s);

            expr = x => x.Isbaz ? x.Foo : x.CreatedAt.ToShortDateString();
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Isbaz,Foo,CreatedAt", s);

            expr = x => x.Isbaz ? x.Foo + x.CreatedAt.ToShortDateString() : "";
            s = SelectExpression.Compile<Bar>(expr);
            Assert.Equal("Isbaz,Foo,CreatedAt", s);
        }
    }
}