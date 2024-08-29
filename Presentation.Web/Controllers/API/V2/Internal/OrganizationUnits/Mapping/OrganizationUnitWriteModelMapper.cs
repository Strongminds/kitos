﻿using Presentation.Web.Controllers.API.V2.Common.Mapping;
using System;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.Organizations.Write;
using Presentation.Web.Infrastructure.Model.Request;
using Presentation.Web.Models.API.V2.Request.OrganizationUnit;
using Core.ApplicationServices.Model.Shared;
using Core.DomainModel.Organization;
using Core.Abstractions.Types;

namespace Presentation.Web.Controllers.API.V2.Internal.OrganizationUnits.Mapping
{
    public class OrganizationUnitWriteModelMapper : WriteModelMapperBase, IOrganizationUnitWriteModelMapper
    {
        public OrganizationUnitWriteModelMapper(ICurrentHttpRequest currentHttpRequest) : base(currentHttpRequest)
        {
        }

        public OrganizationUnitUpdateParameters FromPOST(CreateOrganizationUnitRequestDTO request)
        {
            return MapParameters(request);
        }

        private OrganizationUnitUpdateParameters MapParameters(CreateOrganizationUnitRequestDTO request)
        {
            var rule = CreateChangeRule<CreateOrganizationUnitRequestDTO>(true);
            var parameters = new OrganizationUnitUpdateParameters
            {
                Name = rule.MustUpdate(x => x.Name)
                    ? request.Name.AsChangedValue()
                    : OptionalValueChange<string>.None,
                Origin = rule.MustUpdate(x => x.Origin)
                    ? request.Origin.ToOrganizationUnitOrigin().AsChangedValue()
                    : OptionalValueChange<OrganizationUnitOrigin>.None,
                ParentUuid = rule.MustUpdate(x => x.ParentUuid)
                    ? (request.ParentUuid.FromNullable() ?? Maybe<Guid>.None).AsChangedValue()
                    : OptionalValueChange<Maybe<Guid>>.None,
            };

            return parameters;
        }
    }
}