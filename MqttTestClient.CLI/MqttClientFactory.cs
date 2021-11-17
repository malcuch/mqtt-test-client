using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Oocx.ReadX509CertificateFromPem;

namespace MqttTestClient.CLI
{
    internal static class MqttClientFactory
    {
        internal static async Task<IMqttClient> CreateConnectedClient(Program.CommonOptions options)
        {
            var factory = new MqttFactory();

            var mqttClient = factory.CreateMqttClient();
            var mqttOptions = BuildMqttOptions(options);

            mqttClient.UseConnectedHandler(c => Console.WriteLine($"### Connected {c.AuthenticateResult.ReasonString}"));
            mqttClient.UseDisconnectedHandler(c => Console.WriteLine($"### Disconnected, reason: {c.Reason}"));

            await mqttClient.ConnectAsync(mqttOptions);

            return mqttClient;
        }

        private static IMqttClientOptions BuildMqttOptions(Program.CommonOptions options)
        {
            X509Certificate2 clientCert =
                LoadCertificate(options.CertificateFileName, options.CertificateKeyFileNameOrPassword);

            return new MqttClientOptionsBuilder()
                .WithTcpServer(options.Broker, options.Port)
                .WithClientId(options.ClientId)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    Certificates = new List<X509Certificate2> {clientCert},
                    SslProtocol = SslProtocols.Tls12
                })
                .Build();
        }

        private static X509Certificate2 LoadCertificate(string certificateFileName, string certificateKeyFileNameOrPassword)
        {
            var extension = Path.GetExtension(certificateFileName).ToLower();

            switch (extension)
            {
                case ".pfx":
                    return new X509Certificate2(certificateFileName, certificateKeyFileNameOrPassword, X509KeyStorageFlags.Exportable);
                case ".pem":
                    var reader = new CertificateFromPemReader();
                    return reader.LoadCertificateWithPrivateKey(certificateFileName, certificateKeyFileNameOrPassword);
                default:
                    throw new NotSupportedException($"Unsupported certificate file extension {extension}");
            }
        }
    }
}