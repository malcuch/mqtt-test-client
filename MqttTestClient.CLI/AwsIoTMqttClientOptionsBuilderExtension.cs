using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client.Options;
using Oocx.ReadX509CertificateFromPem;

namespace MqttTestClient.CLI
{
    internal static class AwsIoTMqttClientOptionsBuilderExtension
    {
        public static MqttClientOptionsBuilder WithAwsIotHubAuthentication(this MqttClientOptionsBuilder builder, X509Certificate2 clientCertificate)
        {
            return builder.WithTls(new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                Certificates = new List<X509Certificate2> { clientCertificate },
                SslProtocol = SslProtocols.Tls12
            });
        }

        public static MqttClientOptionsBuilder WithAwsIotHubAuthentication(this MqttClientOptionsBuilder builder, string clientCertificateFileName, string clientPrivateKeyFileName)
        {
            var reader = new CertificateFromPemReader();
            X509Certificate2 clientCert = reader.LoadCertificateWithPrivateKey(clientCertificateFileName, clientPrivateKeyFileName);
            return builder.WithAwsIotHubAuthentication(clientCert);
        }

        /// <summary>
        /// Verifies certificates against a list of manually trusted certs.
        /// If a certificate is not in the Windows cert store, this will check that it's valid against custom cert
        /// </summary>
        internal class RootCertificateTrust
        {

            X509Certificate2Collection certificates;

            internal RootCertificateTrust()
            {
                certificates = new X509Certificate2Collection();
            }

            internal void AddTrustedCertificate(X509Certificate2 x509Certificate2)
            {
                certificates.Add(x509Certificate2);
            }

            internal bool VerifyServerCertificate(MqttClientCertificateValidationCallbackContext arg)
            {
                // no errors occured then it means validation successfull with system CA
                if (arg.SslPolicyErrors == SslPolicyErrors.None) return true;

                var chain = arg.Chain;

                chain.ChainPolicy.ExtraStore.AddRange(certificates);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag; // Check all properties
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // This setup does not have revocation information

                var buildResult = chain.Build(new X509Certificate2(arg.Certificate));

                if (buildResult) return true;

                // status other than UntrustedRoot are not acceptable
                if (chain.ChainStatus.Any(x => x.Status != X509ChainStatusFlags.UntrustedRoot)) return false;

                // UntrustedRoot are acceptable only on registered certificate (ROOT CA)
                foreach (var chainElement in chain.ChainElements)
                {
                    bool isUntrustedRoot = chainElement.ChainElementStatus.Any(s => s.Status == X509ChainStatusFlags.UntrustedRoot);
                    if (isUntrustedRoot)
                    {
                        var found = certificates.Find(X509FindType.FindByThumbprint, chainElement.Certificate.Thumbprint, false);
                        if (found.Count == 0) return false;
                    }
                }

                return true;
            }

        }
    }
}
