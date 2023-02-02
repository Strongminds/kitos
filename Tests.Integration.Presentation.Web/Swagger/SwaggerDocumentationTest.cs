﻿using System;
using System.Net;
using System.Threading.Tasks;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.XUnit;
using Xunit;

namespace Tests.Integration.Presentation.Web.Swagger
{
    [Collection(nameof(SequentialTestGroup))]
    public class SwaggerDocumentationTest
    {
        public class SwaggerDoc
        {
            public string Swagger { get; set; }
            public string Host { get; set; }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Can_Load_Swagger_Doc(int version)
        {
            //Arrange
            var url = CreateUrl(version);

            //Act
            using var result = await HttpApi.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var doc = await result.ReadResponseBodyAsAsync<SwaggerDoc>();
            Assert.Equal("2.0", doc.Swagger);
            Assert.Equal(url.Authority, doc.Host);
        }

        [Fact]
        public async Task Swagger_Doc_V1_Contains_Correct_Types()
        {
            //Arrange
            var url = CreateUrl(1);

            //Act
            using var result = await HttpApi.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var body = await result.Content.ReadAsStringAsync();
            Assert.DoesNotContain("ContractGeneralDataWriteRequestDTO", body);
            Assert.Contains("Advice", body);
            Assert.Contains("OptionDTO", body);
        }

        [Fact]
        public async Task Swagger_Doc_V2_Contains_Correct_Types()
        {
            //Arrange
            var url = CreateUrl(2);

            //Act
            using var result = await HttpApi.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var body = await result.Content.ReadAsStringAsync();
            Assert.Contains("ContractGeneralDataWriteRequestDTO", body);
            Assert.DoesNotContain("Advice", body);
            Assert.DoesNotContain("OptionDTO", body);
        }

        private Uri CreateUrl(int apiVersion)
        {
            return TestEnvironment.CreateUrl($"/swagger/docs/{apiVersion}");
        }
    }
}
