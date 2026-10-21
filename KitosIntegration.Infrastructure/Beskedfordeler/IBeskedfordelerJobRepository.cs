namespace KitosIntegration.Infrastructure.Beskedfordeler;

public interface IBeskedfordelerJobRepository
{
    string? Enqueue(byte[] originalMessage);
}

