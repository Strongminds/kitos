using System;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.STS.Common.Model.Token
{
    public static class CertificateLoader
    {
        /// <summary>
        /// Loads a certificate by thumbprint, trying LocalMachine first and falling back to CurrentUser.
        /// Throws a descriptive exception if the certificate is not found or its private key container is missing.
        /// </summary>
        public static X509Certificate2 LoadCertificate(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            // Try the requested store first, then fall back to the other location
            var locationsToTry = storeLocation == StoreLocation.LocalMachine
                ? new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser }
                : new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine };

            foreach (var location in locationsToTry)
            {
                using var store = new X509Store(storeName, location);
                store.Open(OpenFlags.ReadOnly);
                var result = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (result.Count > 0)
                {
                    var cert = result[0];
                    if (cert.HasPrivateKey)
                    {
                        try
                        {
                            // Eagerly verify the private key is accessible to surface a clear error
                            _ = cert.GetRSAPrivateKey() ?? cert.GetECDsaPrivateKey() as object;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Certificate with thumbprint '{thumbprint}' was found in {location}\\{storeName} " +
                                $"but its private key is not accessible. Re-import the PFX file with its private key. " +
                                $"Inner error: {ex.Message}", ex);
                        }
                    }
                    return cert;
                }
            }

            throw new ArgumentException(
                $"No certificate with thumbprint '{thumbprint}' was found in {storeName} " +
                $"(tried LocalMachine and CurrentUser). Install the certificate and retry.");
        }
    }
}
