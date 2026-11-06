namespace Infrastructure.STS.Beskedfordeler;

public enum BeskedfordelerMappingFailure
{
    UnsupportedMessageType,
    UnsupportedRegistrationCount,
    UnsupportedObjectType,
    UnsupportedAction,
    InvalidUserIdentifier,
    InvalidMunicipalityCvr,
    RecipientMismatch,
    InvalidTimestamp,
    MunicipalityNotConnected,
    AmbiguousMunicipality,
    InvalidOrganizationIdentifier
}
