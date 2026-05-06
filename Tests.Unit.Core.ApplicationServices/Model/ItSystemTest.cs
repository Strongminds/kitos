using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
using System.Linq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model
{
    public class ItSystemTest : WithAutoFixture
    {
        private readonly ItSystem _sut;

        public ItSystemTest()
        {
            _sut = new ItSystem();
        }

        [Fact]
        public void SetLicensingAndCodeModels_Returns_Conflict_If_Proprietary_And_Other_Are_Present()
        {
            var models = new[] { LicensingAndCodeModel.OpenSource, LicensingAndCodeModel.Proprietary };

            var result = _sut.SetLicensingAndCodeModels(models);

            Assert.True(result.HasValue);
            Assert.Equal(OperationFailure.Conflict, result.Value.FailureType);
            Assert.Empty(_sut.LicensingAndCodeModels);
        }

        [Fact]
        public void SetLicensingAndCodeModels_Does_Not_Add_Duplicates()
        {
            var values = new[] { LicensingAndCodeModel.OpenSource, LicensingAndCodeModel.Freeware, LicensingAndCodeModel.Freeware };
            var uniqueValuesCount = values.ToHashSet().Count;
            _sut.SetLicensingAndCodeModels(values);

            var result = _sut.SetLicensingAndCodeModels(values);

            Assert.True(result.IsNone);
            Assert.Equal(uniqueValuesCount, _sut.LicensingAndCodeModels.Count);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData(ArchiveDutyRecommendationTypes.Undecided, null)]
        [InlineData(ArchiveDutyRecommendationTypes.Undecided, "")]
        [InlineData(ArchiveDutyRecommendationTypes.B, "")]
        [InlineData(ArchiveDutyRecommendationTypes.B, "something")]
        [InlineData(ArchiveDutyRecommendationTypes.B, null)]
        [InlineData(ArchiveDutyRecommendationTypes.K, "")]
        [InlineData(ArchiveDutyRecommendationTypes.K, null)]
        [InlineData(ArchiveDutyRecommendationTypes.KB, null)]
        [InlineData(ArchiveDutyRecommendationTypes.KB, "")]
        [InlineData(ArchiveDutyRecommendationTypes.KD, null)]
        [InlineData(ArchiveDutyRecommendationTypes.KD, "")]
        [InlineData(ArchiveDutyRecommendationTypes.BK, null)]
        [InlineData(ArchiveDutyRecommendationTypes.BK, "")]
        public void Can_UpdateRecommendedArchiveDuty(ArchiveDutyRecommendationTypes? recommendation, string comment)
        {
            //Act
            var error = _sut.UpdateRecommendedArchiveDuty(recommendation, comment);

            //Assert
            Assert.False(error.HasValue);
            Assert.Equal(recommendation, _sut.ArchiveDuty);
            Assert.Equal(comment, _sut.ArchiveDutyComment);
        }

        [Theory]
        [InlineData(null, "something")]
        [InlineData(ArchiveDutyRecommendationTypes.Undecided, "something")]
        public void Cannot_UpdateRecommendedArchiveDuty_With_Comment_If_Undecided_OrNull(ArchiveDutyRecommendationTypes? recommendation, string comment)
        {
            //Act
            var error = _sut.UpdateRecommendedArchiveDuty(recommendation, comment);

            //Assert
            Assert.True(error.HasValue);
            Assert.Equal(OperationFailure.BadInput,error.Value.FailureType);
        }
    }
}
