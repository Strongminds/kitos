using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.UIConfiguration;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.UIModuleConfiguration
{
    public class UIModuleCustomizationTest
    {
        [Fact]
        public void UpdateConfigurationNodes_Returns_BadInput_When_Nodes_Are_Null()
        {
            var sut = new UIModuleCustomization();

            var result = sut.UpdateConfigurationNodes(null);

            Assert.True(result.HasValue);
            Assert.Equal(OperationFailure.BadInput, result.Value.FailureType);
        }

        [Fact]
        public void UpdateConfigurationNodes_Returns_BadInput_When_Key_Is_Invalid()
        {
            var sut = new UIModuleCustomization();
            var nodes = new List<CustomizedUINode> { new() { Key = "invalid key", Enabled = true } };

            var result = sut.UpdateConfigurationNodes(nodes);

            Assert.True(result.HasValue);
            Assert.Equal(OperationFailure.BadInput, result.Value.FailureType);
        }

        [Fact]
        public void UpdateConfigurationNodes_Returns_BadInput_When_Keys_Are_Duplicate()
        {
            var sut = new UIModuleCustomization();
            var nodes = new List<CustomizedUINode>
            {
                new() { Key = "Valid.Key", Enabled = true },
                new() { Key = "Valid.Key", Enabled = false }
            };

            var result = sut.UpdateConfigurationNodes(nodes);

            Assert.True(result.HasValue);
            Assert.Equal(OperationFailure.BadInput, result.Value.FailureType);
        }

        [Fact]
        public void UpdateConfigurationNodes_Updates_Nodes_When_Input_Is_Valid()
        {
            var sut = new UIModuleCustomization
            {
                Nodes = new List<CustomizedUINode> { new() { Key = "Old.Key", Enabled = false } }
            };
            var nodes = new List<CustomizedUINode>
            {
                new() { Key = "New.Key", Enabled = true }
            };

            var result = sut.UpdateConfigurationNodes(nodes);

            Assert.False(result.HasValue);
            Assert.Single(sut.Nodes);
            Assert.Contains(sut.Nodes, x => x.Key == "New.Key" && x.Enabled);
        }
    }
}
