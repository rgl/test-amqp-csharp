using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TestAmqp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("TestAmqp <amqp url (e.g. amqp://user:pass@hostName:port/vhost)> [tls versions (e.g. tls,tls11,tls12)]");
                return;
            }

            var factory = new ConnectionFactory()
            {
                Uri = new Uri(args[0]),
            };

            // see https://www.rabbitmq.com/ssl.html
            if (args.Length > 1)
            {
                factory.Ssl.Version = 0;
                foreach (var value in args[1].Split(','))
                {
                    factory.Ssl.Version |= (SslProtocols)Enum.Parse(typeof(SslProtocols), value, true);
                }
            }
            else
            {
                factory.Ssl.Version = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            }

            factory.Ssl.CertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                for (var i = 0; i < chain.ChainElements.Count; ++i)
                {
                    var chainElement = chain.ChainElements[i];
                    var path = $"{factory.Uri.Host}-{i}.der";
                    Console.WriteLine(
                        "Saving {0} certificate chain link #{1} (subject={2}; issuer={3}) to {4}...",
                        factory.Uri.Host,
                        i,
                        chainElement.Certificate.Subject,
                        chainElement.Certificate.Issuer,
                        path);
                    File.WriteAllBytes(
                        path,
                        chainElement.Certificate.Export(X509ContentType.Cert));
                }
                return true;
            };

            using (var connection = factory.CreateConnection())
            {
                Console.WriteLine("Client Properties:");
                DumpProperties(connection.ClientProperties, "");

                Console.WriteLine("Server Properties:");
                DumpProperties(connection.ServerProperties, "");
            }
        }

        static void DumpProperties(IDictionary<string, object> properties, string prefix)
        {
            foreach (var kp in properties)
            {
                if (kp.Value is IDictionary<string, object>)
                {
                    DumpProperties((IDictionary<string, object>)kp.Value, prefix+kp.Key+".");
                }
                else if (kp.Value is byte[])
                {
                    var value = Encoding.UTF8.GetString((byte[])kp.Value);
                    Console.WriteLine($"property {prefix}{kp.Key} = {value}");
                }
                else
                {
                    Console.WriteLine($"property {prefix}{kp.Key} = {kp.Value}");
                }
            }
        }
    }
}
