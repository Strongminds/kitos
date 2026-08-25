using System;
using System.Collections.Generic;
using Core.Abstractions.Helpers;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;

namespace Core.ApplicationServices.Authorization
{
    public class ModuleFieldsPermissionsResult
    {
        private static readonly ModuleFieldsPermissionsResult Empty = new (){Fields = new List<FieldPermissionsResult>()};

        public required IEnumerable<FieldPermissionsResult> Fields { get; set; }

        public static ModuleFieldsPermissionsResult Create(IEnumerable<FieldPermissionsResult> fields)
        {
            return new ModuleFieldsPermissionsResult
            {
                Fields = fields
            };
        }

        public static Result<ModuleFieldsPermissionsResult, OperationError> CreateFromDPRResult(IFieldAuthorizationModel fieldAuthorizationModel, Result<DataProcessingRegistration, OperationError> dprResult, Guid organizationUuid)
        {
            return dprResult.Select(dpr =>
                Create(new List<FieldPermissionsResult>
                {
                    fieldAuthorizationModel.GetFieldPermissions(dpr, ObjectHelper
                        .GetPropertyPath<DataProcessingRegistration>(
                            x => x.IsOversightCompleted), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightDate), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightRemark), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightReportLink), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightReportLinkName), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightOptionId), organizationUuid)
                })
            ).Match<Result<ModuleFieldsPermissionsResult, OperationError>>
            (
                result => result,
                error => error.FailureType == OperationFailure.Forbidden ? Empty : error
            );
        }

        public static Result<ModuleFieldsPermissionsResult, OperationError> CreateFromUsageResult(IFieldAuthorizationModel fieldAuthorizationModel, Result<ItSystemUsage, OperationError> usageResult, Guid organizationUuid)
        {
            return usageResult.Select(usage =>
                Create(new List<FieldPermissionsResult>
                {
                    fieldAuthorizationModel.GetFieldPermissions(usage, ObjectHelper
                        .GetPropertyPath<ItSystemUsage>(
                            x => x.ContainsAITechnology), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(usage, ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel), organizationUuid),
                    fieldAuthorizationModel.GetFieldPermissions(usage, ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment), organizationUuid)
                })
            ).Match<Result<ModuleFieldsPermissionsResult, OperationError>>
            (
                result => result,
                error => error.FailureType == OperationFailure.Forbidden ? Empty : error
            );
        }
    }
}
