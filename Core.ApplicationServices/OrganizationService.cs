﻿using System.Collections.Generic;
using System.Linq;
using Core.DomainModel;
using Core.DomainServices;

namespace Core.ApplicationServices
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IGenericRepository<Organization> _orgRepository;
        private readonly IGenericRepository<AdminRight> _admRightRepository;

        public OrganizationService(IGenericRepository<Organization> orgRepository, IGenericRepository<AdminRight> admRightRepository)
        {
            _orgRepository = orgRepository;
            _admRightRepository = admRightRepository;
        }

        //returns a list of organizations that the user is a member of
        public IEnumerable<Organization> GetOrganizations(User user)
        {
            if (user.IsGlobalAdmin) return _orgRepository.Get();
            else return _admRightRepository.Get().Select(r => r.Object);
        }

        //returns the default org unit for that user inside that organization
        //or null if none has been chosen
        public OrganizationUnit GetDefaultUnit(Organization organization, User user)
        {
            return _admRightRepository.Get(r => r.ObjectId == organization.Id && r.UserId == user.Id).Select(r => r.DefaultOrgUnit).FirstOrDefault();
        }

        public void SetDefaultOrgUnit(User user, int orgId, int orgUnitId)
        {
            //TODO this should probably be Single() ?
            var right = _admRightRepository.Get(r => r.UserId == user.Id && r.ObjectId == orgId).First();
            right.DefaultOrgUnitId = orgUnitId;

            _admRightRepository.Update(right);
            _admRightRepository.Save();
        }

        public void SetupDefaultOrganization(Organization org, User objectOwner)
        {
            org.Config = Config.Default(objectOwner);
            org.OrgUnits.Add(new OrganizationUnit()
                {
                    Name = org.Name,
                    ObjectOwner = org.ObjectOwner,
                    LastChangedByUser = objectOwner
                });
        }
    }
}