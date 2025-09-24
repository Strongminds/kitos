using System;
using Core.Abstractions.Types;

namespace Core.DomainModel
{
    public abstract class LocalOptionEntity<TOptionType> : Entity, IOwnedByOrganization
    {
        public string Description { get; set; }

        public virtual Organization.Organization Organization { get; set; }

        public int OrganizationId { get; set; }

        [Obsolete("This will never be used")]  
        public virtual OptionEntity<TOptionType> Option { get; set; }

        public int OptionId { get; set; }

        public bool IsActive { get; set; }

        public bool IsExternallyUsed { get; set; }
        public string ExternallyUsedDescription { get; set; }

        public void SetupNewLocalOption(int organizationId, int optionId)
        {
            OrganizationId = organizationId;
            OptionId = optionId;
            Activate();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void UpdateDescription(Maybe<string> description)
        {
            if (description.HasValue) Description = description.Value;
        }

        public void UpdateIsExternallyUsed(Maybe<bool> isExternallyUsed)
        {
            if(isExternallyUsed.HasValue)
                IsExternallyUsed = isExternallyUsed.Value;
            if (!IsExternallyUsed)
                ExternallyUsedDescription = null;
        }

        public Maybe<OperationError> UpdateExternallyUsedDescription(Maybe<string> externallyUsedDescription)
        {
            if(externallyUsedDescription.IsNone)
                return Maybe<OperationError>.None;
            if (!IsExternallyUsed)
                return new OperationError("Cannot add externally used description if option is not used externally", OperationFailure.BadState);
            ExternallyUsedDescription = externallyUsedDescription.Value;
            return Maybe<OperationError>.None;
        }
    }
}
