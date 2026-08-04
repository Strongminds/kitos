using System;
using System.Text;

namespace Presentation.Web.Infrastructure.OData
{
    public static class ODataQueryStringNormalizer
    {
        public static string NormalizeNestedQueryOptions(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString) || queryString.Contains("&$") == false)
            {
                return queryString;
            }

            var builder = new StringBuilder(queryString.Length);
            var depth = 0;

            for (var i = 0; i < queryString.Length; i++)
            {
                var current = queryString[i];

                switch (current)
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth = Math.Max(depth - 1, 0);
                        break;
                }

                if (current == '&' && depth > 0 && IsNestedSystemQueryOption(queryString, i + 1))
                {
                    builder.Append(';');
                    continue;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static bool IsNestedSystemQueryOption(string queryString, int index)
        {
            return index < queryString.Length && queryString[index] == '$';
        }
    }
}
