﻿using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.External.V2.Types.Shared;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure
{
    public class SimpleLinkNameMaxLengthAttributeTest
    {
        [Theory]
        [InlineData(5, "12345", true)]
        [InlineData(5, "123456", false)]
        [InlineData(5, "1234", true)]
        [InlineData(5, "", true)]
        public void IsValid_Returns(int maxLength, string input, bool expectedResult)
        {
            //Arrange
            var sut = new SimpleLinkNameMaxLengthAttribute(maxLength);

            //Act
            var isValid = sut.IsValid(new SimpleLinkDTO { Name = input });

            //Assert
            Assert.Equal(expectedResult, isValid);
        }
    }
}
