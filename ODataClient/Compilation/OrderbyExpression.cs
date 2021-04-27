using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hoppinger.OdataClient.Compilation
{
  public static class OrderbyExpression
  {
    public static string Compile<T>(LambdaExpression expr) {
      if(expr.Body is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
      {
        return Compile<T>(Expression.Lambda(
            u.Operand,
            new List<ParameterExpression>()
        ));
      }
      
      if(!IsParamExpression(expr.Body)) throw new Exception($"Expression {expr.Body.NodeType} cannot be compiled to an OData orderby expression as it doesn't act on the parameter");
      if(expr.Body is MemberExpression ma) return String.Join("/", GetMemberPath(ma, new List<string>()));
      throw new Exception($"Expression {expr.Body.NodeType} cannot be compiled to an OData orderby expression");
    }

    private static bool IsParamExpression(Expression e) => e is ParameterExpression || (e is MemberExpression m && IsParamExpression(m.Expression));

    private static IEnumerable<string> GetMemberPath(MemberExpression ma, IEnumerable<string> path) => ma.Expression is MemberExpression ma1 ? GetMemberPath(ma1, path.Prepend(ma.Member.Name)) : path.Prepend(ma.Member.Name);
  }
}