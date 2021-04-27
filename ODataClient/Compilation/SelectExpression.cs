using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hoppinger.OdataClient.Compilation
{
  public static class SelectExpression
  {
    public static string Compile<T>(LambdaExpression selector) {
      return String.Join(',', GetSelectedProps(selector.Body, new HashSet<string>(), typeof(T)));
    }

    private static HashSet<string> GetSelectedProps(Expression expr, HashSet<string> prevvProps, Type type)
    {
      if(expr is ParameterExpression p) return GetParameterExpression(p, prevvProps, type);

      if (expr is MemberExpression me) return GetMemberExpression(me, prevvProps, type);

      if (expr is NewExpression ne) return GetNewExpression(ne, prevvProps, type);

      if (expr is MemberInitExpression mi) return GetMemberInitExpression(mi, prevvProps, type);

      if (expr is BinaryExpression be) return GetBinaryExpression(be, prevvProps, type);

      if(expr is ConstantExpression) return prevvProps;

      if(expr is MethodCallExpression c) return GetMethodCallExpression(c, prevvProps, type);

      if(expr is ListInitExpression l) return GetListInitExpression(l, prevvProps, type);

      if(expr is ConditionalExpression cl) return GetConditionalExpression(cl, prevvProps, type);

      if(expr is DefaultExpression) return prevvProps;

      if(expr is IndexExpression ie) return GetIndexExpression(ie, prevvProps, type);

      if(expr is UnaryExpression u) return GetUnaryExpression(u, prevvProps, type);

      if(expr is LambdaExpression la) return GetLambdaExpression(la, prevvProps, type);

      throw new Exception("Could not compile expression " + expr.NodeType);
    }

    private static HashSet<string> GetLambdaExpression(LambdaExpression la, HashSet<string> prevvProps, Type type)
    {
      var body = GetSelectedProps(la.Body, prevvProps, type);
      return prevvProps.Concat(body).ToHashSet();
    }

    private static HashSet<string> GetUnaryExpression(UnaryExpression u, HashSet<string> prevvProps, Type type)
    {
      var obj = GetSelectedProps(u.Operand, prevvProps, type);
      return prevvProps.Concat(obj).ToHashSet();
    }

    private static HashSet<string> GetIndexExpression(IndexExpression ie, HashSet<string> prevvProps, Type type)
    {
      var arg = GetSelectedProps(ie.Arguments.First(), prevvProps, type);
      var obj = GetSelectedProps(ie.Object, prevvProps, type);
      return prevvProps.Concat(arg).Concat(obj).ToHashSet();
    }
    private static HashSet<string> GetConditionalExpression(ConditionalExpression cl, HashSet<string> prevvProps, Type type)
    {
      var test = GetSelectedProps(cl.Test, prevvProps, type);
      var ifTrue = GetSelectedProps(cl.IfTrue, prevvProps, type);
      var ifFalse = GetSelectedProps(cl.IfFalse, prevvProps, type);
      return prevvProps.Concat(test).Concat(ifTrue).Concat(ifFalse).ToHashSet();
    }

    private static HashSet<string> GetListInitExpression(ListInitExpression l, HashSet<string> prevvProps, Type type)
    {
      var inits = l.Initializers.SelectMany(i => i.Arguments).SelectMany(i => GetSelectedProps(i, prevvProps, type));
      return prevvProps.Concat(inits).ToHashSet();
    }

    private static HashSet<string> GetBinaryExpression(BinaryExpression be, HashSet<string> prevvProps, Type type)
    {
      var leftProps = GetSelectedProps(be.Left, new HashSet<string>(), type);
      var rigthProps = GetSelectedProps(be.Right, new HashSet<string>(), type);
      return prevvProps.Concat(leftProps).Concat(rigthProps).ToHashSet();
    }

    private static HashSet<string> GetMethodCallExpression(MethodCallExpression c, HashSet<string> prevvProps, Type type)
    {
      var argsProps = c.Arguments.SelectMany(a => GetSelectedProps(a, prevvProps, type)).ToHashSet();
      if(c.Object is null) return prevvProps.Concat(argsProps).ToHashSet();
      var obj = GetSelectedProps(c.Object, argsProps, type);
      return prevvProps.Concat(obj).ToHashSet();
    }

    private static HashSet<string> GetMemberInitExpression(MemberInitExpression mi, HashSet<string> prevvProps, Type type)
    {
      var memberProps = mi.Bindings.Select(mb => mb.Member.Name);
      var newProps = GetSelectedProps(mi.NewExpression, new HashSet<string>(), type);
      return prevvProps.Concat(memberProps).Concat(newProps).ToHashSet();
    }

    private static HashSet<string> GetNewExpression(NewExpression ne, HashSet<string> prevvProps, Type type)
    {
      var args = ne.Arguments.SelectMany(arg => GetSelectedProps(arg, new HashSet<string>(), type));
      return prevvProps.Concat(args).ToHashSet();
    }

    private static HashSet<string> GetMemberExpression(MemberExpression me, HashSet<string> prevvProps, Type type)
    {
      // don't add magic props like .Day of a DateTime to the selected props
      if(me.Expression.Type == typeof(DateTime)) return prevvProps;
      return prevvProps.Append(String.Join('/', GetMemberPath(me, new List<string>()))).ToHashSet();
    }

    private static IEnumerable<string> GetMemberPath(MemberExpression ma, IEnumerable<string> path) => ma.Expression is MemberExpression ma1 ? GetMemberPath(ma1, path.Prepend(ma.Member.Name)) : path.Prepend(ma.Member.Name);

    private static HashSet<string> GetParameterExpression(ParameterExpression p, HashSet<string> prevvProps, Type type)
    {
      if(p.Type.Equals(type)) 
        return prevvProps.Concat(type.GetProperties().Select(x => x.Name)).ToHashSet();

      return prevvProps;
    }

  }
}