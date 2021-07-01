﻿using System;
using System.ComponentModel.DataAnnotations;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Models.External.V2.Request
{
    public class RightsHolderCreateItInterfaceRequestDTO : RightsHolderWritableItInterfacePropertiesDTO
    {
        /// <summary>
        /// UUID for owning organization
        /// </summary>
        /// <remarks>Use api/v2/rightsholder/organizations API for getting a list of possible organizations related to the logged in user</remarks>
        [Required]
        [RequireNonEmptyGuid]
        public Guid RightsHolderUuid { get; set; }

        /// <summary>
        /// UUID for IT-Interface
        /// If no UUID is provided, KITOS will assign one.
        /// </summary>
        [RequireNonEmptyGuid]
        public Guid? Uuid { get; set; }
    }
}