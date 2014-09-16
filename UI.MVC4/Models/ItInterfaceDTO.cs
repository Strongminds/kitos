﻿using System.Collections.Generic;
using Core.DomainModel;

namespace UI.MVC4.Models
{
    public class ItInterfaceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string ItInterfaceId { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string ObjectOwnerName { get; set; }
        public AccessModifier AccessModifier { get; set; }
        public int? ExhibitedByItSystemId { get; set; }
        public string ExhibitedByItSystemName { get; set; }
        public int? TsaId { get; set; }
        public int? InterfaceId { get; set; }
        public int? InterfaceTypeId { get; set; }
        public int? MethodId { get; set; }
        public IEnumerable<DataRowDTO> DataRows { get; set; }
    }
}
