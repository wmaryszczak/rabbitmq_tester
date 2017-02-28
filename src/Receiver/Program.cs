using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Text;
using RabbitMQ.Client.MessagePatterns;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
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
                        var queueName = channel.QueueDeclare().QueueName;

                        channel.QueueBind(queue: queueName,
                                    exchange: "amq.fanout",
                                    routingKey: "exports.*");   

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body;
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine(" [x] Received {0} from {1}/{2}", message, ea.Exchange, ea.RoutingKey);
                            Console.WriteLine(" [x] Tenant {0} Supplier {1}", Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["tenant"]), Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["supplier"]));
                        };

                        channel.BasicConsume(queue: queueName,
                                            noAck: true,
                                            consumer: consumer);

                        Console.WriteLine(" Press [enter] to exit.");
                        Console.ReadLine();
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
    }
}
