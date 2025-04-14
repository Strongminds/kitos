using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PubSub.Core.Abstractions.ErrorTypes;

public static class OperationErrorMappings
{
    public IActionResult MapErrorResponse(OperationError error)
    {
        return error switch
        {
            OperationError.Duplicate => throw new NotImplementedException(),
            OperationError.NotFound => NotFound(),
            OperationError.Forbidden => ,
        };
    }
}