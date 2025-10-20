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

        public IEnumerable<FieldPermissionsResult> Fields { get; set; }

        public static ModuleFieldsPermissionsResult Create(IEnumerable<FieldPermissionsResult> fields)
        {
            return new ModuleFieldsPermissionsResult
            {
                Fields = fields
            };
        }

        public static Result<ModuleFieldsPermissionsResult, OperationError> CreateFromDPRResult(IFieldAuthorizationModel fieldAuthorizationModel, Result<DataProcessingRegistration, OperationError> dprResult)
        {
            return dprResult.Select(dpr =>
                Create(new List<FieldPermissionsResult>
                {
                    fieldAuthorizationModel.GetFieldPermissions(dpr, ObjectHelper
                        .GetPropertyPath<DataProcessingRegistration>(
                            x => x.IsOversightCompleted)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightDate)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightRemark)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightReportLink)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        ObjectHelper
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightReportLinkName)),
                })
            ).Match<Result<ModuleFieldsPermissionsResult, OperationError>>
            (
                result => result,
                error => error.FailureType == OperationFailure.Forbidden ? Empty : error
            );
        }

        public static Result<ModuleFieldsPermissionsResult, OperationError> CreateFromUsageResult(IFieldAuthorizationModel fieldAuthorizationModel, Result<ItSystemUsage, OperationError> dprResult)
        {
            return dprResult.Select(dpr =>
                Create(new List<FieldPermissionsResult>
                {
                    fieldAuthorizationModel.GetFieldPermissions(dpr, ObjectHelper
                        .GetPropertyPath<ItSystemUsage>(
                            x => x.ContainsAITechnology))
                })
            ).Match<Result<ModuleFieldsPermissionsResult, OperationError>>
            (
                result => result,
                error => error.FailureType == OperationFailure.Forbidden ? Empty : error
            );
        }
    }
}
