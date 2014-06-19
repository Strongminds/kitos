﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Web.Http;
using Core.DomainModel;
using Core.DomainServices;
using Newtonsoft.Json.Linq;
using UI.MVC4.Models;

namespace UI.MVC4.Controllers.API
{
    public class OrganizationUnitController : GenericApiController<OrganizationUnit, OrgUnitDTO>
    {
        private readonly IOrgUnitService _orgUnitService;
        private readonly IAdminService _adminService;
        private readonly IGenericRepository<TaskRef> _taskRepository;
        private readonly IGenericRepository<TaskUsage> _taskUsageRepository;

        public OrganizationUnitController(IGenericRepository<OrganizationUnit> repository,
            IOrgUnitService orgUnitService, IAdminService adminService, IGenericRepository<TaskRef> taskRepository, IGenericRepository<TaskUsage> taskUsageRepository ) 
            : base(repository)
        {
            _orgUnitService = orgUnitService;
            _adminService = adminService;
            _taskRepository = taskRepository;
            _taskUsageRepository = taskUsageRepository;
        }

        //TODO probably don't use this, use get by organization instead
        /*public HttpResponseMessage GetByUser(int userId)
        {
            try
            {
                var user = KitosUser;

                if(user.Id != userId) throw new SecurityException();

                var orgUnits = _orgUnitService.GetByUser(user);

                return Ok(Map<IEnumerable<OrganizationUnit>, IEnumerable<OrgUnitDTO>>(orgUnits));

            }
            catch (Exception e)
            {
                return Error(e);
            }
        }*/

        /// <summary>
        /// Returns every OrganizationUnit that the user can select as the default unit
        /// </summary>
        /// <param name="byUser">Routing qualifier</param>
        /// <returns></returns>
        public HttpResponseMessage GetByUser(bool? byUser)
        {
            try
            {
                var orgUnits = Repository.Get(x => x.Rights.Any(y => y.UserId == KitosUser.Id)).ToList();

                if (KitosUser.CreatedIn != null)
                {
                    var rootOrgUnit = KitosUser.CreatedIn.GetRoot();

                    orgUnits.Add(rootOrgUnit);
                }

                orgUnits = orgUnits.Distinct().ToList();

                return Ok(Map<IEnumerable<OrganizationUnit>, IEnumerable<OrgUnitSimpleDTO>>(orgUnits));
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetByOrganization(int organization)
        {
            try
            {
                var orgUnit = Repository.Get(o => o.OrganizationId == organization && o.Parent == null).FirstOrDefault();

                if (orgUnit == null) return NotFound();

                var item = Map<OrganizationUnit, OrgUnitDTO>(orgUnit);

                return Ok(item);
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public HttpResponseMessage GetByOrganizationFlat(int organizationId)
        {
            try
            {
                var orgUnit = Repository.Get(o => o.OrganizationId == organizationId);

                return Ok(Map(orgUnit));
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        public override HttpResponseMessage Patch(int id, JObject obj)
        {
            try
            {
                JToken jtoken;
                if (obj.TryGetValue("parentId", out jtoken))
                {
                    //TODO: You have to be local or global admin to change parent

                    var parentId = jtoken.Value<int>();
                    
                    //if the new parent is actually a descendant of the item, don't update - this would create a loop!
                    if (_orgUnitService.IsAncestorOf(parentId, id))
                    {
                        return Conflict("OrgUnit loop detected");
                    }
                }

            }
            catch (Exception e)
            {
                return Error(e);
            }
            return base.Patch(id, obj);
        }

        public override HttpResponseMessage Put(int id, JObject jObject)
        {
            return NotAllowed();
        }

        /// <summary>
        /// Returns every task that a given OrgUnit can use. This depends on the task usages of the parent OrgUnit.
        /// For every task returned, possibly a taskUsage is returned too, if the OrgUnit is currently using that task.
        /// </summary>
        /// <param name="id">ID of the OrgUnit</param>
        /// <param name="taskGroup">Optional id of a taskgroup</param>
        /// <param name="tasks">Routing qualifier</param>
        /// <param name="pagingModel">Paging options</param>
        /// <returns>List of (task, taskUsage), where the taskUsage might be null</returns>
        public HttpResponseMessage GetAccessibleTasks(int id, int? taskGroup, bool? tasks, [FromUri] PagingModel<TaskRef> pagingModel)
        {
            try
            {
                var orgUnit = Repository.GetByKey(id);

                //if the org unit has a parent, only select those tasks that is in use by the parent org unit
                if (orgUnit.ParentId.HasValue)
                    pagingModel.Where(taskRef => taskRef.Usages.Any(usage => usage.OrgUnitId == orgUnit.ParentId));

                //if a task group is given, only find the tasks in that group
                if (taskGroup.HasValue) pagingModel.Where(taskRef => taskRef.ParentId == taskGroup);

                var taskQuery = _taskRepository.AsQueryable();
                var theTasks = Page(taskQuery, pagingModel);

                //convert tasks to DTO containing both the task and possibly also a taskUsage, if that exists
                var dtos = (from taskRef in theTasks
                           let taskUsage = taskRef.Usages.FirstOrDefault(usage => usage.OrgUnitId == id)
                           select new TaskRefUsageDTO()
                               {
                                   TaskRef = Map<TaskRef, TaskRefDTO>(taskRef),
                                   TaskUsage = Map<TaskUsage, TaskUsageDTO>(taskUsage)
                               });

                return Ok(dtos);
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }

        /// <summary>
        /// Returns the task usages of a given OrgUnit.
        /// </summary>
        /// <param name="id">ID of the OrgUnit</param>
        /// <param name="taskGroup">Optional id of a taskgroup</param>
        /// <param name="usages">Routing qualifier</param>
        /// <param name="pagingModel">Paging options</param>
        /// <returns></returns>
        public HttpResponseMessage GetTaskUsages(int id, int? taskGroup, bool? usages,
                                                 [FromUri] PagingModel<TaskUsage> pagingModel)
        {
            try
            {
                var usageQuery = _taskUsageRepository.AsQueryable();

                pagingModel.Where(usage => usage.OrgUnitId == id);

                var theUsages = Page(usageQuery, pagingModel);

                var dtos = Map<IEnumerable<TaskUsage>, IEnumerable<TaskUsageDTO>>(theUsages);

                return Ok(dtos);
            }
            catch (Exception e)
            {
                return Error(e);
            }
        }
    }
}
