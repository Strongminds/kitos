﻿namespace Core.DomainModel.Result
{
    public enum OperationFailure
    {
        BadInput,
        NotFound,
        Forbidden,
        Conflict,
        BadState,
        UnknownError
    }
}
