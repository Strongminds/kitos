using System;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Models.V2
{
    public class ItSystemArchiveWriteRequestMapperTest : WithAutoFixture
    {
        private readonly ItSystemArchiveWriteRequestMapper _sut;

        public ItSystemArchiveWriteRequestMapperTest()
        {
            _sut = new ItSystemArchiveWriteRequestMapper();
        }

        [Fact]
        public void FromRequest_Maps_All_Properties()
        {
            // Arrange
            var archivingDate = A<DateTime>();
            var referenceName = A<string>();
            var note = A<string>();
            var request = new CreateItSystemUsageArchiveRequestDTO
            {
                ArchivingDate = archivingDate,
                ReferenceName = referenceName,
                Note = note
            };

            // Act
            var result = _sut.FromRequest(request);

            // Assert
            Assert.Equal(archivingDate, result.ArchivingDate);
            Assert.Equal(referenceName, result.ReferenceName);
            Assert.Equal(note, result.Note);
        }
    }
}
