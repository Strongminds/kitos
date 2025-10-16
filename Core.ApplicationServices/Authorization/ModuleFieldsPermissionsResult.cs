using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;

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
                    fieldAuthorizationModel.GetFieldPermissions(dpr, SupplierAssociatedFieldsService
                        .GetPropertyPath<DataProcessingRegistration>(
                            x => x.IsOversightCompleted)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        SupplierAssociatedFieldsService
                            .GetPropertyPath<DataProcessingRegistrationOversightDate>(
                                x => x.OversightDate)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        SupplierAssociatedFieldsService
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightRemark)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        SupplierAssociatedFieldsService
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightReportLink)),
                    fieldAuthorizationModel.GetFieldPermissions(dpr,
                        SupplierAssociatedFieldsService
                            .GetPropertyPath< DataProcessingRegistrationOversightDate >(
                                x => x.OversightReportLinkName)),
                })
            ).Match<Result<ModuleFieldsPermissionsResult, OperationError>>
            (
                result => result,
                error => error.FailureType == OperationFailure.Forbidden ? Empty : error
            );
        }
    }
}
