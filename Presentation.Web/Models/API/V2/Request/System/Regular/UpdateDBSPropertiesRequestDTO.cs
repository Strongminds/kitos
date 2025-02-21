using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.API.V2.Request.System.Regular;

public class UpdateDBSPropertiesRequestDTO
{
    [MaxLength(100)]
    public string SystemName { get; set; }

    [MaxLength(100)]
    public string DataProcessorName { get; set; }
}