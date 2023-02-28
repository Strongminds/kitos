﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.DomainServices.Queries;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Core.ApplicationServices.System;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Queries.ItSystem;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V2.Common.Helpers
{
    /// <summary>
    /// Helper method to consolidate shared query logic supporting both internal and external it-system usage API endpoints
    /// </summary>
    public static class ItSystemV2QueryHandler
    {
        public static IEnumerable<ItSystem> ExecuteItSystemsQuery(
            this IItSystemService service,
            [NonEmptyGuid] Guid? rightsHolderUuid = null,
            [NonEmptyGuid] Guid? businessTypeUuid = null,
            string kleNumber = null,
            [NonEmptyGuid] Guid? kleUuid = null,
            int? numberOfUsers = null,
            bool? includeDeactivated = null,
            DateTime? changedSinceGtEq = null,
            [NonEmptyGuid] Guid? usedInOrganizationUuid = null,
            BoundedPaginationQuery paginationQuery = null)
        {
            var refinements = new List<IDomainQuery<ItSystem>>();

            if (rightsHolderUuid.HasValue)
                refinements.Add(new QueryByRightsHolderUuid(rightsHolderUuid.Value));

            if (businessTypeUuid.HasValue)
                refinements.Add(new QueryByBusinessType(businessTypeUuid.Value));

            if (kleNumber != null || kleUuid.HasValue)
                refinements.Add(new QueryByTaskRef(kleNumber, kleUuid));

            if (numberOfUsers.HasValue)
                refinements.Add(new QueryByNumberOfUsages(numberOfUsers.Value));

            if (includeDeactivated != true)
                refinements.Add(new QueryByEnabledEntitiesOnly<ItSystem>());

            if (changedSinceGtEq.HasValue)
                refinements.Add(new QueryByChangedSinceGtEq<ItSystem>(changedSinceGtEq.Value));

            if(usedInOrganizationUuid.HasValue)
                refinements.Add(new QuerySystemByUsedInOrganizationUuid(usedInOrganizationUuid.Value));

            return service.GetAvailableSystems(refinements.ToArray())
                .OrderByDefaultConventions(changedSinceGtEq.HasValue)
                .Page(paginationQuery)
                .ToList();
        }
    }
}