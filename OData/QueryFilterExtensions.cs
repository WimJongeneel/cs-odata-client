using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;

namespace OdataClient
{
  public static class QueryFilterExtensions
  {
    public static Query<M> Filter<M>(this Query<M> query, Expression<Func<M, bool>> predicate)
      where M : class
    {
      var filterExpr = GetFilterExpr(predicate.Body);
      query.Filter = filterExpr;
      return query;
    }

    private static string GetFilterExpr(Expression predicate)
    {
      if(predicate is InvocationExpression iv)
      { 
        var lambda = Expression.Lambda<Func<object>>(iv.Expression).Compile();
        var result = lambda();

        if(result is Func<object> f) 
        {
          var result1 =  f.Invoke();
          if(result1 is string) return "'" + result1 + "'";
          if(result1 is bool b) return b ? "true" : "false";
          return result1.ToString();
        }
      }

      if (predicate is BinaryExpression be)
      {
        if (CsAndOdataOperators.ContainsKey(be.NodeType))
        {
          var l = GetFilterExpr(be.Left);
          var r = GetFilterExpr(be.Right);
          return l + " " + CsAndOdataOperators[be.NodeType] + " " + r;
        }
      }

      if (predicate is ConstantExpression ce)
      {
        var value = Expression.Lambda(predicate).Compile().DynamicInvoke();
        Console.WriteLine(value);
        if(value is string) return $"'{value}'";
        return value.ToString();
      }

      if (predicate is MemberExpression ma)
      {
        if (ma.Expression.NodeType == ExpressionType.Parameter)
        {
          return ma.Member.Name;
        }

        if (ma.Expression.NodeType == ExpressionType.Constant) {
          var inner = (ConstantExpression)ma.Expression;
          var value = (ma.Member as FieldInfo).GetValue(inner.Value);
          if(value is string) return $"'{value}'";
          return value.ToString();
        }

        return GetFilterExpr(ma.Expression);
      }

      throw new ArgumentException("Predicate could not be compiled to OData expresion");
    }

    private static Dictionary<ExpressionType, string> CsAndOdataOperators = new Dictionary<ExpressionType, string>() {
      { ExpressionType.Add, "add" },
      { ExpressionType.AddChecked, "add" },
      { ExpressionType.And, "and" },
      { ExpressionType.AndAlso, "and" },
      { ExpressionType.Decrement, "min" },
      { ExpressionType.Divide, "div" },
      { ExpressionType.Equal, "eq" },
      { ExpressionType.GreaterThan, "gt" },
      { ExpressionType.GreaterThanOrEqual, "ge" },
      { ExpressionType.Increment, "add" },
      { ExpressionType.LessThan, "lt" },
      { ExpressionType.LessThanOrEqual, "le" },
      { ExpressionType.Multiply, "mul" },
      { ExpressionType.NotEqual, "nq" },
      { ExpressionType.Or, "or" },
      { ExpressionType.Subtract, "min" },
    };
  }
}