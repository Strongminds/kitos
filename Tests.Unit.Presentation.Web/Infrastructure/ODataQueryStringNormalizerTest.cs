using Presentation.Web.Infrastructure.OData;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure
{
    public class ODataQueryStringNormalizerTest
    {
        [Fact]
        public void NormalizeNestedQueryOptions_ConvertsNestedAmpersandsToSemicolons()
        {
            const string input = "?$expand=Usages($select=OrganizationId&$expand=Organization($select=Uuid,Name)&$filter=OrganizationId eq 42)&$skip=0&$top=100&$count=true";
            const string expected = "?$expand=Usages($select=OrganizationId;$expand=Organization($select=Uuid,Name);$filter=OrganizationId eq 42)&$skip=0&$top=100&$count=true";

            var result = ODataQueryStringNormalizer.NormalizeNestedQueryOptions(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeNestedQueryOptions_LeavesTopLevelQuerySeparatorsUntouched()
        {
            const string input = "?$expand=BusinessType($select=Name),Organization($select=Id,Name)&$skip=0&$top=100&$count=true";

            var result = ODataQueryStringNormalizer.NormalizeNestedQueryOptions(input);

            Assert.Equal(input, result);
        }
    }
}
