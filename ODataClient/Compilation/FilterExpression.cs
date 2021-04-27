using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Hoppinger.OdataClient.Compilation
{
  public static class FilterExpression
  {
    public static string Compile(LambdaExpression expr) => GetExpression(expr.Body, true);

    private static string GetExpression(Expression expr, bool root = false)
    {
      if (expr is InvocationExpression iv) return GetInvocationExpression(iv);

      if (expr is BinaryExpression be) return GetBinaryExpression(be, root);

      if (expr is ConstantExpression ce) return GetConstantExpression(ce);

      if (expr is MemberExpression ma) return GetMemberExpression(ma);

      if (expr is MethodCallExpression mc) return GetMethodCallExpression(mc);

      if(expr is UnaryExpression n) return GetUnaryExpression(n);

      throw new ArgumentException($"Expresion type {expr.NodeType} can not be translated to an OData filter expression.");
    }

    private static string GetUnaryExpression(UnaryExpression n)
    {
      if(CsAndOdataOperators.ContainsKey(n.NodeType)) return $"{CsAndOdataOperators[n.NodeType]}{GetExpression(n.Operand)}";
      // C# insert converts around math operators which aren't needed in OData
      if(n.NodeType == ExpressionType.Convert && n.Type == typeof(double)) return GetExpression(n.Operand);
      if(n.NodeType == ExpressionType.Convert && n.Type == typeof(decimal)) return GetExpression(n.Operand);
      if(n.NodeType == ExpressionType.Convert && n.Type == typeof(object)) return GetExpression(n.Operand);
      throw new ArgumentException($"UnaryExpression type {n.NodeType} can not be translated to an OData filter expression.");
    }

    private static string GetInvocationExpression(InvocationExpression iv)
    {
        var lambda = Expression.Lambda<Func<object>>(iv.Expression).Compile();
        var result = lambda();

        if(result is Func<object> f) 
        {
          var value = f.Invoke();
          return FormatValue(value);
        }

        throw new ArgumentException($"Invocation didn't result in a value"); 
    }

    private static string GetConstantExpression(ConstantExpression ce)
    {
      return FormatValue(ce.Value);
    }

    private static string GetBinaryExpression(BinaryExpression be, bool root)
    {
        // compile the addition of strings to the concat function 
        if((be.NodeType == ExpressionType.AddChecked || be.NodeType == ExpressionType.Add) && be.Type == typeof(string))
        {
          return $"concat({GetExpression(be.Left)}, {GetExpression(be.Right)})";
        }
   
        if (CsAndOdataOperators.ContainsKey(be.NodeType))
        {
          var l = GetExpression(be.Left);
          var r = GetExpression(be.Right);
          if(root) return $"{l} {CsAndOdataOperators[be.NodeType]} {r}";
          return $"({l} {CsAndOdataOperators[be.NodeType]} {r})";
        }

        throw new ArgumentException($"Cannot use operator {be.NodeType} in $filter"); 
    }

    private static string GetMemberExpression(MemberExpression ma)
    {
      if(ma.Expression is null)
      {
        // we are accessing a static property
        var value = Expression.Lambda(ma).Compile().DynamicInvoke();
        return FormatValue(value);
      }

      if (ma.Expression is ConstantExpression cs) {
        var value = (ma.Member as FieldInfo).GetValue(cs.Value);
        return FormatValue(value);
      }


      if(IsParamExpression(ma.Expression))
      {
        // for magic get accessors that are function in odata
        var key = $"{ma.Expression.Type.Name}.{ma.Member.Name}";
        if(CsToODataFunctions.ContainsKey(key)) return $"{CsToODataFunctions[key]}({GetExpression(ma.Expression)})";

        // DateTime has many magic get accessors that don't have a matching function in odata
        if(ma.Expression is MemberExpression m && m.Type == typeof(DateTime))
        {
          throw new ArgumentException($"Cannot acces member {ma.Member.Name} of a DateTime in $filter");
        }

        return String.Join("/", GetMemberPath(ma, new List<string>()));
      }

      var value1 = Expression.Lambda(ma).Compile().DynamicInvoke();
      return FormatValue(value1);

      throw new ArgumentException($"Cannot acces member {ma.Member.Name} in $filter");
    }

    private static string GetMethodCallExpression(MethodCallExpression mc)
    {
      if(IsParamExpression(mc.Object) || mc.Arguments.Any(IsParamExpression)) {
        // substringof is special as it accepts its arguments in reversed order
        if(mc.Method.Name == "Contains") return $"substringof({GetExpression(mc.Arguments.Single())}, {GetExpression(mc.Object)})";

        // the arguments of the odata function, including the instance object (if not a static method call)
        var args = String.Join(", ", (mc.Object is null ? mc.Arguments : mc.Arguments.Prepend(mc.Object)).Select(x => GetExpression(x)));
        // when static, the name of the class. when not static, the name of the type of the instance
        var className = mc.Object is null ? mc.Method.DeclaringType.Name : mc.Object.Type.Name;
        var key = $"{className}.{mc.Method.Name}";
        if(CsToODataFunctions.ContainsKey(key)) return $"{CsToODataFunctions[key]}({args})";        
        throw new ArgumentException($"Method {mc.Method.Name} on type {className} not supported in OData");
      }

      var value = Expression.Lambda(mc).Compile().DynamicInvoke();
      return FormatValue(value);
    }

    private static string FormatValue(object value)
    {
      if(value is string || value is char) return $"'{value}'";
      if(value is bool b) return b ? "true" : "false";
      if(value is DateTime d) return $"'{d.ToString("o")}'";
      if(value is decimal c) return c.ToString(new CultureInfo("en-US", false));
      if(value is float f) return f.ToString(new CultureInfo("en-US", false));
      if(value is double q) return q.ToString(new CultureInfo("en-US", false));
      return value is null ? "null" : value.ToString();
    }

    private static bool IsParamExpression(Expression e) => 
      e is ParameterExpression 
      || (e is MemberExpression m && IsParamExpression(m.Expression)) 
      || (e is MethodCallExpression n && IsParamExpression(n.Object))
      || (e is BinaryExpression b && (IsParamExpression(b.Left) || IsParamExpression(b.Left)))
      || (e is UnaryExpression u && IsParamExpression(u.Operand));

    private static IEnumerable<string> GetMemberPath(MemberExpression ma, IEnumerable<string> path) => ma.Expression is MemberExpression ma1 ? GetMemberPath(ma1, path.Prepend(ma.Member.Name)) : path.Prepend(ma.Member.Name);

    private static Dictionary<string, string> CsToODataFunctions = new Dictionary<string, string>()
    {
      {"String.ToLower", "tolower"},
      {"String.ToLowerInvariant", "tolower"},
      {"String.ToUpper", "toupper"},
      {"String.ToUpperInvariant", "ToUpperInvariant"},
      {"String.Trim", "trim"},
      {"String.Length", "length"},
      {"String.Substring", "substring"},
      {"String.StartsWith", "startswith"},
      {"String.EndsWith", "endswith"},
      {"String.IndexOf", "indexof"},
      {"String.Replace", "replace"},
      {"Math.Round", "round"},
      {"Math.Floor", "floor"},
      {"Math.Ceiling", "ceiling"},
      {"DateTime.Day", "day"},
      {"DateTime.Hour", "hour"},
      {"DateTime.Minute", "minute"},
      {"DateTime.Month", "month"},
      {"DateTime.Second", "second"},
      {"DateTime.Year", "year"},
    };

    private static Dictionary<ExpressionType, string> CsAndOdataOperators = new Dictionary<ExpressionType, string>()
    {
      // simple algabra
      { ExpressionType.Add, "add" },
      { ExpressionType.AddChecked, "add" },
      { ExpressionType.Subtract, "min" },
      { ExpressionType.SubtractChecked, "min" },
      { ExpressionType.Divide, "div" },
      { ExpressionType.Multiply, "mul" },
      { ExpressionType.MultiplyChecked, "mul" },
      { ExpressionType.Modulo, "mod" },

      //  -- and ++
      { ExpressionType.Decrement, "min 1" },
      { ExpressionType.Increment, "add 1" },

      // negation of numbers
      { ExpressionType.Negate, "-"},
      { ExpressionType.NegateChecked, "-"},
      { ExpressionType.UnaryPlus, "+"},

      // && and ||
      { ExpressionType.Or, "or" },
      { ExpressionType.And, "and" },
      { ExpressionType.AndAlso, "and" },
      { ExpressionType.OrElse, "or" },

      // equality
      { ExpressionType.Equal, "eq" },
      { ExpressionType.NotEqual, "ne" },
      { ExpressionType.GreaterThan, "gt" },
      { ExpressionType.GreaterThanOrEqual, "ge" },
      { ExpressionType.LessThan, "lt" },
      { ExpressionType.LessThanOrEqual, "le" },

      // !
      { ExpressionType.Not, "not "},
    };
  }
}