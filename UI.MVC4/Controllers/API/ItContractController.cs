﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Core.ApplicationServices;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainServices;
using UI.MVC4.Models;

namespace UI.MVC4.Controllers.API
{
    public class ItContractController : GenericHierarchyApiController<ItContract, ItContractDTO>
    {
        private readonly IGenericRepository<AgreementElement> _agreementElementRepository;
        private readonly IGenericRepository<ItContractRole> _roleRepository;

        private readonly IGenericRepository<ItSystemUsage> _usageRepository;
        public ItContractController(IGenericRepository<ItContract> repository,
            IGenericRepository<ItSystemUsage> usageRepository, IGenericRepository<AgreementElement> agreementElementRepository, IGenericRepository<ItContractRole> roleRepository) 
            : base(repository)
        {
            _usageRepository = usageRepository;
            _agreementElementRepository = agreementElementRepository;
            _roleRepository = roleRepository;
        }

        public virtual HttpResponseMessage Get(string q, int orgId, [FromUri] PagingModel<ItContract> paging)
        {
            paging.Where(x => x.Name.Contains(q) && x.OrganizationId == orgId);
            return base.GetAll(paging);
        }

        public virtual HttpResponseMessage PostAgreementElement(int id, int elemId)
        {
            try
            {
                var contract = Repository.GetByKey(id);
                if (!HasWriteAccess(contract)) return Unauthorized();

                var elem = _agreementElementRepository.GetByKey(elemId);

                contract.AgreementElements.Add(elem);
                contract.LastChanged = DateTime.Now;
                contract.LastChangedByUser = KitosUser;

                Repository.Save();

                return Ok();
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public virtual HttpResponseMessage DeleteAgreementElement(int id, int elemId)
        {
            try
            {
                var contract = Repository.GetByKey(id);
                if (!HasWriteAccess(contract)) return Unauthorized();

                var elem = _agreementElementRepository.GetByKey(elemId);

                contract.AgreementElements.Remove(elem);
                contract.LastChanged = DateTime.Now;
                contract.LastChangedByUser = KitosUser;

                Repository.Save();

                return Ok();
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }
        
        /// <summary>
        /// Adds an ItSystemUsage to the list of associated ItSystemUsages for that contract
        /// </summary>
        /// <param name="id">ID of the contract</param>
        /// <param name="systemUsageId">ID of the system usage</param>
        /// <returns>List of associated ItSystemUsages</returns>
        public HttpResponseMessage PostSystemUsage(int id, int systemUsageId)
        {
            try
            {
                var contract = Repository.GetByKey(id);
                if (!HasWriteAccess(contract)) return Unauthorized();

                if (contract.AssociatedSystemUsages.Any(usage => usage.Id == systemUsageId))
                    return Conflict("The IT system is already associated with the contract");

                var systemUsage = _usageRepository.GetByKey(systemUsageId);

                contract.AssociatedSystemUsages.Add(systemUsage);
                
                contract.LastChanged = DateTime.Now;
                contract.LastChangedByUser = KitosUser;

                Repository.Update(contract);
                Repository.Save();
                
                return Ok(MapSystemUsages(contract));
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        /// <summary>
        /// Removes an ItSystemUsage from the list of associated ItSystemUsages for that contract
        /// </summary>
        /// <param name="id">ID of the contract</param>
        /// <param name="systemUsageId">ID of the system usage</param>
        /// <returns>List of associated ItSystemUsages</returns>
        public HttpResponseMessage DeleteSystemUsage(int id, int systemUsageId)
        {
            try
            {
                var contract = Repository.GetByKey(id);
                if (!HasWriteAccess(contract)) return Unauthorized();

                if (contract.AssociatedSystemUsages.All(usage => usage.Id != systemUsageId))
                    return Conflict("The IT system is not associated with the contract");

                var systemUsage = _usageRepository.GetByKey(systemUsageId);

                contract.AssociatedSystemUsages.Remove(systemUsage);

                contract.LastChanged = DateTime.Now;
                contract.LastChangedByUser = KitosUser;

                Repository.Update(contract);
                Repository.Save();
                
                return Ok(MapSystemUsages(contract));
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }
        
        public HttpResponseMessage GetHierarchy(int id, [FromUri] bool? hierarchy)
        {
            try
            {
                var itContract = Repository.GetByKey(id);

                if (itContract == null)
                    return NotFound();

                // this trick will put the first object in the result as well as the children
                var children = new [] { itContract }.SelectNestedChildren(x => x.Children);
                // gets parents only
                var parents = itContract.SelectNestedParents(x => x.Parent);
                // put it all in one result
                var contracts = children.Union(parents);
                return Ok(Map(contracts));
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetOverview(bool? overview, int organizationId, [FromUri] PagingModel<ItContract> pagingModel, [FromUri] string q)
        {
            try
            {
                //Get contracts within organization
                pagingModel.Where(contract => contract.OrganizationId == organizationId);

                //Get contracts without parents (roots)
                pagingModel.Where(contract => contract.ParentId == null);

                if (!string.IsNullOrEmpty(q)) pagingModel.Where(contract => contract.Name.Contains(q));

                var contracts = Page(Repository.AsQueryable(), pagingModel);

                var overviewDtos = AutoMapper.Mapper.Map<IEnumerable<ItContractOverviewDTO>>(contracts);

                return Ok(overviewDtos);
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetExcel(bool? csv, int organizationId, [FromUri] PagingModel<ItContract> pagingModel, [FromUri] string q)
        {
            try
            {
                //Get contracts within organization
                pagingModel.Where(contract => contract.OrganizationId == organizationId);

                if (!string.IsNullOrEmpty(q)) pagingModel.Where(contract => contract.Name.Contains(q));

                var contracts = Page(Repository.AsQueryable(), pagingModel);

                var overviewDtos = AutoMapper.Mapper.Map<IEnumerable<ItContractOverviewDTO>>(contracts);
                var roles = _roleRepository.Get().ToList();
                var list = new List<dynamic>();
                var header = new ExpandoObject() as IDictionary<string, Object>;
                header.Add("Aktiv", "Aktiv");
                header.Add("It Kontrakt", "It Kontrakt");
                header.Add("OrgUnit", "Ansv. organisationsenhed");
                header.Add("Underskriver", "KontraktUnderskriver");
                foreach (var role in roles)
                    header.Add(role.Name, role.Name);
                header.Add("Leverandor", "Leverandør");
                header.Add("Anskaffelse", "Anskaffelse");
                header.Add("driftar", "Drift/år");
                header.Add("Betalingsmodel", "Betalingsmodel");
                header.Add("Audit", "Audit");
                list.Add(header);
                foreach (var contract in overviewDtos)
                {
                    var obj = new ExpandoObject() as IDictionary<string, Object>;
                    obj.Add("Aktiv", contract.IsActive);
                    obj.Add("It Kontrakt", contract.Name);
                    obj.Add("OrgUnit", contract.ResponsibleOrganizationUnitName);
                    obj.Add("Underskriver", contract.ContractSignerName);
                    foreach (var role in roles)
                    {
                        var roleId = role.Id;
                        obj.Add(role.Name,
                                String.Join(",", contract.Rights.Where(x => x.RoleId == roleId).Select(x => x.User.Name)));
                    }
                    obj.Add("Leverandor", contract.SupplierName);
                    obj.Add("Anskaffelse", contract.AcquisitionSum);
                    obj.Add("driftar", contract.OperationSum);
                    obj.Add("Betalingsmodel", contract.PaymentModelName);
                    obj.Add("Audit", contract.FirstAuditDate);
                    list.Add(obj);
                }
                var s = list.ToCsv();
                var bytes = Encoding.Unicode.GetBytes(s);
                var stream = new MemoryStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "itsystemanvendelsesoversigt.csv" };
                return result;
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetPlan(bool? plan, int organizationId, [FromUri] PagingModel<ItContract> pagingModel, [FromUri] string q)
        {
            try
            {
                //Get contracts within organization
                pagingModel.Where(contract => contract.OrganizationId == organizationId);

                //Get contracts without parents (roots)
                pagingModel.Where(contract => contract.ParentId == null);

                if (!string.IsNullOrEmpty(q)) pagingModel.Where(contract => contract.Name.Contains(q));

                var contracts = Page(Repository.AsQueryable(), pagingModel);

                var overviewDtos = AutoMapper.Mapper.Map<IEnumerable<ItContractPlanDTO>>(contracts);

                return Ok(overviewDtos);
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetExcelPlan(bool? csvplan, int organizationId, [FromUri] PagingModel<ItContract> pagingModel, [FromUri] string q)
        {
            try
            {
                //Get contracts within organization
                pagingModel.Where(contract => contract.OrganizationId == organizationId);

                if (!string.IsNullOrEmpty(q)) pagingModel.Where(contract => contract.Name.Contains(q));

                var contracts = Page(Repository.AsQueryable(), pagingModel);

                var overviewDtos = AutoMapper.Mapper.Map<IEnumerable<ItContractPlanDTO>>(contracts);

                var list = new List<dynamic>();
                var header = new ExpandoObject() as IDictionary<string, Object>;
                header.Add("Aktiv", "Aktiv");
                header.Add("It Kontrakt", "It Kontrakt");
                header.Add("Type", "Kontrakttype");
                header.Add("Skabelon", "Kontraktskabelon");
                header.Add("Pur", "Indkøbsform");
                header.Add("Indgaet", "Indgået");
                header.Add("Varighed", "Varighed");
                header.Add("Udlobsdato", "Udløbsdato");
                header.Add("Option", "Option");
                header.Add("Opsigelse", "Opsigelse");
                header.Add("Uopsigelig", "Uopsigelig til");
                header.Add("Udbudsstrategi", "Udbudsstrategi");
                header.Add("Udbudsplan", "Udbudsplan");
                list.Add(header);
                foreach (var contract in overviewDtos)
                {
                    var obj = new ExpandoObject() as IDictionary<string, Object>;
                    obj.Add("Aktiv", contract.IsActive);
                    obj.Add("It Kontrakt", contract.Name);
                    obj.Add("Type", contract.ContractTypeName);
                    obj.Add("Skabelon", contract.ContractTemplateName);
                    obj.Add("Pur", contract.PurchaseFormName);
                    obj.Add("Indgaet", contract.Concluded);
                    obj.Add("Varighed", contract.Duration);
                    obj.Add("Udlobsdato", contract.ExpirationDate);
                    obj.Add("Option", contract.OptionExtendName);
                    obj.Add("Opsigelse", contract.TerminationDeadlineName);
                    obj.Add("Uopsigelig", contract.IrrevocableTo);
                    obj.Add("Udbudsstrategi", contract.ProcurementStrategyName);
                    obj.Add("Udbudsplan", contract.ProcurementPlanHalf + " | " + contract.ProcurementPlanYear);
                    list.Add(obj);
                }
                var s = list.ToCsv();
                var bytes = Encoding.Unicode.GetBytes(s);
                var stream = new MemoryStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "itsystemanvendelsesoversigt.csv" };
                return result;
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        private IEnumerable<ItSystemUsageSimpleDTO> MapSystemUsages(ItContract contract)
        {
            return Map<IEnumerable<ItSystemUsage>, IEnumerable<ItSystemUsageSimpleDTO>>(contract.AssociatedSystemUsages);
        }
    }
}