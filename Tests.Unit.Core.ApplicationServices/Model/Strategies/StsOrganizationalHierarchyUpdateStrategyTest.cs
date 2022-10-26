﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.DomainModel.Extensions;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainModel.Organization.Strategies;
using Tests.Toolkit.Extensions;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model.Strategies
{
    public class StsOrganizationalHierarchyUpdateStrategyTest : WithAutoFixture
    {
        private readonly StsOrganizationalHierarchyUpdateStrategy _sut;
        private readonly Organization _organization;
        private int _nextOrgUnitId;

        public StsOrganizationalHierarchyUpdateStrategyTest()
        {
            _organization = new Organization();
            _sut = new StsOrganizationalHierarchyUpdateStrategy(_organization);
            _nextOrgUnitId = 0;
        }

        private int GetNewOrgUnitId() => _nextOrgUnitId++;

        private void PrepareConnectedOrganization()
        {
            _organization.StsOrganizationConnection = new StsOrganizationConnection
            {
                Connected = true,
                Organization = _organization
            };

            var organizationUnit = CreateOrganizationUnit
            (
                OrganizationUnitOrigin.STS_Organisation, new[]
                {
                    CreateOrganizationUnit(OrganizationUnitOrigin.Kitos),
                    CreateOrganizationUnit(OrganizationUnitOrigin.STS_Organisation,new []
                    {
                        CreateOrganizationUnit(OrganizationUnitOrigin.STS_Organisation)
                    }),
                    CreateOrganizationUnit(OrganizationUnitOrigin.STS_Organisation, new[]
                    {
                        CreateOrganizationUnit(OrganizationUnitOrigin.STS_Organisation),
                        CreateOrganizationUnit(OrganizationUnitOrigin.Kitos)

                    })
                });

            foreach (var unit in organizationUnit.FlattenHierarchy())
            {
                _organization.OrgUnits.Add(unit);
            }
        }

        private OrganizationUnit CreateOrganizationUnit(OrganizationUnitOrigin origin, IEnumerable<OrganizationUnit> children = null)
        {
            var unit = new OrganizationUnit
            {
                Id = GetNewOrgUnitId(),
                Name = A<string>(),
                Origin = origin,
                ExternalOriginUuid = origin == OrganizationUnitOrigin.STS_Organisation ? A<Guid>() : null,
                Organization = _organization
            };

            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Parent = unit;
                    unit.Children.Add(child);
                }
            }
            return unit;
        }

        [Fact, Description("Since it is an update strategy it is a programmers error to invoke it on an unconnected organization")]
        public void ComputeUpdate_Throws_If_Organization_Is_Not_Connected()
        {
            //Arrange
            var externalOrganizationUnit = new ExternalOrganizationUnit(A<Guid>(), A<string>(), new Dictionary<string, string>(), Array.Empty<ExternalOrganizationUnit>());

            //Act + Assert
            Assert.Throws<InvalidOperationException>(() => _sut.ComputeUpdate(externalOrganizationUnit));
        }

        [Fact, Description("Ensures that change sets that contain no changes will not impact kitos")]
        public void ComputeUpdate_Detects_No_External_Changes()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();

            var externalTree = ConvertToExternalTree(root);

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
        }

        [Fact, Description("Verifies that additions in the external hierarchy is detected correctly")]
        public void ComputeUpdate_Detects_New_OrganizationUnits()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var randomParentOfNewSubTree = root
                .FlattenHierarchy()
                .Skip(1) // Skip the root
                .Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation)
                .RandomItem();

            var expectedSubTree = CreateOrganizationUnit(
                OrganizationUnitOrigin.STS_Organisation,
                new[]
                {
                    CreateOrganizationUnit(OrganizationUnitOrigin.STS_Organisation)
                }
            );
            var expectedNewUnits = expectedSubTree.FlattenHierarchy().ToList();
            var expectedChild = expectedNewUnits.Skip(1).Single();

            var externalTree = ConvertToExternalTree(root, (current, currentChildren) =>
            {
                //Add the new sub tree if this is the parent of the new sub tree we expect
                if (current == randomParentOfNewSubTree)
                {
                    return expectedSubTree
                        .WrapAsEnumerable()
                        .Concat(currentChildren);
                }

                return currentChildren;
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);

            var addedUnits = consequences.AddedExternalOrganizationUnits.ToList();
            Assert.Equal(2, addedUnits.Count);
            Assert.Contains(addedUnits, unit => expectedNewUnits.Any(x => x.ExternalOriginUuid.GetValueOrDefault() == unit.unitToAdd.Uuid));

            var addedRoot = Assert.Single(addedUnits.Where(x => x.unitToAdd.Uuid == expectedSubTree.ExternalOriginUuid.GetValueOrDefault()));
            Assert.Equal(randomParentOfNewSubTree.ExternalOriginUuid.GetValueOrDefault(), addedRoot.parent.Uuid);
            var addedChild = Assert.Single(addedUnits.Where(x => x.unitToAdd.Uuid == expectedChild.ExternalOriginUuid.GetValueOrDefault()));
            Assert.Equal(addedRoot.unitToAdd, addedChild.parent);

        }

        [Fact]
        public void ComputeUpdate_Detects_Renamed_OrganizationUnits()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var randomItemToRename = root
                .FlattenHierarchy()
                .Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation)
                .RandomItem();

            var externalTree = ConvertToExternalTree(root);

            var expectedNewName = randomItemToRename.Name; //as converted
            var expectedOldNAme = A<string>();
            randomItemToRename.Name = expectedOldNAme; //Rename the local item to enforce name change detection

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
            var (affectedUnit, oldName, newName) = Assert.Single(consequences.OrganizationUnitsBeingRenamed);
            Assert.Same(randomItemToRename, affectedUnit);
            Assert.Equal(expectedOldNAme, oldName);
            Assert.Equal(expectedNewName, newName);
        }

        [Fact, Description("Verifies if we detect if an existing unit has been moved to another existing unit")]
        public void ComputeUpdate_Detects_Units_Moved_To_Existing_Parent()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var randomLeafWhichMustBeMovedToRoot = root
                .FlattenHierarchy()
                .Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation)
                .Where(x => x.IsLeaf())
                .RandomItem();

            var externalTree = ConvertToExternalTree(root, (current, currentChildren) =>
            {
                if (current == randomLeafWhichMustBeMovedToRoot.Parent)
                {
                    //Remove from the current parent
                    return currentChildren.Where(child => child != randomLeafWhichMustBeMovedToRoot).ToList();
                }

                if (current.IsRoot())
                {
                    //Move to the root
                    return currentChildren.Append(randomLeafWhichMustBeMovedToRoot).ToList();
                }

                return currentChildren;
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);

            var (movedUnit, oldParent, newParent) = Assert.Single(consequences.OrganizationUnitsBeingMoved);
            Assert.Equal(randomLeafWhichMustBeMovedToRoot, movedUnit);
            Assert.Equal(movedUnit.Parent, oldParent);
            Assert.Equal(externalTree, newParent);
        }

        [Fact, Description("Verifies if we detect if an existing unit has been moved one of the new units")]
        public void ComputeUpdate_Detects_Units_Moved_To_Newly_Added_Parent()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var randomLeafMovedToNewlyImportedItem = root
                .FlattenHierarchy()
                .Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation)
                .Where(x => x.IsLeaf())
                .RandomItem();

            var newItem = CreateOrganizationUnit(
                OrganizationUnitOrigin.STS_Organisation,
                new[]
                {
                    //NOTE: Make a copy to not modify the existing object (children in the list will get the parent in scope and this affects detection)
                    new OrganizationUnit
                    {
                        Id = randomLeafMovedToNewlyImportedItem.Id,
                        Organization = _organization,
                        Origin = randomLeafMovedToNewlyImportedItem.Origin,
                        ExternalOriginUuid = randomLeafMovedToNewlyImportedItem.ExternalOriginUuid,
                        Name = randomLeafMovedToNewlyImportedItem.Name
                    }
                }
            );
            newItem.Parent = root;

            var externalTree = ConvertToExternalTree(root, (current, currentChildren) =>
            {
                if (current == randomLeafMovedToNewlyImportedItem.Parent)
                {
                    //Remove from the current parent
                    return currentChildren.Where(child => child != randomLeafMovedToNewlyImportedItem).ToList();
                }

                if (current.IsRoot())
                {
                    //Add the new item to the root and the new item contains the moved item
                    return currentChildren.Append(newItem).ToList();
                }

                return currentChildren;
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            var (unitToAdd, parent) = Assert.Single(consequences.AddedExternalOrganizationUnits);
            Assert.Equal(newItem.ExternalOriginUuid.GetValueOrDefault(), unitToAdd.Uuid);
            Assert.Equal(root.ExternalOriginUuid.GetValueOrDefault(), parent.Uuid);

            var (movedUnit, oldParent, newParent) = Assert.Single(consequences.OrganizationUnitsBeingMoved);
            Assert.Equal(randomLeafMovedToNewlyImportedItem, movedUnit);
            Assert.Equal(movedUnit.Parent, oldParent);
            Assert.Equal(unitToAdd.Uuid, newParent.Uuid);
        }

        [Fact]
        public void ComputeUpdate_Detects_Removed_Units_Which_Are_Converted_Since_They_Contain_Retained_SubTree_Content()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var nodeExpectedToBeConverted = root
                .FlattenHierarchy()
                .Where(x => //Find a synced node that contains native children which are not deleted by external deletions. We expect the native child to be deleted (if any)
                    x != root &&
                    x.Origin == OrganizationUnitOrigin.STS_Organisation &&
                    x.Children.Any(c => c.Origin == OrganizationUnitOrigin.Kitos))
                .RandomItem();

            var subtreeOfRemovedExternalItem = nodeExpectedToBeConverted
                .FlattenHierarchy()
                .Where(node => node != nodeExpectedToBeConverted);
            var expectedRemovedUnits = subtreeOfRemovedExternalItem.Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation).ToList();

            var externalTree = ConvertToExternalTree(root, (_, currentChildren) =>
            {
                //Make sure the removed subtree is filtered out
                return currentChildren.Where(child => child != nodeExpectedToBeConverted);
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            var organizationUnit = Assert.Single(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Same(nodeExpectedToBeConverted, organizationUnit);

            var expectedRemovedItems = expectedRemovedUnits.OrderBy(unit => unit.Id);
            var actualRemovedItems = consequences.DeletedExternalUnitsBeingDeleted.OrderBy(unit => unit.Id);
            Assert.Equal(expectedRemovedItems, actualRemovedItems);

            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
        }

        [Fact]
        public void ComputeUpdate_Detects_Removed_Units_Which_Are_Converted_Since_They_Are_Still_In_Use()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var removedNodeInUse = root
                .FlattenHierarchy()
                .Where(x => //Find a synced node that contains native children which are not deleted by external deletions. We expect the native child to be deleted (if any)
                    x.Origin == OrganizationUnitOrigin.STS_Organisation &&
                    x.IsLeaf())
                .RandomItem();
            removedNodeInUse.Using.Add(new ItSystemUsageOrgUnitUsage());

            var externalTree = ConvertToExternalTree(root, (_, currentChildren) =>
            {
                //Make sure the removed subtree is filtered out
                return currentChildren.Where(child => child != removedNodeInUse);
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            var organizationUnit = Assert.Single(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Same(removedNodeInUse, organizationUnit);

            Assert.Empty(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
        }

        [Fact]
        public void ComputeUpdate_Detects_Removed_Units_Which_Are_Deleted()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var expectedRemovedUnit = root
                .FlattenHierarchy()
                .Where(x => //Find a synced node that contains native children which are not deleted by external deletions. We expect the native child to be deleted (if any)
                    x.Origin == OrganizationUnitOrigin.STS_Organisation &&
                    x.IsLeaf())
                .RandomItem();

            var externalTree = ConvertToExternalTree(root, (_, currentChildren) =>
            {
                //Make sure the removed subtree is filtered out
                return currentChildren.Where(child => child != expectedRemovedUnit);
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            var removedUnit = Assert.Single(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Same(expectedRemovedUnit, removedUnit);

            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
            Assert.Empty(consequences.OrganizationUnitsBeingMoved);
        }

        [Fact]
        public void ComputeUpdate_Detects_Removed_Nodes_Where_Leafs_Are_Moved_To_Removed_UnitsParent()
        {
            //Arrange
            PrepareConnectedOrganization();
            var root = _organization.GetRoot();
            var expectedRemovedUnit = root
                .FlattenHierarchy()
                .Where(x => //Find a synced node that contains native children which are not deleted by external deletions. We expect the native child to be deleted (if any)
                    x != root &&
                    x.Origin == OrganizationUnitOrigin.STS_Organisation &&
                    !x.IsLeaf() &&
                    x.FlattenHierarchy().All(c => c.Origin == OrganizationUnitOrigin.STS_Organisation))
                .RandomItem();

            var expectedParentChanges = expectedRemovedUnit.Children;

            var externalTree = ConvertToExternalTree(root, (current, currentChildren) =>
            {
                if (current == expectedRemovedUnit.Parent)
                {
                    return currentChildren
                        .Where(child => child != expectedRemovedUnit)
                        //Move the children of the removed item to the removed item's parent
                        .Concat(expectedParentChanges.Select(x => new OrganizationUnit
                        {
                            Id = x.Id,
                            Organization = _organization,
                            ExternalOriginUuid = x.ExternalOriginUuid,
                            Origin = x.Origin,
                            Children = x.Children,
                            Name = x.Name
                        })).ToList();
                }
                return currentChildren;
            });

            //Act
            var consequences = _sut.ComputeUpdate(externalTree);

            //Assert
            var removedUnit = Assert.Single(consequences.DeletedExternalUnitsBeingDeleted);
            Assert.Same(expectedRemovedUnit, removedUnit);
            var movedUnits = consequences.OrganizationUnitsBeingMoved.ToList();
            Assert.Equal(expectedParentChanges.Count, movedUnits.Count);
            foreach (var (movedUnit, oldParent, newParent) in movedUnits)
            {
                Assert.Equal(removedUnit, oldParent);
                Assert.Equal(removedUnit.Parent.ExternalOriginUuid.GetValueOrDefault(), newParent.Uuid);
                Assert.Contains(movedUnit, expectedParentChanges);
            }

            Assert.Empty(consequences.DeletedExternalUnitsBeingConvertedToNativeUnits);
            Assert.Empty(consequences.OrganizationUnitsBeingRenamed);
            Assert.Empty(consequences.AddedExternalOrganizationUnits);
        }

        private static ExternalOrganizationUnit ConvertToExternalTree(OrganizationUnit root, Func<OrganizationUnit, IEnumerable<OrganizationUnit>, IEnumerable<OrganizationUnit>> customChildren = null)
        {
            customChildren ??= ((unit, existingChildren) => existingChildren);

            return new ExternalOrganizationUnit(
                root.ExternalOriginUuid.GetValueOrDefault(),
                root.Name,
                new Dictionary<string, string>(),
                root
                    .Children
                    .Where(x => x.Origin == OrganizationUnitOrigin.STS_Organisation)
                    .Transform(filteredChildren => customChildren(root, filteredChildren))
                    .Select(child => ConvertToExternalTree(child, customChildren))
                    .ToList()
                );
        }
    }
}
