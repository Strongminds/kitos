﻿using Core.Abstractions.Types;
using Core.DomainModel;

namespace Core.DomainServices.Options
{
    public interface ISingleOptionTypeInstanceAssignmentService<in TOwner, TOption> 
        where TOption : OptionEntity<TOwner>
        where TOwner: IOwnedByOrganization
    {
        Result<TOption, OperationError> Assign(TOwner owner, int optionId);
        Result<TOption, OperationError> Clear(TOwner owner);
    }
}
