using System.Collections.Generic;
using System.Linq;

namespace Core.DomainModel.ItContract
{
    public class ItContractValidationResult
    {
        public IEnumerable<ItContractValidationError> ValidationErrors { get; }
        public bool Result { get; }
        public bool EnforcedValid { get; }
        public bool RequireValidParent { get; set; }

        public ItContractValidationResult(bool enforcedValid, bool requireValidParent, bool parentIsValid, IEnumerable<ItContractValidationError> validationErrors)

        {
            var errors = validationErrors.ToList();
            var contractResult = requireValidParent ? parentIsValid : errors.Any();
            ValidationErrors = errors;
            Result = enforcedValid || contractResult;
            EnforcedValid = enforcedValid;
            RequireValidParent = requireValidParent;
        }

    }
}
