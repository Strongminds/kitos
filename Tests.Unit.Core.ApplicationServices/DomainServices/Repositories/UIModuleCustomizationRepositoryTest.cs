using Core.DomainModel.Events;
using Core.DomainModel.UIConfiguration;
using Core.DomainServices;
using Core.DomainServices.Repositories.UICustomization;
using Moq;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.DomainServices.Repositories
{
    public class UIModuleCustomizationRepositoryTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(42)]
        public void Update_Inserts_New_Modules_And_Updates_Persisted_Modules(int id)
        {
            var repository = new Mock<IGenericRepository<UIModuleCustomization>>();
            var domainEvents = new Mock<IDomainEvents>();
            var sut = new UIModuleCustomizationRepository(repository.Object,
                Mock.Of<IGenericRepository<CustomizedUINode>>(), domainEvents.Object);
            var module = new UIModuleCustomization { Id = id, Module = "ItSystemUsages" };

            sut.Update(module);

            repository.Verify(x => x.Insert(module), id == 0 ? Times.Once() : Times.Never());
            repository.Verify(x => x.Update(module), id == 0 ? Times.Never() : Times.Once());
            repository.Verify(x => x.Save(), Times.Once());
            domainEvents.Verify(x => x.Raise(It.IsAny<EntityUpdatedEvent<UIModuleCustomization>>()), Times.Once());
        }
    }
}
