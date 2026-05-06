using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
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
        public void AddLicensingAndCodeModel_Can_Add_Both_OpenSource_And_Freeware()
        {
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.OpenSource);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Freeware);

            Assert.Contains(LicensingAndCodeModel.OpenSource, _sut.LicensingAndCodeModels);
            Assert.Contains(LicensingAndCodeModel.Freeware, _sut.LicensingAndCodeModels);
        }

        [Fact]
        public void AddLicensingAndCodeModel_Cannot_Add_Duplicates()
        {
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.OpenSource);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Freeware);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.OpenSource);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Freeware);

            Assert.Equal(2, _sut.LicensingAndCodeModels.Count);
            Assert.Contains(LicensingAndCodeModel.OpenSource, _sut.LicensingAndCodeModels);
            Assert.Contains(LicensingAndCodeModel.Freeware, _sut.LicensingAndCodeModels);
        }

        [Fact]
        public void AddLicensingAndCodeModel_Adding_Proprietary_Overrides_Other_Values()
        {
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.OpenSource);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Freeware);
            Assert.Equal(2, _sut.LicensingAndCodeModels.Count);

            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Proprietary);

            Assert.Single(_sut.LicensingAndCodeModels);
            Assert.Contains(LicensingAndCodeModel.Proprietary, _sut.LicensingAndCodeModels);
        }

        [Fact]
        public void AddLicensingAndCodeModel_Cannot_Add_NonProprietary_Values_After_Proprietary()
        { 
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Proprietary);
            Assert.Single(_sut.LicensingAndCodeModels);

            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.OpenSource);
            _sut.AddLicensingAndCodeModel(LicensingAndCodeModel.Freeware);

            Assert.Single(_sut.LicensingAndCodeModels);
            Assert.Contains(LicensingAndCodeModel.Proprietary, _sut.LicensingAndCodeModels);
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
