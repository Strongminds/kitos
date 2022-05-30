using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel;

namespace Core.DomainServices.FKOrganization
{
    public class FKOrganizationUnit
    {
        public Guid Uuid { get; }
        public string Name { get; }
        public IReadOnlyList<FKOrganizationUnit> Children { get; }

        public FKOrganizationUnit(Guid uuid, string name, IReadOnlyList<FKOrganizationUnit> children)
        {
            Uuid = uuid;
            Name = name;
            Children = children;
        }
    }

    public interface IFKOrganizationUnitImportService
    {
        Result<FKOrganizationUnit, OperationError> ImportOrganizationTree(int organizationId);
    }
}
