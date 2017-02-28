using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ConsoleApplication
{
    public class Program
    {
        private static IContainer Container { get; set; }
        private static IServiceProvider ServiceCollection { get; set; }
        private static string connectionString;
        private static string certPath;
        
        public static void Main(string[] args)
        {
            connectionString = args[0];
            certPath = args.Length > 1 ? args[1] : null;
            var builder = new ContainerBuilder();
            Container = builder.Build();            
            ServiceCollection = new AutofacServiceProvider(Container);
            Run();

        }
        
        private static void Run()
        {
            var factory = new ConnectionFactory();
            factory.uri = new Uri(connectionString);
            factory.Ssl.ServerName = System.Net.Dns.GetHostName();
            factory.Ssl.Enabled = true;
            factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            //factory.Ssl.CertificateValidationCallback = RemoteCertificateValidationCallback;
            factory.Ssl.Version = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            factory.Ssl.Certs = new X509Certificate2Collection(new X509Certificate2(certPath));

            try
            {
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        string message = "Hello World!";
                        var body = Encoding.UTF8.GetBytes(message);

                        var properties = channel.CreateBasicProperties();
                        properties.Headers = new Dictionary<string, object>();
                        properties.Headers.Add("tenant", "test");
                        properties.Headers.Add("supplier", "BB");

                        channel.BasicPublish(exchange: "amq.fanout",
                                            routingKey: "exports.stations",
                                            basicProperties: properties,
                                            body: body);
                        Console.WriteLine(" [x] Sent {0}", message);

                    }
                }
            }            
            catch (BrokerUnreachableException bex)
            {
                Exception ex = bex;
                while (ex != null)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("inner:");
                    ex = ex.InnerException;
                }
            }
        }

        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {            
            Console.WriteLine("validation error {0} {1} {2}", sslPolicyErrors.ToString(), certificate.Issuer, certificate.Subject);
            return true;
        }
    }
}
