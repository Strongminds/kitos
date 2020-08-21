﻿using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Model.Shared;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Presentation.Web.Models;

namespace Presentation.Web.Extensions
{
    public static class DTOMappingExtensions
    {
        public static IEnumerable<NamedEntityDTO> MapToNamedEntityDTOs<T>(this IEnumerable<T> source)
            where T : Entity, IHasName
        {
            return source.Select(MapToNamedEntityDTO);
        }

        public static NamedEntityDTO MapToNamedEntityDTO<T>(this T source)
            where T : Entity, IHasName
        {
            return new NamedEntityDTO(source.Id, source.Name);
        }

        public static NamedEntityDTO MapToNamedEntityDTO(this NamedEntity source)
        {
            return new NamedEntityDTO(source.Id, source.Name);
        }

        public static NamedEntityWithEnabledStatusDTO MapToNamedEntityWithEnabledStatusDTO(this ItSystem source)
        {
            return new NamedEntityWithEnabledStatusDTO(source.Id, source.Name, source.Disabled);
        }

        public static NamedEntityWithEnabledStatusDTO MapToNamedEntityWithEnabledStatusDTO(this ItSystemUsage source)
        {
            return new NamedEntityWithEnabledStatusDTO(source.Id, source.ItSystem.Name, source.ItSystem.Disabled);
        }
    }
}