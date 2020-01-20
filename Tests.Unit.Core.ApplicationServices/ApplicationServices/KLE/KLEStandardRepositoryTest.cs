﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Core.DomainModel.ItProject;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.KLE;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Repositories.KLE;
using Infrastructure.Services.DataAccess;
using Infrastructure.Services.KLEDataBridge;
using Moq;
using Serilog;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.KLE
{
    public class KLEStandardRepositoryTest
    {
        [Theory]
        [InlineData("31-10-2019", false)]
        [InlineData("01-01-2020", true)]
        private void GetKLEStatus_Returns_ValidStatus(string lastUpdatedString, bool expectedUpToDate)
        {
            var mockKLEDataBridge = new Mock<IKLEDataBridge>();
            var document = XDocument.Load("./ApplicationServices/KLE/20200106-kle-only-published-date.xml");
            var expectedPublishDate = DateTime.Parse(document.Descendants("UdgivelsesDato").First().Value, CultureInfo.GetCultureInfo("da-DK"));
            mockKLEDataBridge.Setup(r => r.GetKLEXMLData()).Returns(document);
            var stubTaskRefRepository = new GenericRepositoryTaskRefStub();
            var mockTransactionManager = new Mock<ITransactionManager>();
            var mockLogger = new Mock<ILogger>();
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(mockKLEDataBridge.Object, mockTransactionManager.Object, stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, mockLogger.Object);
            var result = sut.GetKLEStatus(DateTime.Parse(lastUpdatedString, CultureInfo.GetCultureInfo("da-DK")));
            Assert.Equal(expectedUpToDate, result.UpToDate);
            Assert.Equal(expectedPublishDate, result.Published);
        }

        [Fact]
        private void GetKLEChangeSummary_Returns_List_Of_Changes()
        {
            var mockKLEDataBridge = new Mock<IKLEDataBridge>();
            var document = XDocument.Load("./ApplicationServices/KLE/20200106-kle-sample-changes.xml");
            mockKLEDataBridge.Setup(r => r.GetKLEXMLData()).Returns(document);
            var stubTaskRefRepository = new GenericRepositoryTaskRefStub();
            // Removed item examples
            stubTaskRefRepository.Insert(SetupTaskRef(1, "KLE-Hovedgruppe", "03"));
            stubTaskRefRepository.Insert(SetupTaskRef(2, "KLE-Gruppe", "00.02" ));
            stubTaskRefRepository.Insert(SetupTaskRef(3, "KLE-Emne", "00.03.01"));
            // Renamed item example
            stubTaskRefRepository.Insert(SetupTaskRef(4, "KLE-Emne", "00.03.00", "International virksomhed og EU"));
            // Unchanged item example
            stubTaskRefRepository.Insert(SetupTaskRef(5, "KLE-Emne", "02.02.00", "Bebyggelsens højde- og afstandsforhold i almindelighed"));
            var mockTransactionManager = new Mock<ITransactionManager>();
            var mockLogger = new Mock<ILogger>();
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(mockKLEDataBridge.Object, mockTransactionManager.Object, stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, mockLogger.Object);
            var result = sut.GetKLEChangeSummary();
            var numberOfKLEMainGroups = document.Descendants("Hovedgruppe").Count();
            var numberOfKLEGroups = document.Descendants("Gruppe").Count();
            var numberOfKLESubjects = document.Descendants("Emne").Count();
            var totalKLEItems = numberOfKLEMainGroups + numberOfKLEGroups + numberOfKLESubjects;
            const int expectedNumberOfRemoved = 3;
            const int expectedNumberOfRenames = 1;
            const int expectedNumberOfUnchanged = 1;
            var expectedNumberOfAdded = totalKLEItems - expectedNumberOfRenames - expectedNumberOfUnchanged;
            Assert.Equal(3, numberOfKLEMainGroups);
            Assert.Equal(9, numberOfKLEGroups);
            Assert.Equal(9, numberOfKLESubjects);
            Assert.Equal(expectedNumberOfRemoved, result.Count(c => c.ChangeType == KLEChangeType.Removed));
            Assert.Equal(expectedNumberOfRenames, result.Count(c => c.ChangeType == KLEChangeType.Renamed));
            Assert.Equal(expectedNumberOfAdded, result.Count(c => c.ChangeType == KLEChangeType.Added));
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_Returns_Published_Date()
        {
            var updateObjects = SetupUpdateObjects();
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            var result = sut.UpdateKLE(0, 0);
            Assert.Equal(DateTime.Parse("01-11-2019", CultureInfo.GetCultureInfo("da-DK")), result);
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_Renamed_TaskRefs()
        {
            var updateObjects = SetupUpdateObjects();
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0, 0);
            Assert.Equal("HAS BEEN RENAMED", updateObjects.renamedTaskRef.Description);
            Assert.Equal(Guid.Parse("f8d6e719-e050-48d8-89e2-977d0eaba6bb"), updateObjects.renamedTaskRef.Uuid);
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_On_Empty_Repos_Adds_All_TaskRefs()
        {
            var updateObjects = SetupUpdateObjects();
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0, 0);
            Assert.Equal(21, updateObjects.stubTaskRefRepository.Get().Count());
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_On_NonEmpty_Repos_Fills_Uuid_On_Existing_TaskRef()
        {
            var updateObjects = SetupUpdateObjects();
            // Existing item with no Uuid
            const string existingItemTaskKey = "02.02.00";
            updateObjects.stubTaskRefRepository.Insert(SetupTaskRef(1, "KLE-Emne", existingItemTaskKey, "Bebyggelsens højde- og afstandsforhold i almindelighed"));
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0, 0);
            Assert.Equal(21, updateObjects.stubTaskRefRepository.Get().Count());
            var sampleTaskRef = updateObjects.stubTaskRefRepository.Get(t => t.TaskKey == existingItemTaskKey).First();
            Assert.Equal(Guid.Parse("f0820080-181a-4ea4-9587-02b86aa13898"), sampleTaskRef.Uuid);
        }
        
        [Fact]
        private void UpdateKLE_Given_Summary_Updates_ItProject()
        {
            var updateObjects = SetupUpdateObjects();
            const int itProjectKey = 1;
            var itProject = new ItProject
            {
                Id = itProjectKey,
                TaskRefs = new List<TaskRef> {updateObjects.removedTaskRef}
            };
            updateObjects.removedTaskRef.ItProjects = new List<ItProject> { itProject };
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0, 0);
            Assert.False(itProject.TaskRefs.Contains(updateObjects.removedTaskRef));
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_ItSystem()
        {
            var updateObjects = SetupUpdateObjects();
            const int itSystemKey = 1;
            var itSystem = new ItSystem
            {
                Id = itSystemKey,
                TaskRefs = new List<TaskRef> { updateObjects.removedTaskRef }
            };
            updateObjects.removedTaskRef.ItSystems = new List<ItSystem> { itSystem };
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0,0);
            Assert.False(itSystem.TaskRefs.Contains(updateObjects.removedTaskRef));
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_ItSystemUsages()
        {
            var updateObjects = SetupUpdateObjects();
            const int itSystemUsageKey = 1;
            var itSystemUsage = new ItSystemUsage
            {
                Id = itSystemUsageKey,
                TaskRefs = new List<TaskRef> { updateObjects.removedTaskRef }
            };
            var itSystemUsages = new List<ItSystemUsage> { itSystemUsage };
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            mockSystemUsageRepository
                .Setup(s => s.GetWithReferencePreload(t => t.TaskRefs))
                .Returns(itSystemUsages.AsQueryable);
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0,0);
            Assert.False(itSystemUsage.TaskRefs.Contains(updateObjects.removedTaskRef));
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_ItSystemUsageOptOut()
        {
            var updateObjects = SetupUpdateObjects();
            const int itSystemUsagesOptOutKey = 1;
            var itSystemUsage = new ItSystemUsage
            {
                Id = itSystemUsagesOptOutKey,
                TaskRefs = new List<TaskRef> { updateObjects.removedTaskRef }
            };
            updateObjects.removedTaskRef.ItSystemUsagesOptOut = new List<ItSystemUsage> { itSystemUsage };
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0,0);
            Assert.False(itSystemUsage.TaskRefs.Contains(updateObjects.removedTaskRef));
        }

        [Fact]
        private void UpdateKLE_Given_Summary_Updates_TaskUsage()
        {
            var updateObjects = SetupUpdateObjects();
            const int taskUsageKey = 1;
            var taskUsage = new TaskUsage
            {
                Id = taskUsageKey,
                TaskRef = updateObjects.removedTaskRef
            };
            var taskUsages = new List<TaskUsage> { taskUsage };
            var mockSystemUsageRepository = new Mock<IGenericRepository<ItSystemUsage>>();
            var mockTaskUsageRepository = new Mock<IGenericRepository<TaskUsage>>();
            mockTaskUsageRepository
                .Setup(s => s.GetWithReferencePreload(t => t.TaskRef))
                .Returns(taskUsages.AsQueryable);
            mockTaskUsageRepository.Setup(s => s.RemoveRange(taskUsages));
            var sut = new KLEStandardRepository(updateObjects.mockKLEDataBridge.Object, updateObjects.mockTransactionManager.Object, updateObjects.stubTaskRefRepository, mockSystemUsageRepository.Object, mockTaskUsageRepository.Object, updateObjects.mockLogger.Object);
            sut.UpdateKLE(0,0);
            mockTaskUsageRepository.VerifyAll();
        }

        #region Helpers

        private static (
            Mock<IKLEDataBridge> mockKLEDataBridge, 
            Mock<ITransactionManager> mockTransactionManager, 
            Mock<ILogger> mockLogger,
            GenericRepositoryTaskRefStub stubTaskRefRepository, 
            TaskRef removedTaskRef, 
            TaskRef renamedTaskRef) SetupUpdateObjects()
        {
            var mockKLEDataBridge = new Mock<IKLEDataBridge>();
            var document = XDocument.Load("./ApplicationServices/KLE/20200106-kle-sample-changes.xml");
            mockKLEDataBridge.Setup(r => r.GetKLEXMLData()).Returns(document);
            var removedTaskRef = SetupTaskRef(1, "KLE-Emne", "00.03.01", "Dummy");
            var renamedTaskRef = SetupTaskRef(2, "KLE-Emne", "00.03.00", "International virksomhed og EU");
            var stubTaskRefRepository = new GenericRepositoryTaskRefStub();
            stubTaskRefRepository.Insert(removedTaskRef);
            stubTaskRefRepository.Insert(renamedTaskRef);
            var mockTransactionManager = new Mock<ITransactionManager>();
            mockTransactionManager.Setup(t => t.Begin(It.IsAny<IsolationLevel>())).Returns(new Mock<IDatabaseTransaction>().Object);
            var mockLogger = new Mock<ILogger>();
            return (mockKLEDataBridge, mockTransactionManager, mockLogger, stubTaskRefRepository, removedTaskRef, renamedTaskRef);
        }

        private static TaskRef SetupTaskRef(int id, string kleType, string kleTaskKey, string kleDescription = "")
        {
            return new TaskRef { Id = id, Type = kleType, TaskKey = kleTaskKey, Description = kleDescription };
        }

        #endregion
    }
}
