﻿using System.Linq;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Queries;
using Tests.Unit.Presentation.Web.Helpers;
using Xunit;

namespace Tests.Unit.Presentation.Web.DomainServices
{
    public class QueryByPartOfNameTest : WithAutoFixture
    {
        [Fact]
        public void Apply_Returns_Results_Where_NameContent_Is_Found()
        {
            //Arrange
            var sharedContent = A<string>();
            var included1 = CreateItSystem(sharedContent + A<string>());
            var included2 = CreateItSystem(A<string>() + sharedContent + A<string>());
            var included3 = CreateItSystem(A<string>() + sharedContent);
            var excluded = CreateItSystem(A<string>());
            var input = new[] { included1, included2, included3, excluded }.AsQueryable();
            var sut = new QueryByPartOfName<ItSystem>(sharedContent);

            //Act
            var result = sut.Apply(input).ToList();

            //Assert
            Assert.Equal(3, result.Count);
            Assert.True(result.Contains(included1));
            Assert.True(result.Contains(included2));
            Assert.True(result.Contains(included3));
        }

        private ItSystem CreateItSystem(string name)
        {
            return new ItSystem
            {
                Id = A<int>(),
                Name = name
            };
        }
    }
}
