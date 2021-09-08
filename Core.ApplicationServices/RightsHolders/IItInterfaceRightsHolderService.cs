﻿using Core.ApplicationServices.Model.Interface;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;

namespace Core.ApplicationServices.RightsHolders
{
    public interface IItInterfaceRightsHolderService
    {
        Result<IQueryable<ItInterface>, OperationError> GetInterfacesWhereAuthenticatedUserHasRightsHolderAccess(IEnumerable<IDomainQuery<ItInterface>> refinements, Guid? rightsHolderUuid = null);
        Result<ItInterface, OperationError> GetInterfaceAsRightsHolder(Guid interfaceUuid);
        Result<ItInterface, OperationError> CreateNewItInterface(Guid rightsHolderUuid, RightsHolderItInterfaceCreationParameters creationParameters);
        Result<ItInterface, OperationError> UpdateItInterface(Guid interfaceUuid, RightsHolderItInterfaceUpdateParameters updateParameters);
        Result<ItInterface, OperationError> Deactivate(Guid interfaceUuid, string reason);
    }
}
