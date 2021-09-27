﻿using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.Interface;
using Core.ApplicationServices.Model.Shared;
using Presentation.Web.Controllers.API.V2.External.Generic;
using Presentation.Web.Infrastructure.Model.Request;
using Presentation.Web.Models.API.V2.Request.Interface;
using Presentation.Web.Models.API.V2.SharedProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Controllers.API.V2.External.ItInterfaces.Mapping
{
    public class ItInterfaceWriteModelMapper : WriteModelMapperBase, IItInterfaceWriteModelMapper
    {
        public ItInterfaceWriteModelMapper(ICurrentHttpRequest currentHttpRequest)
            : base(currentHttpRequest)
        {
        }

        public RightsHolderItInterfaceUpdateParameters FromPATCH(RightsHolderWritableItInterfacePropertiesDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return Map(dto, false);
        }

        public RightsHolderItInterfaceCreationParameters FromPOST(RightsHolderCreateItInterfaceRequestDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new RightsHolderItInterfaceCreationParameters(dto.RightsHolderUuid, Map(dto, false));
        }

        public RightsHolderItInterfaceUpdateParameters FromPUT(RightsHolderWritableItInterfacePropertiesDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return Map(dto, true);
        }

        private RightsHolderItInterfaceUpdateParameters Map<T>(T dto, bool enforceFallbackIfNotProvided) where T : RightsHolderWritableItInterfacePropertiesDTO, IHasNameExternal
        {

            return new RightsHolderItInterfaceUpdateParameters
            {
                Name = ClientRequestsChangeTo(nameof(IHasNameExternal.Name)) || enforceFallbackIfNotProvided ? dto.Name.AsChangedValue() : OptionalValueChange<string>.None,
                ExposingSystemUuid = ClientRequestsChangeTo(nameof(RightsHolderWritableItInterfacePropertiesDTO.ExposedBySystemUuid)) || enforceFallbackIfNotProvided ? dto.ExposedBySystemUuid.AsChangedValue() : OptionalValueChange<Guid>.None,
                Description = ClientRequestsChangeTo(nameof(RightsHolderWritableItInterfacePropertiesDTO.Description)) || enforceFallbackIfNotProvided ? dto.Description.AsChangedValue() : OptionalValueChange<string>.None,
                InterfaceId = ClientRequestsChangeTo(nameof(RightsHolderWritableItInterfacePropertiesDTO.InterfaceId)) || enforceFallbackIfNotProvided ? dto.InterfaceId.AsChangedValue() : OptionalValueChange<string>.None,
                Version = ClientRequestsChangeTo(nameof(RightsHolderWritableItInterfacePropertiesDTO.Version)) || enforceFallbackIfNotProvided ? dto.Version.AsChangedValue() : OptionalValueChange<string>.None,
                UrlReference = ClientRequestsChangeTo(nameof(RightsHolderWritableItInterfacePropertiesDTO.UrlReference)) || enforceFallbackIfNotProvided ? dto.UrlReference.AsChangedValue() : OptionalValueChange<string>.None
            };
        }
    }
}