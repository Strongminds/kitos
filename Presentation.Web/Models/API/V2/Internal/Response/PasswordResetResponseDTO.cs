using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Presentation.Web.Models.API.V2.Internal.Response
{
    public class PasswordResetResponseDTO
    {
        [Required]
        public string Email { get; set; }
    }
}