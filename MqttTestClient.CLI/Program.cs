using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Oocx.ReadX509CertificateFromPem;

namespace MqttTestClient.CLI
{
    class Program
    {
        public class CommonOptions
        {
            [Option('s', "server", Required = true, HelpText = "MQTT server host")]
            public string Broker { get; set; }

            [Option('p', "port", Required = false, HelpText = "MQTT server port", Default = 8883)]
            public int Port { get; set; }

            [Option('t', "topic", Required = true, HelpText = "MQTT topic to publish/subscribe")]
            public string Topic { get; set; }

            [Option('c', "client", Required = false, HelpText = "(Default: TestClientPublisher or TestClientSubscriber) MQTT client ID", Default = null)]
            public string ClientId { get; set; }

            [Option('x', "certificate", Required = false, HelpText = "Certificate file name (pem or pfx)", Default = "cert.pem")]            
            public string CertificateFileName { get; set; }

            [Option('k', "key", Required = false, HelpText = "Certificate private key file name or password if pfx certificate file is used", Default ="private.key" )]
            public string CertificateKeyFileNameOrPassword { get; set; }
        }

        [Verb("publish", HelpText = "Publish message to specified topic")]
        class PublishOptions : CommonOptions
        {
            [Option('m', "message", Required = true, HelpText = "Message to publiush over MQTT (string)")]
            public string Message { get; set; }

            [Option('l', "loop", Required = false, HelpText = "Publish message in 1 second intervals until ESC key is pressed")]
            public bool Loop { get; set; }

            public PublishOptions()
            {
                ClientId = "TestClientPublisher";
            }
        }

        [Verb("subscribe", HelpText = "Subscribe to specified topic and dump received messages")]
        class SubscribeOptions : CommonOptions
        {
            public SubscribeOptions()
            {
                ClientId = "TestClientSubscribed";
            }
        }

        static async Task Main(string[] args)
        {            
            var result = Parser.Default.ParseArguments<PublishOptions, SubscribeOptions>(args);
            await result.WithParsedAsync<PublishOptions>(PublishCommand);
            await result.WithParsedAsync<SubscribeOptions>(SubscribeCommand);            
        }

        private static async Task PublishCommand(PublishOptions options)
        {
            using var mqttClient = await MqttClientFactory.CreateConnectedClient(options);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(options.Topic)
                .WithPayload(options.Message)
                .WithAtLeastOnceQoS()
                .Build();
           
            do
            {
                Log($"Publish '{options.Message}'");
                await mqttClient.PublishAsync(message);

                var isStopped = Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape;
                if (isStopped) break;

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            while (options.Loop);

        }

        private static  async Task SubscribeCommand(SubscribeOptions options)
        {
            using var mqttClient = await MqttClientFactory.CreateConnectedClient(options);

            mqttClient.UseApplicationMessageReceivedHandler(m => {
                Log($"From '{m.ClientId}' -> '{Encoding.UTF8.GetString(m.ApplicationMessage.Payload)}'");
            });

            await mqttClient.SubscribeAsync(options.Topic);

            Console.ReadKey();
        }
        
        private static void Log(string message) => Console.WriteLine("   > " + message);
    }    
}