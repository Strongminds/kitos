using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.UriParser;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Presentation.Web.OData
{
    public class CaseInsensitiveContainsFilterBinder: FilterBinder
    {
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;
        private static readonly Expression OrdinalIgnoreCase = Expression.Constant(StringComparison.OrdinalIgnoreCase);
        private const string ContainsFunctionName = "contains";

        public override Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            if (node.Name is ContainsFunctionName)
            {
                var parameters = node.Parameters.ToList();
                if (parameters[0].GetType() == typeof(string) && parameters[1].GetType() == typeof(string))
                {
                    var left = Bind(parameters[0], context);
                    var right = Bind(parameters[1], context);
                    return Expression.Call(left, ContainsMethod, right, OrdinalIgnoreCase);
                };
            }

            return base.BindSingleValueFunctionCallNode(node, context);
        }

    }
}
