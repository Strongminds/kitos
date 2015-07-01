﻿using System.Collections.Generic;
using Core.DomainModel;

namespace Core.DomainServices
{
    public interface IOrgUnitService
    {
        OrganizationUnit GetRoot(OrganizationUnit unit);

        ICollection<OrganizationUnit> GetSubTree(int orgUnitId);
        ICollection<OrganizationUnit> GetSubTree(OrganizationUnit unit);

        bool IsAncestorOf(OrganizationUnit unitA, OrganizationUnit unitB);
        bool IsAncestorOf(int unitIdA, int unitIdB);
    }
}