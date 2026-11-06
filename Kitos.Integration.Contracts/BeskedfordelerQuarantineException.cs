using System;

namespace Kitos.Integration.Contracts;

/// <summary>A terminal processing outcome retained in Hangfire Failed jobs for operator replay.</summary>
public sealed class BeskedfordelerQuarantineException(string reason)
    : Exception($"Beskedfordeler quarantined: {reason}.");
