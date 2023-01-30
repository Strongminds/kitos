﻿using Core.ApplicationServices.OptionTypes;
using Core.DomainModel;
using Core.DomainServices.Model.Options;
using Presentation.Web.Models.API.V2.Response.Options;

namespace Presentation.Web.Controllers.API.V2.External
{
    public abstract class BaseRoleOptionTypeV2Controller<TParent, TOption> : BaseOptionTypeV2Controller<TParent, TOption, RoleOptionResponseDTO, RoleOptionExtendedResponseDTO>
        where TOption : OptionEntity<TParent>, IRoleEntity
    {
        protected override RoleOptionResponseDTO ToDTO(OptionDescriptor<TOption> option)
        {
            return new(option.Option.Uuid, option.Option.Name, option.Option.HasWriteAccess,option.Description);
        }

        protected override RoleOptionExtendedResponseDTO ToExtendedDTO(TOption option, bool isAvailable)
        {
            return new(option.Uuid, option.Name, option.HasWriteAccess, isAvailable);
        }

        protected BaseRoleOptionTypeV2Controller(IOptionsApplicationService<TParent, TOption> optionApplicationService)
            : base(optionApplicationService)
        {

        }
    }
}