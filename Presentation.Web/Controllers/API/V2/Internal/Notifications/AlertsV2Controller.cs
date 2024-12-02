﻿using Core.ApplicationServices.Notification;
using Core.DomainModel.Notification;
using Core.DomainModel.Shared;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Presentation.Web.Controllers.API.V2.Internal.Notifications
{
    [RoutePrefix("api/v2/internal/alerts")]
    public class AlertsV2Controller : InternalApiV2Controller
    {
        private readonly IUserNotificationApplicationService _userNotificationApplicationService;

        public AlertsV2Controller(IUserNotificationApplicationService userNotificationApplicationService)
        {
            _userNotificationApplicationService = userNotificationApplicationService;
        }

        [HttpDelete]
        [Route("{alertUuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult DeleteAlert([FromUri][NonEmptyGuid] Guid alertUuid)
        {
            return _userNotificationApplicationService.DeleteByUuid(alertUuid).Match(FromOperationError, NoContent);
        }

        [HttpGet]
        [Route("organization/{organizationUuid}/user/{userUuid}/{relatedEntityType}/all")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<object>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetByOrganizationAndUser([FromUri][NonEmptyGuid] Guid organizationUuid, [FromUri][NonEmptyGuid] Guid userUuid, RelatedEntityType relatedEntityType)
        {
            return _userNotificationApplicationService.GetNotificationsForUserByUuid(organizationUuid, userUuid, relatedEntityType)
                    .Select(MapUserNotificationsToDTOs)
                    .Match(Ok, FromOperationError);
        }

        private IEnumerable<object> MapUserNotificationsToDTOs(IEnumerable<UserNotification> userNotifcations)
        {
            return userNotifcations.Select(MapUserNotificationToDTO).ToList();
        }

        private object MapUserNotificationToDTO(UserNotification userNotification) {
            return userNotification;
        }
    }
}