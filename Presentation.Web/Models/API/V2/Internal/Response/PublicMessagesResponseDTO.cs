using System;
using System.ComponentModel.DataAnnotations;
using Core.DomainModel.PublicMessage;
using Presentation.Web.Controllers.API.V2.Internal.Messages.Mapping;

namespace Presentation.Web.Models.API.V2.Internal.Response
{
    public class PublicMessagesResponseDTO
    {
        public PublicMessagesResponseDTO() {}

        public PublicMessagesResponseDTO(PublicMessage publicMessageModel)
        {
            Uuid = publicMessageModel.Uuid;
            LongDescription = publicMessageModel.LongDescription;
            ShortDescription = publicMessageModel.ShortDescription;
            Status = publicMessageModel.Status?.ToPublicMessageStatusChoice();
            Link = publicMessageModel.Link;
        }

        public Guid Uuid { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string LongDescription { get; set; }
        public string ShortDescription { get; set; }
        public PublicMessageStatusChoice? Status { get; set; }
        public string Link { get; set; }
    }
}