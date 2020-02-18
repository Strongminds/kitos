﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using Core.ApplicationServices.References;
using Core.DomainModel;
using Core.DomainModel.References;
using Core.DomainModel.Result;
using Core.DomainServices;
using Presentation.Web.Models;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;

namespace Presentation.Web.Controllers.API
{
    [PublicApi]
    public class ReferenceController : GenericApiController<ExternalReference, ExternalReferenceDTO>
    {
        private readonly IReferenceService _referenceService;

        public ReferenceController(
            IGenericRepository<ExternalReference> repository,
            IReferenceService referenceService)
            : base(repository)
        {
            _referenceService = referenceService;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            //NOTE: In this case we make sure dependencies are loaded on POST so we CAN use GetOwner
            return new ChildEntityCrudAuthorization<ExternalReference, IEntityWithExternalReferences>(reference => reference.GetOwner(), base.GetCrudAuthorization());
        }

        public override HttpResponseMessage Post(ExternalReferenceDTO dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            return
                GetOwnerTypeAnId(dto)
                    .Match
                    (
                        onValue: typeAndId => _referenceService
                            .Create
                            (
                                typeAndId.Value,
                                typeAndId.Key,
                                dto.Title,
                                dto.ExternalReferenceId,
                                dto.URL,
                                dto.Display
                            )
                            .Match
                            (
                                onSuccess: NewObjectCreated,
                                onFailure: FromOperationError
                            ),
                        onNone: () => BadRequest("Target owner Id must be defined")
                    );
        }

        private static Maybe<KeyValuePair<ReferenceRootType, int>> GetOwnerTypeAnId(ExternalReferenceDTO dto)
        {
            return GetOwnerTypeAndIdOrFallback(ReferenceRootType.Contract, dto.ItContract_Id,
                fallback: () => GetOwnerTypeAndIdOrFallback(ReferenceRootType.System, dto.ItSystem_Id,
                    fallback: () => GetOwnerTypeAndIdOrFallback(ReferenceRootType.SystemUsage, dto.ItSystemUsage_Id,
                        fallback: () => GetOwnerTypeAndIdOrFallback(ReferenceRootType.Project, dto.ItProject_Id,
                            fallback: () => Maybe<KeyValuePair<ReferenceRootType, int>>.None))));
        }

        private static Maybe<KeyValuePair<ReferenceRootType, int>> GetOwnerTypeAndIdOrFallback(ReferenceRootType ownerType, int? ownerId, Func<Maybe<KeyValuePair<ReferenceRootType, int>>> fallback)
        {
            return ownerId.HasValue ? new KeyValuePair<ReferenceRootType, int>(ownerType, ownerId.Value) : fallback();
        }
    }
}