using Core.DomainModel.Advice;

namespace Core.ApplicationServices.Model.Notification
{
    public class RecipientModel //TODO: Split into Emial and rolerecipient
    {
        public string Email { get; set; }

        public int? ItContractRoleId { get; set; }
        public int? ItSystemRoleId { get; set; }
        public int? DataProcessingRegistrationRoleId { get; set; }
        //TODO: Just add a role type (enum) and role id

        public RecieverType ReceiverType { get; set; }
        public RecipientType RecipientType { get; set; }
        
    }
}
