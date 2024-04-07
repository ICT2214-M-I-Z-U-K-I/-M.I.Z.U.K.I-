#pragma warning disable CA2252, CA1416, CS8618, CS8600, CS8625
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using server;

namespace server
{
    internal class QuicHandler
    {
        private QuicListener Listener;
        public static readonly Guid OwnUuid = Guid.NewGuid(); //read own guid somehow
        public static ConcurrentDictionary<Guid, ConnectionHandler> ClientConnections = new ConcurrentDictionary<Guid, ConnectionHandler>();


        public static ConnectionHandler GetConnectionHandler(Guid clientId)
        {
            if (ClientConnections.TryGetValue(clientId, out ConnectionHandler connectionHandler))
            {
                return connectionHandler;
            }
            else
            {
                throw new KeyNotFoundException("Client ID not found in the connections.");
            }
        }

        public async Task<QuicConnection> createConnection(IPAddress ip, int port)
        {
            return await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
            {
                DefaultCloseErrorCode = 0,
                DefaultStreamErrorCode = 0,
                IdleTimeout = TimeSpan.FromSeconds(60),
                KeepAliveInterval = TimeSpan.FromSeconds(30),
                RemoteEndPoint = new IPEndPoint(ip, port),
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { new("mizuki") },
                    // Modify this to provide a cert for validation purposes
                    //ClientCertificates = cert,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        return true; //replace with code to validate using certs
                    }
                }
            });
        }

        public async void createListener(IPAddress ip, int port)
        {
            var newListener = await QuicListener.ListenAsync(new QuicListenerOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { new("mizuki") }, //SslApplicationProtocol.Http3
                ListenEndPoint = new IPEndPoint(ip, port),
                ConnectionOptionsCallback = (connection, ssl, token) => ValueTask.FromResult(new QuicServerConnectionOptions()
                {
                    DefaultStreamErrorCode = 0,
                    DefaultCloseErrorCode = 0,
                    IdleTimeout = TimeSpan.FromSeconds(60),
                    KeepAliveInterval = TimeSpan.FromSeconds(30),
                    ServerAuthenticationOptions = new SslServerAuthenticationOptions()
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol>() { new("mizuki") },
                        //Modify this to properly provide a cert
                        ServerCertificate = GetServerCertificate(),
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                        {
                            var serverCert = GetServerCertificate();
                            if (serverCert.Issuer == certificate.Issuer)
                                {
                                    return true;
                                }
                            return false;
                        }
                    }
                })
            });
            this.Listener = newListener;
        }

        public async Task StartAsyncListener()
        {
            Console.WriteLine("Quic Server Running...");
            Console.WriteLine(this.Listener.LocalEndPoint);

            while (true)
            {
                try
                {
                    var connection = await this.Listener.AcceptConnectionAsync();
                    var handler = new ConnectionHandler(connection);
                    // Run each connection handler in its own task
                    _ = Task.Run(() => handler.HandleConnectionAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception accepting connection: {ex.Message}");
                }
            }
        }

        public static X509Certificate2 GetServerCertificate()
        {
            var RSAPEMB64 = @"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";
            var RSACertB64 = @"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";

            var RSAKey = RSA.Create();
            RSAKey.ImportPkcs8PrivateKey(Convert.FromBase64String(RSAPEMB64), out _);
            X509Certificate2 rsaCert = new X509Certificate2(Convert.FromBase64String(RSACertB64));
            X509Certificate2 rsaCertWithKey = rsaCert.CopyWithPrivateKey(RSAKey);

            return rsaCertWithKey;
        }

        public X509Certificate2 GenerateManualCertificate()
        {
            X509Certificate2 cert = null;
            var store = new X509Store("KestrelWebTransportCertificates", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            if (store.Certificates.Count > 0)
            {
                cert = store.Certificates[^1];

                // rotate key after it expires
                if (DateTime.Parse(cert.GetExpirationDateString(), null) < DateTimeOffset.UtcNow)
                {
                    cert = null;
                }
            }
            if (cert == null)
            {
                // generate a new cert
                var now = DateTimeOffset.UtcNow;
                SubjectAlternativeNameBuilder sanBuilder = new();
                sanBuilder.AddDnsName("localhost");
                using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                CertificateRequest req = new("CN=localhost", ec, HashAlgorithmName.SHA256);
                // Adds purpose
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1") // serverAuth

        }, false));
                // Adds usage
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                // Adds subject alternate names
                req.CertificateExtensions.Add(sanBuilder.Build());
                // Sign
                using var crt = req.CreateSelfSigned(now, now.AddDays(14)); // 14 days is the max duration of a certificate for this
                cert = new(crt.Export(X509ContentType.Pfx));

                // Save
                store.Add(cert);
            }
            store.Close();

            var hash = SHA256.HashData(cert.RawData);
            var certStr = Convert.ToBase64String(hash);
            //Console.WriteLine($"\n\n\n\n\nCertificate: {certStr}\n\n\n\n"); // <-- you will need to put this output into the JS API call to allow the connection
            return cert;
        }
    }
}
