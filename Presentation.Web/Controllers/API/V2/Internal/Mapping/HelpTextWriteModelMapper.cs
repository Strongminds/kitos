using Core.ApplicationServices.Model.HelpTexts;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Model.Request;
using Presentation.Web.Models.API.V2.Internal.Request;

namespace Presentation.Web.Controllers.API.V2.Internal.Mapping;

public class HelpTextWriteModelMapper : WriteModelMapperBase, IHelpTextWriteModelMapper
{
    public HelpTextCreateParameters ToCreateParameters(HelpTextCreateRequestDTO dto)
    {
        return new()
        {
            Key = dto.Key,
            Description = dto.Description,
            Title = dto.Title
        };
    }

    public HelpTextWriteModelMapper(ICurrentHttpRequest currentHttpRequest) : base(currentHttpRequest)
    {
    }
}