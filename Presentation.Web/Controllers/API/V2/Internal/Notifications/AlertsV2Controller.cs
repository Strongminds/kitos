using Core.ApplicationServices.Notification;
using Core.DomainModel.Notification;
using Core.DomainModel.Shared;
using Presentation.Web.Infrastructure.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Presentation.Web.Models.API.V2.Internal.Response.Notifications;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Types.Notifications;

namespace Presentation.Web.Controllers.API.V2.Internal.Notifications
{
    [Route("api/v2/internal/alerts")]
    public class AlertsV2Controller : InternalApiV2Controller
    {
        private readonly IUserNotificationApplicationService _userNotificationApplicationService;

        public AlertsV2Controller(IUserNotificationApplicationService userNotificationApplicationService)
        {
            _userNotificationApplicationService = userNotificationApplicationService;
        }

        [HttpDelete]
        [Route("{alertUuid}")]
        public IActionResult DeleteAlert([FromQuery][NonEmptyGuid] Guid alertUuid)
        {
            return _userNotificationApplicationService.DeleteByUuid(alertUuid).Match(FromOperationError, NoContent);
        }

        [HttpGet]
        [Route("organization/{organizationUuid}/user/{userUuid}/{relatedEntityType}")]
        public IActionResult GetByOrganizationAndUser([FromQuery][NonEmptyGuid] Guid organizationUuid, [FromQuery][NonEmptyGuid] Guid userUuid, RelatedEntityType relatedEntityType)
        {
            return _userNotificationApplicationService.GetNotificationsForUserByUuid(organizationUuid, userUuid, relatedEntityType)
                    .Select(MapUserAlertsToDTO)
                    .Match(Ok, FromOperationError);
        }

        private static IEnumerable<AlertResponseDTO> MapUserAlertsToDTO(IEnumerable<UserNotification> userAlerts)
        {
            return userAlerts.Select(MapUserAlertsToDTO).ToList();
        }

        private static AlertResponseDTO MapUserAlertsToDTO(UserNotification userAlert) {
            return new AlertResponseDTO
            {
                AlertType = AlertType.Advis,
                Created = userAlert.Created,
                Message = userAlert.NotificationMessage,
                Name = userAlert.Name,
                Uuid = userAlert.Uuid,
                EntityUuid = userAlert.GetRelatedEntityUuid()
                
            };
        }
    }
}

