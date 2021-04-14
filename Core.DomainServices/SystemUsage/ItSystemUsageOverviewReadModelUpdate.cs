﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.ItSystemUsage.Read;
using Core.DomainServices.Model;

namespace Core.DomainServices.SystemUsage
{
    public class ItSystemUsageOverviewReadModelUpdate : IReadModelUpdate<DomainModel.ItSystemUsage.ItSystemUsage, ItSystemUsageOverviewReadModel>
    {
        private readonly IGenericRepository<ItSystemUsageOverviewRoleAssignmentReadModel> _roleAssignmentRepository;

        public ItSystemUsageOverviewReadModelUpdate(IGenericRepository<ItSystemUsageOverviewRoleAssignmentReadModel> roleAssignmentRepository)
        {
            _roleAssignmentRepository = roleAssignmentRepository;
        }

        public void Apply(DomainModel.ItSystemUsage.ItSystemUsage source, ItSystemUsageOverviewReadModel destination)
        {
            destination.SourceEntityId = source.Id;
            destination.OrganizationId = source.OrganizationId;
            destination.Name = source.ItSystem.Name;
            destination.ItSystemDisabled = source.ItSystem.Disabled;
            destination.IsActive = source.IsActive;
            destination.Version = source.Version;
            destination.LocalCallName = source.LocalCallName;
            destination.LocalSystemId = source.LocalSystemId;
            destination.ItSystemUuid = source.ItSystem.Uuid.ToString("D");

            PatchParentSystemName(source, destination);
            PatchRoleAssignments(source, destination);
        }

        private static void PatchParentSystemName(DomainModel.ItSystemUsage.ItSystemUsage source, ItSystemUsageOverviewReadModel destination)
        {
            destination.ParentItSystemName = source.ItSystem.Parent?.Name;
            destination.ParentItSystemId = source.ItSystem.Parent?.Id;
        }

        private void PatchRoleAssignments(DomainModel.ItSystemUsage.ItSystemUsage source, ItSystemUsageOverviewReadModel destination)
        {
            static string CreateRoleKey(int roleId, int userId) => $"R:{roleId}U:{userId}";

            var incomingRights = source.Rights.ToDictionary(x => CreateRoleKey(x.RoleId, x.UserId));

            //Remove rights which were removed
            var assignmentsToBeRemoved =
                destination.RoleAssignments
                    .Where(x => incomingRights.ContainsKey(CreateRoleKey(x.RoleId, x.UserId)) == false).ToList();

            RemoveAssignments(destination, assignmentsToBeRemoved);

            var existingAssignments = destination.RoleAssignments.ToDictionary(x => CreateRoleKey(x.RoleId, x.UserId));
            foreach (var incomingRight in source.Rights.ToList())
            {
                if (!existingAssignments.TryGetValue(CreateRoleKey(incomingRight.RoleId, incomingRight.UserId), out var assignment))
                {
                    //Append the assignment if it is not already present
                    assignment = new ItSystemUsageOverviewRoleAssignmentReadModel
                    {
                        Parent = destination,
                        RoleId = incomingRight.RoleId,
                        UserId = incomingRight.UserId
                    };
                    destination.RoleAssignments.Add(assignment);
                }

                var fullName = $"{incomingRight.User.Name ?? ""} {incomingRight.User.LastName ?? ""}";
                assignment.UserFullName = fullName.TrimEnd().Substring(0, Math.Min(fullName.Length, 100));
            }

            _roleAssignmentRepository.Save();
        }

        private void RemoveAssignments(ItSystemUsageOverviewReadModel destination, List<ItSystemUsageOverviewRoleAssignmentReadModel> assignmentsToBeRemoved)
        {
            assignmentsToBeRemoved.ForEach(assignmentToBeRemoved =>
            {
                destination.RoleAssignments.Remove(assignmentToBeRemoved);
                _roleAssignmentRepository.Delete(assignmentToBeRemoved);
            });
        }

    }
}
