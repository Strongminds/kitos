﻿using System;
using System.Collections.Generic;
using Core.DomainModel;

namespace Presentation.Web.Models.API.V1
{
    public class ItSystemDTO
    {
        public ItSystemDTO()
        {
            TaskRefIds = new List<int>();
        }

        public int Id { get; set; }
        public Guid Uuid { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }
        public bool ParentDisabled { get; set; }

        /// <summary>
        /// Gets or sets the exposed interface ids.
        /// </summary>
        /// <value>
        /// The exposed interface ids.
        /// </value>
        /// <remarks>
        /// This dones't exist in the model.
        /// It's a way to bind to <see cref="CanUseInterfaces"/>
        /// </remarks>
        public IEnumerable<int> ExposedInterfaceIds { get; set; }
        public int? BelongsToId { get; set; }
        public string BelongsToName { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string Name { get; set; }
        public string PreviousName { get; set; }
        public string ItSystemId { get; set; }
        public int ObjectOwnerId { get; set; }
        public string ObjectOwnerName { get; set; }
        public string ObjectOwnerLastName { get; set; }
        public string ObjectOwnerFullName
        {
            get { return ObjectOwnerName + " " + ObjectOwnerLastName; }
        }
        public AccessModifier? AccessModifier { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the task reference ids.
        /// </summary>
        /// <value>
        /// The task reference ids.
        /// </value>
        /// <remarks>
        /// This dones't exist in the model.
        /// It's a way to bind to <see cref="TaskRefs"/>
        /// </remarks>
        public IEnumerable<int> TaskRefIds { get; set; }
        public IEnumerable<TaskRefDTO> TaskRefs { get; set; }

        public int? BusinessTypeId { get; set; }
        public string BusinessTypeName { get; set; }

        public int? InterfaceId { get; set; }

        public DateTime LastChanged { get; set; }
        public string LastChangedByName { get; set; }
        public int LastChangedByUserId { get; set; }

        /// <summary>
        /// Gets or sets whether this instance has a usage in any organization.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has a usage; otherwise, <c>false</c>.
        /// </value>
        public bool IsUsed { get; set; }
        public bool? Active { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? Concluded { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool Disabled { get; set; }

        public ICollection<ExternalReferenceDTO> ExternalReferences { get; set; }
        public int? ReferenceId { get; set; }
        public ExternalReferenceDTO Reference;
        public ArchiveDutyRecommendationTypes? ArchiveDuty { get; set; }
        public string ArchiveDutyComment { get; set; }
    }
}
