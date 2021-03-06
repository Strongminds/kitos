﻿using Core.ApplicationServices.Authorization.Permissions;

namespace Core.ApplicationServices.Authorization
{
    public interface IPermissionVisitor
    {
        bool Visit(BatchImportPermission permission);
        bool Visit(SystemUsageMigrationPermission permission);
        bool Visit(VisibilityControlPermission permission);
        bool Visit(AdministerOrganizationRightPermission permission);
        bool Visit(DefineOrganizationTypePermission permission);
        bool Visit(CreateEntityWithVisibilityPermission permission);
        bool Visit(ViewBrokenExternalReferencesReportPermission permission);
        bool Visit(TriggerBrokenReferencesReportPermission permission);
    }
}
