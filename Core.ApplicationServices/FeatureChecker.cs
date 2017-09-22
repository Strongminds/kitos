﻿using System.Collections.Generic;
using System.Linq;
using Core.DomainModel;
using Core.DomainModel.Organization;

namespace Core.ApplicationServices
{
    public enum Feature
    {
        MakeGlobalAdmin = 1,
        MakeLocalAdmin,
        MakeReportAdmin,
        MakeOrganization,
        CanSetAccessModifierToPublic,
        CanSetOrganizationTypeKommune,
        CanSetOrganizationTypeInteressefællesskab,
        CanSetOrganizationTypeVirksomhed,
        CanSetOrganizationTypeAndenOffentligMyndighed,
        CanSetOrganizationAccessModifierToPublic,
        CanModifyUsers,
        CanModifyContracts,
        CanModifyOrganizations,
        CanModifySystems,
        CanModifyProjects,
        CanModifyReports,
        CanSetContractElementsAccessModifierToPublic
    }

    public class FeatureChecker : IFeatureChecker
    {
        private Dictionary<Feature, List<OrganizationRole>> _features;

        public FeatureChecker()
        {
            Init();
        }

        public bool CanExecute(User user, Feature feature)
        {
            var userRoles = CreateRoleList(user);
            var featureRoles = _features[feature];
            return userRoles.Any(userRole => featureRoles.Contains(userRole));
        }

        private static IEnumerable<OrganizationRole> CreateRoleList(User user)
        {
            var roles = user.OrganizationRights.Where(or => or.OrganizationId == user.DefaultOrganizationId).Select(x => x.Role).Distinct().ToList();
            if (user.IsGlobalAdmin)
                roles.Add(OrganizationRole.GlobalAdmin);

            return roles;
        }

        private void Init()
        {
            _features = new Dictionary<Feature, List<OrganizationRole>>
            {
                {Feature.MakeGlobalAdmin, new List<OrganizationRole> {OrganizationRole.GlobalAdmin}},
                {Feature.MakeLocalAdmin, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin}},
                {Feature.CanSetOrganizationTypeKommune, new List<OrganizationRole> {OrganizationRole.GlobalAdmin}},
                {Feature.CanSetOrganizationTypeInteressefællesskab, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin}},
                {Feature.CanSetOrganizationTypeVirksomhed, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin}},
                {Feature.CanSetOrganizationTypeAndenOffentligMyndighed, new List<OrganizationRole> {OrganizationRole.GlobalAdmin}},
                {Feature.CanSetAccessModifierToPublic, new List<OrganizationRole> {OrganizationRole.GlobalAdmin}},
                {Feature.CanSetOrganizationAccessModifierToPublic, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin}},
                {Feature.CanModifyUsers, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.OrganizationModuleAdmin } },
                {Feature.CanModifyContracts, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.ContractModuleAdmin } },
                {Feature.CanModifyOrganizations, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.OrganizationModuleAdmin } },
                {Feature.CanModifyProjects, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.ProjectModuleAdmin } },
                {Feature.CanModifySystems, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.SystemModuleAdmin} },
                {Feature.CanModifyReports, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.ReportModuleAdmin} },
                {Feature.CanSetContractElementsAccessModifierToPublic, new List<OrganizationRole> {OrganizationRole.GlobalAdmin, OrganizationRole.LocalAdmin, OrganizationRole.ContractModuleAdmin} }
            };
        }


    }
}