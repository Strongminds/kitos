﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Web.Http;
using Core.Abstractions.Types;
using Core.ApplicationServices;
using Core.ApplicationServices.Contract;
using Core.DomainModel;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Model.Options;
using Newtonsoft.Json.Linq;
using Presentation.Web.Controllers.API.V1.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.ItContract;
using Presentation.Web.Models.API.V1.Shared;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V1
{
    [PublicApi]
    [RoutePrefix("api/itcontract")]
    public class ItContractController : GenericHierarchyApiController<ItContract, ItContractDTO>
    {
        private readonly IGenericRepository<AgreementElementType> _agreementElementRepository;
        private readonly IGenericRepository<ItContractItSystemUsage> _itContractItSystemUsageRepository;
        private readonly IGenericRepository<ItSystemUsage> _usageRepository;
        private readonly IItContractService _itContractService;

        public ItContractController(IGenericRepository<ItContract> repository,
            IGenericRepository<ItSystemUsage> usageRepository,
            IGenericRepository<AgreementElementType> agreementElementRepository,
            IGenericRepository<ItContractItSystemUsage> itContractItSystemUsageRepository,
            IItContractService itContractService)
            : base(repository)
        {
            _usageRepository = usageRepository;
            _agreementElementRepository = agreementElementRepository;
            _itContractItSystemUsageRepository = itContractItSystemUsageRepository;
            _itContractService = itContractService;
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<NamedEntityDTO>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public virtual HttpResponseMessage Get(string q, int orgId, [FromUri] PagingModel<ItContract> paging)
        {
            var contractQuery = _itContractService.GetAllByOrganization(orgId, q);

            var contractDTO = Page(contractQuery, paging)
                .AsEnumerable()
                .MapToNamedEntityDTOs()
                .ToList();

            return Ok(contractDTO);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<ItContractDTO>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public override HttpResponseMessage GetSingle(int id)
        {

            try
            {
                var item = Repository.GetByKey(id);

                if (item == null)
                {
                    return NotFound();
                }

                if (!AllowRead(item))
                {
                    return Forbidden();
                }

                var dto = Map(item);

                if (GetOrganizationReadAccessLevel(item.OrganizationId) != OrganizationDataReadAccessLevel.All)
                {
                    dto.Note = "";
                }

                return Ok(dto);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public virtual HttpResponseMessage PostAgreementElement(int id, int organizationId, int elemId)
        {
            try
            {
                var contract = Repository.GetByKey(id);

                if (contract == null)
                {
                    return NotFound();
                }

                if (!AllowModify(contract))
                {
                    return Forbidden();
                }

                var elem = _agreementElementRepository.GetByKey(elemId);

                contract.AssociatedAgreementElementTypes.Add(new ItContractAgreementElementTypes
                {
                    AgreementElementType_Id = elem.Id,
                    ItContract_Id = contract.Id
                });

                RaiseUpdated(contract);
                Repository.Save();

                return Ok();
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public virtual HttpResponseMessage DeleteAgreementElement(int id, int organizationId, int elemId)
        {
            try
            {
                var contract = Repository.GetByKey(id);

                if (contract == null)
                {
                    return NotFound();
                }

                if (!AllowModify(contract))
                {
                    return Forbidden();
                }

                var elem = _agreementElementRepository.GetByKey(elemId);

                var relation = contract.AssociatedAgreementElementTypes.FirstOrDefault(e => e.AgreementElementType_Id == elem.Id);
                contract.AssociatedAgreementElementTypes.Remove(relation);

                RaiseUpdated(contract);
                Repository.Save();

                return Ok();
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        /// <summary>
        /// Adds an ItSystemUsage to the list of associated ItSystemUsages for that contract
        /// </summary>
        /// <param name="id">ID of the contract</param>
        /// <param name="organizationId"></param>
        /// <param name="systemUsageId">ID of the system usage</param>
        /// <returns>List of associated ItSystemUsages</returns>
        public HttpResponseMessage PostSystemUsage(int id, int organizationId, int systemUsageId)
        {
            try
            {
                var contract = Repository.GetByKey(id);
                if (contract == null)
                {
                    return NotFound();
                }

                if (!AllowModify(contract))
                {
                    return Forbidden();
                }

                var usage = _usageRepository.GetByKey(systemUsageId);

                if (usage == null)
                {
                    return BadRequest($"System usage with id {systemUsageId} not found");
                }

                if (_itContractItSystemUsageRepository.GetByKey(new object[] { id, systemUsageId }) != null)
                    return Conflict("The IT system usage is already associated with the contract");

                contract.AssociatedSystemUsages.Add(new ItContractItSystemUsage { ItContractId = id, ItSystemUsageId = systemUsageId });

                RaiseUpdated(contract);
                Repository.Save();

                return Ok(MapSystemUsages(contract));
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        /// <summary>
        /// Removes an ItSystemUsage from the list of associated ItSystemUsages for that contract
        /// </summary>
        /// <param name="id">ID of the contract</param>
        /// <param name="organizationId"></param>
        /// <param name="systemUsageId">ID of the system usage</param>
        /// <returns>List of associated ItSystemUsages</returns>
        public HttpResponseMessage DeleteSystemUsage(int id, int organizationId, int systemUsageId)
        {
            try
            {
                var contract = Repository.GetByKey(id);

                if (contract == null)
                {
                    return NotFound();
                }

                if (!AllowModify(contract))
                {
                    return Forbidden();
                }

                var contractItSystemUsage = _itContractItSystemUsageRepository.GetByKey(new object[] { id, systemUsageId });
                if (contractItSystemUsage == null)
                    return Conflict("The IT system is not associated with the contract");

                contract.AssociatedSystemUsages.Remove(contractItSystemUsage);

                RaiseUpdated(contract);
                Repository.Save();

                return Ok(MapSystemUsages(contract));
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<ItContractDTO>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public HttpResponseMessage GetHierarchy(int id, [FromUri] bool? hierarchy)
        {
            try
            {
                var itContract = Repository.GetByKey(id);

                if (itContract == null)
                    return NotFound();

                if (!AllowRead(itContract))
                {
                    return Forbidden();
                }
                // this trick will put the first object in the result as well as the children
                var children = new[] { itContract }.SelectNestedChildren(x => x.Children);
                // gets parents only
                var parents = itContract.SelectNestedParents(x => x.Parent);
                // put it all in one result
                var contracts = children.Union(parents);
                return Ok(Map(contracts));
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [HttpGet]
        [Route("{id}/data-processing-registration/available")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public HttpResponseMessage GetAvailableDataProcessingRegistrations(int id, [FromUri] string nameQuery = null, [FromUri] int pageSize = 25)
        {
            return _itContractService
                .GetDataProcessingRegistrationsWhichCanBeAssigned(id, nameQuery, pageSize)
                .Match(systems => Ok(systems.Select(x => x.MapToNamedEntityDTO()).ToList()), FromOperationError);
        }

        [HttpPatch]
        [Route("{id}/data-processing-registration/assign")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        public HttpResponseMessage AssignOversightOption(int id, [FromBody] SingleValueDTO<int> dataProcessingRegistrationId)
        {
            if (dataProcessingRegistrationId == null)
                return BadRequest($"{nameof(dataProcessingRegistrationId)} must be provided");

            return _itContractService
                .AssignDataProcessingRegistration(id, dataProcessingRegistrationId.Value)
                .Match(_ => Ok(), FromOperationError);
        }

        [HttpPatch]
        [Route("{id}/data-processing-registration/remove")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public HttpResponseMessage RemoveOversightOption(int id, [FromBody] SingleValueDTO<int> dataProcessingRegistrationId)
        {
            if (dataProcessingRegistrationId == null)
                return BadRequest($"{nameof(dataProcessingRegistrationId)} must be provided");

            return _itContractService
                .RemoveDataProcessingRegistration(id, dataProcessingRegistrationId.Value)
                .Match(_ => Ok(), FromOperationError);
        }

        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Conflict, Description = "It Contract names must be unique within the organization")]
        public HttpResponseMessage GetCanCreateNewProjectWithName(string checkname, int orgId)
        {
            try
            {
                return _itContractService
                    .CanCreateNewContractWithName(checkname, orgId)
                    .Select(result => result ? Ok() : Conflict("Name taken"))
                    .Match(result => result, FromOperationError);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }
        
        [HttpGet]
        [Route("available-options-in/{organizationId}")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public HttpResponseMessage GetContractOptions(int organizationId)
        {
            return _itContractService
                .GetAssignableContractOptions(organizationId)
                .Select(result => new ContractOptionsDTO
                {
                    CriticalityOptions = ToDTOs(result.CriticalityOptions, organizationId).ToList()
                })
                .Match(Ok, FromOperationError);
        }

        private IEnumerable<ItSystemUsageSimpleDTO> MapSystemUsages(ItContract contract)
        {
            return Map<IEnumerable<ItSystemUsage>, IEnumerable<ItSystemUsageSimpleDTO>>(contract.AssociatedSystemUsages.Select(x => x.ItSystemUsage));
        }

        protected override void DeleteQuery(ItContract entity)
        {
            var result = _itContractService.Delete(entity.Id);
            if (result.Ok == false)
            {
                switch (result.Error)
                {
                    case OperationFailure.Forbidden:
                        throw new SecurityException();
                    default:
                        throw new InvalidOperationException(result.Error.ToString("G"));
                }
            }
        }

        protected override void RaiseDeleted(ItContract entity)
        {
            //Delete is handled by the service so prevent duplicate deletion tracking
        }

        protected override void PrepareNewObject(ItContract newContract)
        {
            newContract.Uuid = Guid.NewGuid();
            base.PrepareNewObject(newContract);
        }

        public override HttpResponseMessage Patch(int id, int organizationId, JObject obj)
        {
            if (obj.TryGetValue(nameof(ItContract.Name), StringComparison.OrdinalIgnoreCase, out var nameToken))
            {
                var name = nameToken.ToObject<string>();
                var validationError = _itContractService.ValidateNewName(id, name);

                if (validationError.HasValue)
                    return FromOperationError(validationError.Value);
            }
            return base.Patch(id, organizationId, obj);
        }

        public override HttpResponseMessage Post(int organizationId, ItContractDTO dto)
        {
            return _itContractService
                .Create(organizationId, dto.Name)
                .Match(NewObjectCreated, FromOperationError);
        }

        private static IEnumerable<OptionWithDescriptionAndExpirationDTO> ToDTOs<T>(IEnumerable<(OptionDescriptor<T> option, bool available)> options, int organizationId) where T : OptionEntity<ItContract>
        {
            return options.Select(ToDTO);
        }

        private static OptionWithDescriptionAndExpirationDTO ToDTO<T>((OptionDescriptor<T> option, bool available) optionObject) where T : OptionEntity<ItContract>
        {
            return new OptionWithDescriptionAndExpirationDTO(optionObject.option.Option.Id, optionObject.option.Option.Name, optionObject.available == false, optionObject.option.Description);
        }
    }
}
