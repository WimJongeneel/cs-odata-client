using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OdataClient
{
  public static class QuerySelectExtensions
  {
    public static Query<R> Select<O, R>(this Query<O> query, Expression<Func<O, R>> selector)
      where O : class
      where R : class
    {
      var props = GetSelectedProps(selector.Body, new HashSet<string>(), typeof(O));
      var newQuery = new Query<R>();
      newQuery.Select = props.Aggregate((p1, p2) => $"{p1},{p2}");
      newQuery.Filter = query.Filter;
      return newQuery;
    }

    private static HashSet<string> GetSelectedProps(Expression selector, HashSet<string> prevvProps, Type type)
    {
      Console.WriteLine("ype: " + selector.NodeType);

      if(selector is ParameterExpression)
      {
        if(selector.Type.Equals(type)) 
          return prevvProps.Concat(type.GetProperties().Select(p => p.Name)).ToHashSet();
      }

      if (selector is MemberExpression me)
      {
        prevvProps.Add(me.Member.Name);
        return prevvProps;
      }

      if (selector is NewExpression ne)
      {
        var args = ne.Arguments.Select(arg => GetSelectedProps(arg, new HashSet<string>(), type));
        return prevvProps.Concat(args.Aggregate((a1, a2) => a1.Concat(a2).ToHashSet())).ToHashSet();
      }

      if (selector is MemberInitExpression mi)
      {
        var memberProps = mi.Bindings.Select(mb => mb.Member.Name);
        var newProps = GetSelectedProps(mi.NewExpression, new HashSet<string>(), type);
        return prevvProps.Concat(memberProps).Concat(newProps).ToHashSet();
      }

      if (selector is BinaryExpression be)
      {
        var leftProps = GetSelectedProps(be.Left, new HashSet<string>(), type);
        var rigthProps = GetSelectedProps(be.Right, new HashSet<string>(), type);
        return prevvProps.Concat(leftProps).Concat(rigthProps).ToHashSet();
      }

     throw new Exception("Could not compile expression " + selector.NodeType);
    }
  }
}