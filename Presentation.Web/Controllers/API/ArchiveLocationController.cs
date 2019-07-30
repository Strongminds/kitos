﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API
{
    using Core.DomainModel.ItSystem;
    using Core.DomainModel.ItSystemUsage;
    using Core.DomainServices;

    using Presentation.Web.Models;

    [InternalApi]
    public class ArchiveLocationController : GenericOptionApiController<ArchiveLocation, ItSystemUsage, OptionDTO>
    {
        /// <summary>
        /// Arkiveringssted for it system anvendelse fanen arkivering
        /// </summary>
        /// <param name="repository"></param>
        public ArchiveLocationController(IGenericRepository<ArchiveLocation> repository)
            : base(repository)
        {
        }
    }
}