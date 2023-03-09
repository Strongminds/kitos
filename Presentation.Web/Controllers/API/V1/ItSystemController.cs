﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.ApplicationServices.Model.System;
using Core.ApplicationServices.System;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;
using Core.DomainServices.Time;
using Newtonsoft.Json.Linq;
using Presentation.Web.Controllers.API.V1.Mapping;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.ItSystem;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V1
{
    [InternalApi]
    public class ItSystemController : GenericHierarchyApiController<ItSystem, ItSystemDTO>
    {
        private readonly IGenericRepository<TaskRef> _taskRepository;
        private readonly IItSystemService _systemService;
        private readonly IOperationClock _operationClock;

        public ItSystemController(
            IGenericRepository<ItSystem> repository,
            IGenericRepository<TaskRef> taskRepository,
            IItSystemService systemService,
            IOperationClock operationClock)
            : base(repository)
        {
            _taskRepository = taskRepository;
            _systemService = systemService;
            _operationClock = operationClock;
        }


        // DELETE api/T
        public override HttpResponseMessage Delete(int id, int organizationId)
        {
            var deleteResult = _systemService.Delete(id);
            switch (deleteResult)
            {
                case SystemDeleteResult.Forbidden:
                    return Forbidden();
                case SystemDeleteResult.NotFound:
                    return NotFound();
                case SystemDeleteResult.InUse:
                case SystemDeleteResult.HasChildren:
                case SystemDeleteResult.HasInterfaceExhibits:
                    return DeleteConflict(deleteResult.MapToConflict());
                case SystemDeleteResult.Ok:
                    return Ok();
                default:
                    return Error($"Something went wrong trying to delete system with id: {id}");
            }
        }

        private HttpResponseMessage DeleteConflict(SystemDeleteConflict response)
        {
            var responseAsString = response.ToString("G");
            return CreateResponse(HttpStatusCode.Conflict, responseAsString, responseAsString);
        }

        /// <summary>
        /// Henter alle IT-Systemer i organisationen samt offentlige IT Systemer fra andre organisationer
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="paging"></param>
        /// <param name="q">Mulighed for søgning på navneindhold</param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<NamedEntityWithEnabledStatusDTO>>))]
        public HttpResponseMessage GetPublic([FromUri] int organizationId, [FromUri] PagingModel<ItSystem> paging, [FromUri] string q)
        {
            try
            {
                var systemQuery = _systemService.GetAvailableSystems(organizationId, q);

                var query = Page(systemQuery, paging)
                    .AsEnumerable()
                    .Select(system => system.MapToNamedEntityWithEnabledStatusDTO())
                    .ToList();

                return Ok(query);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<NamedEntityWithEnabledStatusDTO>>))]
        public HttpResponseMessage GetInterfacesSearch(string q, int orgId, int excludeId, int take = 25)
        {
            try
            {
                var systems = _systemService
                    .GetAvailableSystems(orgId, q)
                    .ExceptEntitiesWithIds(excludeId)
                    .OrderBy(_ => _.Name)
                    .Take(take)
                    .AsEnumerable()
                    .Select(system => system.MapToNamedEntityWithEnabledStatusDTO())
                    .ToList();


                return Ok(systems);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<ItSystemDTO>>))]
        public HttpResponseMessage GetHierarchy(int id, [FromUri] bool hierarchy)
        {
            try
            {
                return _systemService.GetHierarchy(id)
                    .Select(Map)
                    .Match(Ok, FromOperationError);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public HttpResponseMessage Post(CreateItSystemDTO dto)
        {
            try
            {
                if (!CanCreateSystemWithName(dto.Name, dto.OrganizationId))
                {
                    return Conflict("Name is already taken!");
                }

                var item = new ItSystem
                {
                    Name = dto.Name,
                    OrganizationId = dto.OrganizationId,
                    BelongsToId = dto.OrganizationId,
                    Uuid = Guid.NewGuid(),
                    AccessModifier = dto.AccessModifier ?? AccessModifier.Public,
                    Created = _operationClock.Now
                };

                if (!AllowCreate<ItSystem>(dto.OrganizationId, item))
                {
                    return Forbidden();
                }

                var savedItem = PostQuery(item);
                RaiseNewObjectCreated(savedItem);
                return Created(Map(savedItem), new Uri(Request.RequestUri + "/" + savedItem.Id));
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [NonAction]
        public override HttpResponseMessage Post(int organizationId, ItSystemDTO dto) => throw new NotSupportedException();

        public HttpResponseMessage PostTasksUsedByThisSystem(int id, int organizationId, [FromUri] int? taskId)
        {
            try
            {
                var system = Repository.GetByKey(id);
                if (system == null) return NotFound();

                if (!AllowModify(system))
                {
                    return Forbidden();
                }

                List<TaskRef> tasks;
                if (taskId.HasValue)
                {
                    // get child leaves of taskId that havn't got a usage in the system
                    tasks = _taskRepository.Get(
                        x =>
                            (x.Id == taskId || x.ParentId == taskId || x.Parent.ParentId == taskId) && !x.Children.Any() &&
                            x.ItSystems.All(y => y.Id != id)).ToList();
                }
                else
                {
                    // no taskId was specified so get everything
                    tasks = _taskRepository.Get(
                        x =>
                            !x.Children.Any() &&
                            x.ItSystems.All(y => y.Id != id)).ToList();
                }

                if (!tasks.Any())
                    return NotFound();

                foreach (var task in tasks)
                {
                    system.TaskRefs.Add(task);
                }
                RaiseUpdated(system);
                Repository.Save();
                return Ok();
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public HttpResponseMessage DeleteTasksUsedByThisSystem(int id, int organizationId, [FromUri] int? taskId)
        {
            try
            {
                var system = Repository.GetByKey(id);
                if (system == null)
                {
                    return NotFound();
                }

                if (!AllowModify(system))
                {
                    return Forbidden();
                }

                List<TaskRef> tasks;
                if (taskId.HasValue)
                {
                    tasks =
                        system.TaskRefs.Where(
                            x =>
                                (x.Id == taskId || x.ParentId == taskId || x.Parent?.ParentId == taskId) &&
                                !x.Children.Any())
                            .ToList();
                }
                else
                {
                    // no taskId was specified so get everything
                    tasks = system.TaskRefs.ToList();
                }

                if (!tasks.Any())
                    return NotFound();

                foreach (var task in tasks)
                {
                    system.TaskRefs.Remove(task);
                }
                RaiseUpdated(system);
                Repository.Save();
                return Ok();
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        /// <summary>
        /// Returns a list of task ref and whether or not they are currently associated with a given system
        /// </summary>
        /// <param name="id">ID of the system</param>
        /// <param name="tasks">Routing qualifer</param>
        /// <param name="onlySelected">If true, only return those task ref that are associated with the system. If false, return all task ref.</param>
        /// <param name="taskGroup">Optional filtering on task group</param>
        /// <param name="pagingModel">Paging model</param>
        /// <returns>List of TaskRefSelectedDTO</returns>
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<List<TaskRefSelectedDTO>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public HttpResponseMessage GetTasks(int id, bool? tasks, bool onlySelected, int? taskGroup, [FromUri] PagingModel<TaskRef> pagingModel)
        {
            try
            {
                var system = Repository.GetByKey(id);
                if (system == null)
                {
                    return NotFound();
                }

                if (!AllowRead(system))
                {
                    return Forbidden();
                }

                IQueryable<TaskRef> taskQuery = _taskRepository.AsQueryable();
                if (onlySelected)
                {
                    taskQuery = Repository.AsQueryable().Where(p => p.Id == id).SelectMany(p => p.TaskRefs);
                }

                // if a task group is given, only find the tasks in that group
                if (taskGroup.HasValue)
                    pagingModel.Where(taskRef => (taskRef.ParentId.Value == taskGroup.Value ||
                                                  taskRef.Parent.ParentId.Value == taskGroup.Value) &&
                                                 !taskRef.Children.Any());
                else
                    pagingModel.Where(taskRef => taskRef.Children.Count == 0);

                var theTasks = Page(taskQuery, pagingModel).ToList();

                var dtos = theTasks.Select(task => new TaskRefSelectedDTO()
                {
                    TaskRef = Map<TaskRef, TaskRefDTO>(task),
                    IsSelected = onlySelected || system.TaskRefs.Any(t => t.Id == task.Id)
                }).ToList(); // must call .ToList here else the output will be wrapped in $type,$values

                return Ok(dtos);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public override HttpResponseMessage Patch(int id, int organizationId, JObject obj)
        {
            // try get AccessModifier value
            JToken accessModToken;
            obj.TryGetValue("accessModifier", out accessModToken);

            var itSystem = Repository.GetByKey(id);

            if (itSystem == null)
            {
                return NotFound();
            }

            if (accessModToken != null && accessModToken.ToObject<AccessModifier>() == AccessModifier.Public && !AllowEntityVisibilityControl(itSystem))
            {
                return Forbidden();
            }

            // try get name value
            JToken nameToken;
            obj.TryGetValue("name", out nameToken);
            var namechange = false;
            if (nameToken != null)
            {
                string name = nameToken.Value<string>();
                namechange = name != itSystem.Name;
                var allowed = _systemService.CanChangeNameTo(organizationId, id, name);
                if (!allowed)
                    return Conflict("Name is already taken!");

            }

            if (obj.TryGetValue(nameof(ItSystem.Uuid), StringComparison.OrdinalIgnoreCase, out var uuidToken) &&
                uuidToken.ToObject<Guid>() != itSystem.Uuid)
            {
                return BadRequest("Cannot change uuid");
            }

            var httpResponseMessage = base.Patch(id, organizationId, obj);

            if (httpResponseMessage.IsSuccessStatusCode && namechange)
            {
                RaiseUpdated(itSystem);
            }

            return httpResponseMessage;
        }

        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Conflict, Description = "It System names must be new")]
        public HttpResponseMessage GetNameAvailable(string checkname, int orgId)
        {
            try
            {
                if (GetOrganizationReadAccessLevel(orgId) == OrganizationDataReadAccessLevel.None)
                {
                    return Forbidden();
                }
                return CanCreateSystemWithName(checkname, orgId) ? Ok() : Conflict("Name is already taken!");
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        [HttpGet]
        [Route("api/v1/ItSystem/{id}/usingOrganizations")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<IEnumerable<UsingOrganizationDTO>>))]
        public HttpResponseMessage GetUsingOrganizations([FromUri] int id)
        {
            var itSystemUsages = _systemService.GetUsingOrganizations(id);
            if (itSystemUsages.Ok)
            {
                var dto = Map(itSystemUsages.Value);
                return Ok(dto);
            }

            return FromOperationFailure(itSystemUsages.Error);
        }

        private bool CanCreateSystemWithName(string name, int orgId)
        {
            return _systemService.CanCreateSystemWithName(orgId, name);
        }

        private static IEnumerable<UsingOrganizationDTO> Map(IEnumerable<UsingOrganization> usingOrganizations)
        {
            return usingOrganizations
                .Select(
                usingOrganization => new UsingOrganizationDTO
                {
                    SystemUsageUuid = usingOrganization.ItSystemUsageUuid,
                    Organization = usingOrganization.Organization.MapToNamedEntityDTO(),
                    OrganizationUuid = usingOrganization.OrganizationUuid
                })
                .ToList();
        }
    }
}
