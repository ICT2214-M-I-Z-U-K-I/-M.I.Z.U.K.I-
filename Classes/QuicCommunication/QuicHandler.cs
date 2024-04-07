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
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace Mizuki.Classes.QuicCommunication
{
    [RequiresPreviewFeatures]
    internal class QuicHandler
    {
        private QuicListener _listener;
        private Guid _ownUuid;
        private bool _relayed;
        private bool _caed;

        //public static readonly Guid OwnUuid = Guid.NewGuid(); //read own guid somehow

        public QuicHandler(Guid myUUID, bool relayed, bool caed)
        {
            _ownUuid = myUUID;
            _relayed = relayed;
            _caed = caed;
        }

        public async Task<QuicConnection> createConnection(IPAddress ip, int port, X509Certificate2 clientCertificate)
        {
            X509Certificate2Collection CertCollection = new X509Certificate2Collection();
            CertCollection.Add(clientCertificate);
            QuicConnection quicCon =  await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
            {
                DefaultCloseErrorCode = 0,
                DefaultStreamErrorCode = 0,
                IdleTimeout = TimeSpan.FromSeconds(60),
                KeepAliveInterval = TimeSpan.FromSeconds(30),
                RemoteEndPoint = new IPEndPoint(ip, port),
                MaxInboundUnidirectionalStreams = 100,
                MaxInboundBidirectionalStreams = 100,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { new("mizuki") },
                    // Modify this to provide a cert for validation purposes
                    ClientCertificates = CertCollection,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        if (_caed)
                        {
                            //Check if the peer certificate has the same parent, with my certificate
                            if (clientCertificate.Issuer == certificate.Issuer)
                            {
                                return true;
                            }
                            return false;
                        }
                        return true;
                    }
                }
            });
            return quicCon;
        }


        //Exist in the Context of Direct Mode Only.
        public async Task createListener(X509Certificate2 clientCertificate)
        {
            _listener = await QuicListener.ListenAsync(new QuicListenerOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { new("mizuki") }, //SslApplicationProtocol.Http3
                ListenEndPoint = new IPEndPoint(IPAddress.Any, 31038),
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
                        ServerCertificate = clientCertificate,
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                        {
                            if (_caed)
                            {
                                //Check if the peer certificate has the same parent, with my certificate
                                if (clientCertificate.Issuer == certificate.Issuer)
                                {
                                    return true;
                                }
                                return false;
                            }
                            return true;
                        }
                    }
                })
            });
        }

        public async Task StartAsyncListener(X509Certificate2 clientCertificate)
        {
            createListener(clientCertificate);
            Console.WriteLine("Quic Server Running...");
            Console.WriteLine(_listener.LocalEndPoint);

            while (true)
            {
                try
                {
                    var connection = await _listener.AcceptConnectionAsync();
                    var handler = new QuicConnectioner(connection, _ownUuid);
                    // Run each connection handler in its own task
                    _ = Task.Run(() => handler.HandleConnectionAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception accepting connection: {ex.Message}");
                }
            }
        }

        /*
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
        */
    }
}


