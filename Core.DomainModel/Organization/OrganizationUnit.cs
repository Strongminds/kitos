using System;
using System.Collections.Generic;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystemUsage;
// ReSharper disable VirtualMemberCallInConstructor

namespace Core.DomainModel.Organization
{
    /// <summary>
    /// Represents a unit or department within an organization (OIO term: "OrgEnhed").
    /// </summary>
    public class OrganizationUnit : HasRightsEntity<OrganizationUnit, OrganizationUnitRight, OrganizationUnitRole>, IHierarchy<OrganizationUnit>, IOrganizationModule, IOwnedByOrganization, IHasUuid, IHasName
    {
        public const int MaxNameLength = 100;
        public OrganizationUnit()
        {
            OwnedTasks = new List<TaskRef>();
            DefaultUsers = new List<OrganizationRight>();
            Using = new List<ItSystemUsageOrgUnitUsage>();
            Uuid = Guid.NewGuid();
        }

        public string LocalId { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// EAN number of the department.
        /// </summary>
        public long? Ean { get; set; }

        public int? ParentId { get; set; }
        /// <summary>
        /// Parent department.
        /// </summary>
        public virtual OrganizationUnit Parent { get; set; }
        public virtual ICollection<OrganizationUnit> Children { get; set; }

        public int OrganizationId { get; set; }
        /// <summary>
        /// The organization which the unit belongs to.
        /// </summary>
        public virtual Organization Organization { get; set; }
        /// <summary>
        /// Local tasks that was created in this unit
        /// </summary>
        public virtual ICollection<TaskRef> OwnedTasks { get; set; }
        /// <summary>
        /// Gets or sets the delegated system usages.
        /// </summary>
        /// <value>
        /// The delegated system usages.
        /// </value>
        public virtual ICollection<ItSystemUsage.ItSystemUsage> DelegatedSystemUsages { get; set; }

        /// <summary>
        /// Users which have set this as their default OrganizationUnit.
        /// </summary>
        /// <remarks>
        /// Goes through <seealso cref="OrganizationRight"/>.
        /// So to access the user you must call .User on the rights object.
        /// </remarks>
        public virtual ICollection<OrganizationRight> DefaultUsers { get; set; }

        /// <summary>
        /// This Organization Unit is using these IT Systems (Via ItSystemUsage)
        /// </summary>
        public virtual ICollection<ItSystemUsageOrgUnitUsage> Using { get; set; }

        /// <summary>
        /// This Organization Unit is responsible for these IT ItContracts
        /// </summary>
        public virtual ICollection<ItContract.ItContract> ResponsibleForItContracts { get; set; }

        /// <summary>
        /// The Organization Unit is listed in these economy streams
        /// </summary>
        public virtual ICollection<EconomyStream> EconomyStreams { get; set; }

        /// <summary>
        /// Determines whether a user has write access to this instance.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>
        ///   <c>true</c> if user has write access, otherwise <c>false</c>.
        /// </returns>
        public override bool HasUserWriteAccess(User user)
        {
            // check rights on parent org unit
            if (Parent != null && Parent.HasUserWriteAccess(user))
                return true;

            return base.HasUserWriteAccess(user);
        }

        public override OrganizationUnitRight CreateNewRight(OrganizationUnitRole role, User user)
        {
            return new OrganizationUnitRight()
            {
                Role = role,
                User = user,
                Object = this
            };
        }

        public Guid Uuid { get; set; }
    }
}
