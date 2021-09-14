﻿using System;
using Core.Abstractions.Types;
using Core.DomainModel;


namespace Core.DomainServices.Generic
{
    /// <summary>
    /// Service to resolve an entity uuid based on db or the other way around.
    /// </summary>
    public interface IEntityIdentityResolver
    {
        Maybe<Guid> ResolveUuid<T>(int dbId) where T : class, IHasUuid, IHasId;
        Maybe<int> ResolveDbId<T>(Guid uuid) where T : class, IHasUuid, IHasId;
        Maybe<T> ResolveDbEntity<T>(Guid uuid) where T : class, IHasUuid, IHasId;
    }
}
