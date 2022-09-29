﻿using System;
using System.Linq;
using Core.DomainModel.ItContract;
using Core.DomainServices.Queries.SystemUsage;
using Tests.Toolkit.Patterns;
using Xunit;
using static Tests.Unit.Presentation.Web.Helpers.ItSystemUsageOverviewReadModelTestData;

namespace Tests.Unit.Presentation.Web.DomainServices.SystemUsage
{
    public class QueryReadModelsWhichShouldBecomeActiveTest : WithAutoFixture
    {
        private readonly DateTime _now;
        private readonly QueryReadModelsWhichShouldBecomeActive _sut;

        public QueryReadModelsWhichShouldBecomeActiveTest()
        {
            this._now = DateTime.UtcNow.Date;
            _sut = new QueryReadModelsWhichShouldBecomeActive(_now);
        }

        [Fact]
        public void Apply_Includes_Systems_Which_Are_Currently_Inactive_But_Should_Be_Active()
        {
            var mainContract = new ItContractItSystemUsage
            {
                ItContract = new ItContract
                {
                    Terminated = _now.Date.AddDays(-1)
                }
            };

            //Arrange
            var excludedSinceReadModelIsCurrentlyActive = CreateReadModel(true, null, null, null);
            var excludedSinceConcludedHasNotYetPassed = CreateReadModel(false, _now.Date.AddDays(1), null, null);
            var includedSinceConcludedHasPassedAndNoExpiration = CreateReadModel(false, _now.Date, null, null);
            var includedSinceConcludedHasPassedAndNotExpired = CreateReadModel(false, _now.Date, _now.Date.AddDays(1), null);
            var excludedSinceConcludedSinceExpired = CreateReadModel(false, _now.Date.AddDays(-2), _now.Date.AddDays(-1), null);
            var excludedSinceConcludedSinceContractExpired = CreateReadModel(false, _now.Date, _now.Date.AddDays(1), mainContract);

            var input = new[]
            {
                excludedSinceReadModelIsCurrentlyActive,
                excludedSinceConcludedHasNotYetPassed,
                includedSinceConcludedHasPassedAndNoExpiration,
                includedSinceConcludedHasPassedAndNotExpired,
                excludedSinceConcludedSinceExpired,
                excludedSinceConcludedSinceContractExpired
            };

            //Act
            var result = _sut.Apply(input.AsQueryable()).ToList();

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(includedSinceConcludedHasPassedAndNoExpiration, result);
            Assert.Contains(includedSinceConcludedHasPassedAndNotExpired, result);
        }
    }
}
