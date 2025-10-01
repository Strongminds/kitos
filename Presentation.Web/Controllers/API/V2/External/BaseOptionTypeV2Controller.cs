using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel;
using Core.DomainServices.Model.Options;
using Presentation.Web.Extensions;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;

namespace Presentation.Web.Controllers.API.V2.External
{
    public abstract class BaseOptionTypeV2Controller<TParent,TOption, TCollectionEntryDTO, TExtendedDto> : ExternalBaseController where TOption : OptionEntity<TParent>
    {
        private readonly IOptionsApplicationService<TParent, TOption> _optionApplicationService;

        protected BaseOptionTypeV2Controller(IOptionsApplicationService<TParent, TOption> optionApplicationService)
        {
            _optionApplicationService = optionApplicationService;
        }

        protected IHttpActionResult GetAll(Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return GetAvailableOptions(organizationUuid, pagination);
        }

        protected IHttpActionResult GetSingle(Guid optionUuid, Guid organizationUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _optionApplicationService
                .GetOptionType(organizationUuid, optionUuid)
                .Select(x => ToExtendedDTO(x.option, x.available))
                .Match(Ok, FromOperationError);
        }

        protected abstract IHttpActionResult GetAvailableOptions(Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null);

        protected abstract TCollectionEntryDTO ToDTO(OptionDescriptor<TOption> option);

        protected abstract TExtendedDto ToExtendedDTO(OptionDescriptor<TOption> option, bool isAvailable);
    }
}