﻿using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Core.DomainModel;
using Core.DomainModel.ItContract;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.OData
{
    [Authorize]
    [PublicApi]
    public class EconomyStreamsController : BaseEntityController<EconomyStream>
    {
        private readonly IGenericRepository<EconomyStream> _repository;

        public EconomyStreamsController(IGenericRepository<EconomyStream> repository)
            : base(repository)
        {
            _repository = repository;
        }

        [NonAction]
        public override IHttpActionResult Post(int organizationId, EconomyStream entity) => throw new NotSupportedException();

        [NonAction]
        public override IHttpActionResult Patch(int key, Delta<EconomyStream> delta) => throw new NotSupportedException();

        [NonAction]
        public override IHttpActionResult Delete(int key) => throw new NotSupportedException();

        [NonAction]
        public override IHttpActionResult Get() => throw new NotSupportedException();

        [NonAction]
        public override IHttpActionResult Get(int key) => throw new NotSupportedException();

        // GET /Organizations(1)/ItContracts
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All & ~AllowedQueryOptions.Expand)]
        [ODataRoute("ExternEconomyStreams(Organization={orgKey})")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IQueryable<EconomyStream>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [RequireTopOnOdataThroughKitosToken]
        public IHttpActionResult GetByOrganization(int orgKey)
        {
            var result =
                _repository.AsQueryable()
                    .Where(
                        m =>
                            m.ExternPaymentFor.OrganizationId == orgKey &&
                            m.InternPaymentFor == null);

            var economyStream = result.FirstOrDefault();

            var accessLevel = GetOrganizationReadAccessLevel(orgKey);

            if (economyStream != null)
            {
                var contractId = economyStream.ExternPaymentFor.Id;

                var economyStreamIsPublic = EconomyStreamIsPublic(contractId);

                if (accessLevel < OrganizationDataReadAccessLevel.All && economyStreamIsPublic == false)
                {
                    return Forbidden();
                }

                if (economyStreamIsPublic && accessLevel < OrganizationDataReadAccessLevel.Public)
                {
                    return Forbidden();
                }

            }
            //No access to organization -> forbidden, not empty response
            else if (accessLevel < OrganizationDataReadAccessLevel.Public)
            {
                return Forbidden();
            }

            return Ok(result);
        }

        private bool EconomyStreamIsPublic(int contractKey)
        {
            if (contractKey == 0)
            {
                // contractKey is zero by default if GetByOrganization does not find any EconomyStreams
                return false;
            }

            var economyStream = _repository.AsQueryable()
                .FirstOrDefault(e => e.ExternPaymentFor.Id == contractKey || e.InternPaymentFor.Id == contractKey);

            return economyStream != null && economyStream.AccessModifier == AccessModifier.Public;
        }
    }
}
