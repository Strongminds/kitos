﻿namespace Core.ApplicationServices.Model.Result
{
    public enum SystemDeleteResult
    {
        Ok,
        Forbidden,
        InUse,
        HasChildren,
        HasExhibitInterfaces,
        UnknownError
    }
}
