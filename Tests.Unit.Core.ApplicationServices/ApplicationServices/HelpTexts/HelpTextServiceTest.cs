﻿using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.HelpTexts;
using Core.ApplicationServices.Model.HelpTexts;
using Core.DomainModel;
using Core.DomainServices;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.HelpTexts
{
    public class HelpTextServiceTest: WithAutoFixture
    {
        private readonly HelpTextService _sut;
        private readonly Mock<IGenericRepository<HelpText>> _helpTextsRepository;
        private readonly Mock<IOrganizationalUserContext> _userContext;

        public HelpTextServiceTest()
        {
            _helpTextsRepository = new Mock<IGenericRepository<HelpText>>();
            _userContext = new Mock<IOrganizationalUserContext>();
            _sut = new HelpTextService(_helpTextsRepository.Object, _userContext.Object);
        }

        [Fact]
        public void Can_Get_Help_Texts()
        {
            var expected = SetupRepositoryReturnsOne();
            var result = _sut.GetHelpTexts();

            Assert.True(result.Ok);
            var helpText = result.Value.FirstOrDefault();
            Assert.NotNull(helpText);
            Assert.Equivalent(expected, helpText);
        }

        [Fact]
        public void Create_Help_Text_Returns_Forbidden_If_Not_Global_Admin()
        {
            SetupIsNotGlobalAdmin();

            var parameters = A<HelpTextCreateParameters>();

            var result = _sut.CreateHelpText(parameters);

            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void Can_Create_Help_Text()
        {
            SetupIsGlobalAdmin();

            var parameters = A<HelpTextCreateParameters>();

            var result = _sut.CreateHelpText(parameters);

            Assert.True(result.Ok);
            var helpText = result.Value;
            Assert.Equal(parameters.Description, helpText.Description);
            Assert.Equal(parameters.Title, helpText.Title);
            Assert.Equal(parameters.Key, helpText.Key);
        }

        [Fact]
        public void Can_Delete_Help_Text()
        {
            var existing = SetupRepositoryReturnsOne();
            SetupIsGlobalAdmin();

            var errorMaybe = _sut.DeleteHelpText(existing.Key);

            Assert.False(errorMaybe.HasValue);
            _helpTextsRepository.Verify(_ => _.Delete(existing));

        }

        [Fact]
        public void Can_Patch_Help_Text_Except_Key()
        {
            var existing = SetupRepositoryReturnsOne();
            SetupIsGlobalAdmin();
            var parameters = new HelpTextUpdateParameters()
            {
                Key = existing.Key,
                Title = A<Maybe<string>>().AsChangedValue(),
                Description = A<Maybe<string>>().AsChangedValue()
            };

            var result = _sut.PatchHelpText(parameters);

            Assert.True(result.Ok);
            var helpText = result.Value;
            Assert.Equal(parameters.Description.NewValue.Value, helpText.Description);
            Assert.Equal(parameters.Title.NewValue.Value, helpText.Title);
            Assert.Equal(existing.Key, helpText.Key);
        }

        private HelpText SetupRepositoryReturnsOne()
        {
            var existing = new HelpText()
            {
                Description = A<string>(),
                Key = A<string>(),
                Title = A<string>()
            };
            _helpTextsRepository.Setup(_ => _.AsQueryable()).Returns(new List<HelpText>() { existing }.AsQueryable());
            return existing;
        }

        private void SetupIsGlobalAdmin()
        {
            _userContext.Setup(_ => _.IsGlobalAdmin()).Returns(true);
        }

        private void SetupIsNotGlobalAdmin()
        {
            _userContext.Setup(_ => _.IsGlobalAdmin()).Returns(false);
        }


    }
}
