﻿using System;
using System.Collections.Generic;
using Core.DomainModel;

namespace UI.MVC4.Models
{
    public class ItSystemDTO
    {
        public int Id { get; set; }

        public int? ParentId { get; set; }
        public string ParentName { get; set; }

        public int? ExposedById { get; set; }
        public ItSystemDTO ExposedBy { get; set; }

        public IEnumerable<int> CanBeUsedByIds { get; set; }
        public IEnumerable<int> CanUseInterfaceIds { get; set; }
        public IEnumerable<ItSystemSimpleDTO> CanUseInterfaces { get; set; }
        public IEnumerable<int> ExposedInterfaceIds { get; set; }
        public int? BelongsToId { get; set; }
        public string BelongsToName { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string SystemId { get; set; }
        public int ObjectOwnerId { get; set; }

        public AccessModifier AccessModifier { get; set; }

        public string Description { get; set; }
        public string Url { get; set; }
        public IEnumerable<int> TaskRefIds { get; set; }
        public IEnumerable<TaskRefDTO> TaskRefs { get; set; }

        public int? AppTypeId { get; set; }
        public string AppTypeName { get; set; }
        public int? BusinessTypeId { get; set; }

        public int? InterfaceId { get; set; }
        public int? InterfaceTypeId { get; set; }
        public int? TsaId { get; set; }
        public int? MethodId { get; set; }

        public IEnumerable<DataRowDTO> DataRows { get; set; }

        public DateTime LastChanged { get; set; }
        public int LastChangedByUserId { get; set; }
    }
}