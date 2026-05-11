using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.UriParser;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Presentation.Web.Infrastructure.OData
{
    public class CaseInsensitiveContainsFilterBinder: FilterBinder
    {
        private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;
        private static readonly Expression OrdinalIgnoreCase = Expression.Constant(StringComparison.OrdinalIgnoreCase);
        private const string ContainsFunctionName = "contains";

        public override Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            return node.Name is ContainsFunctionName
                ? CallCaseInsensitiveContainsExpression(node, context)
                : base.BindSingleValueFunctionCallNode(node, context);
        }

        private Expression CallCaseInsensitiveContainsExpression(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            var parameters = node.Parameters.ToList();
            var left = Bind(parameters[0], context);
            var right = Bind(parameters[1], context);

            Expression containsCall = Expression.Call(left, ContainsMethod, right, OrdinalIgnoreCase);

            // Guard against NullReferenceException when the property is null during in-memory filter
            // evaluation (e.g. after ToList()). OData semantics: contains(null, 'x') = false.
            if (!left.Type.IsValueType)
            {
                containsCall = Expression.Condition(
                    Expression.Equal(left, Expression.Default(left.Type)),
                    Expression.Constant(false),
                    containsCall);
            }

            return containsCall;
        }

    }
}
