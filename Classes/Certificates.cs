using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms.ComponentModel.Com2Interop;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.Crypto.Generator;



//This NameSpace handles all certificate related functions, local Certificate Store, Create, Generate, Store, Modify.
namespace Mizuki.Classes
{
    internal class Certificates
    {
        private X509Certificate2 _certificate;
        private Guid _uuid;

        public Certificates()
        {
            int CheckResult = CheckMizukiCertificate();
            if (CheckResult == 1)
            {
                EquipCert();
            }
            else if (CheckResult == 2)
            {
                SelectCert();
            }
        }

        public X509Certificate2 Certificate
        {
            get { return _certificate; }
        }

        public Guid UUID { get { return _uuid; } }
        //public X509Certificate2 Cert { get { return _certificate; } set { _certificate = value; } }

        public class CSRJSONResponse
        {
            public string certificate { get; set; }
            public string serial_number { get; set; }
            public string response_format { get; set; }
            public List<string> certificate_chain { get; set; }
        }
        public void EquipCert()
        {
            using (X509Store store = new X509Store("MY", StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, "clients.mizuki.ltd", false);
                _certificate = certCollection[0];
                _uuid = new Guid(certCollection[0].Subject.Substring(3, 36));
            };
        }

        public void SelectCert()
        {
            using (X509Store store = new X509Store("MY", StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, "clients.mizuki.ltd", false);
                X509Certificate2Collection certs = X509Certificate2UI.SelectFromCollection(certCollection, "Select", "Select a certificate to use with Mizuki", X509SelectionFlag.SingleSelection);
                if (certs.Count == 1)
                {
                    _certificate = certs[0];
                }
                _uuid = new Guid(certs[0].Subject.Substring(3, 36));
            };
        }

        public static int CheckMizukiCertificate()
        {
            using (X509Store store = new X509Store("MY", StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, "clients.mizuki.ltd", false);
                if (certCollection != null)
                {
                    if (certCollection.Count == 1)
                    {
                        return 1;
                    }
                    else if (certCollection.Count > 1)
                    {
                        return 2;
                    }
                }
            };
            return 0;
        }

        public static void CreateSelfSignedCertificate(Guid UUID)
        {
            RSA RSAKeyPair = RSA.Create(4096);
            CertificateRequest csrequest = GenerateCSR(UUID, RSAKeyPair);
            X509Certificate2 selfSignedCert = csrequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(2));
            selfSignedCert.FriendlyName = $"{UUID.ToString()}.clients.mizuki.ltd";
            var keyStoreFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet;

            var exportOfCopiedPrivKeyCert = selfSignedCert.Export(X509ContentType.Pkcs12);
            var withFlagsReimportedOfCopiedWithPrivKeyCert = new X509Certificate2(exportOfCopiedPrivKeyCert, (SecureString)null, keyStoreFlags);
            withFlagsReimportedOfCopiedWithPrivKeyCert.FriendlyName = $"{UUID}.clients.mizuki.ltd";

            InstallCert(withFlagsReimportedOfCopiedWithPrivKeyCert, 3);
        }

        private static X509Certificate2 LoadRegistrationCert()
        {
            var ECCPemB64 = @"MHcCAQEEIBFS3jHuqSsJHeDfSXZsKenS4B7k7m2Hp5g/G/rD0u/qoAoGCCqGSM49AwEHoUQDQgAE/xcuhPIYmREM3+urygqXrAxBg7qVunhqyIBbCB+cm/TvpehdY59lEcDYS+jpUqxOyyQ0zFM4SkYYCpbKf5+dNQ==";
            var ECCCertB64 = @"MIIB3zCCAYWgAwIBAgIUM068+COIRQ2VvqMrkYSaLZsrSNowCgYIKoZIzj0EAwQwRjELMAkGA1UEBhMCU0cxEzARBgNVBAoMCk1penVraSBMdGQxIjAgBgNVBAMMGU1penVraSBDbGllbnRzIFJvb3QgQ0EtRzEwHhcNMjQwMzA4MTEwMzUyWhcNMjYwMzA4MTEwMzUxWjAYMRYwFAYDVQQDDA1SZWdpc3RyYXRvcjAxMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE/xcuhPIYmREM3+urygqXrAxBg7qVunhqyIBbCB+cm/TvpehdY59lEcDYS+jpUqxOyyQ0zFM4SkYYCpbKf5+dNaN/MH0wDAYDVR0TAQH/BAIwADAfBgNVHSMEGDAWgBS+1MYwQge1OiFboFgOKb7mmB3w8DAdBgNVHSUEFjAUBggrBgEFBQcDAgYIKwYBBQUHAwQwHQYDVR0OBBYEFIiobGY1kmyC5uzPWx4yTlct+mzLMA4GA1UdDwEB/wQEAwIF4DAKBggqhkjOPQQDBANIADBFAiEA/YX6BmwDVXkR9isUgdN4yiJRHuexc6EMGNBFS9fOwg8CIB8XEt7t3W1xjczvjTL4dPwyqIKAZoLf0oMCSCtRTAOx";

            var ECCKey = ECDsa.Create();
            ECCKey.ImportECPrivateKey(Convert.FromBase64String(ECCPemB64), out _);
            var ECCCert = new X509Certificate2(Convert.FromBase64String(ECCCertB64));
            var ECCCertWithKey = ECCCert.CopyWithPrivateKey(ECCKey);

            return ECCCertWithKey;
        }

        public static void CreateSignedCertificate(Guid UUID)
        {
            RSA RSAKeyPair = RSA.Create(4096);
            CertificateRequest csrequest = GenerateCSR(UUID, RSAKeyPair);
            byte[] derEncodedCsr = csrequest.CreateSigningRequest();
            var csrSb = new StringBuilder();
            csrSb.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");
            csrSb.AppendLine(Convert.ToBase64String(derEncodedCsr));
            csrSb.AppendLine("-----END CERTIFICATE REQUEST-----");

            string csr = csrSb.ToString();
            var task = Task.Run(async () => await RequestCertificate(UUID, csr));
            CSRJSONResponse jsonResponse = task.Result;

            var RSACAB64 = jsonResponse.certificate_chain[1];
            var RSACAPEM = new StringBuilder();
            RSACAPEM.AppendLine("-----BEGIN CERTIFICATE-----");
            RSACAPEM.AppendLine(RSACAB64);
            RSACAPEM.AppendLine("-----END CERTIFICATE-----");
            X509Certificate2 ECCCACert = X509Certificate2.CreateFromPem(RSACAPEM.ToString());

            InstallCert(ECCCACert, 1);

            var RSAICAB64 = jsonResponse.certificate_chain[0];
            var RSAICAPEM = new StringBuilder();
            RSAICAPEM.AppendLine("-----BEGIN CERTIFICATE-----");
            RSAICAPEM.AppendLine(RSAICAB64);
            RSAICAPEM.AppendLine("-----END CERTIFICATE-----");
            X509Certificate2 ECCICACert = X509Certificate2.CreateFromPem(RSAICAPEM.ToString());

            InstallCert(ECCICACert, 2);

            var RSACertB64 = jsonResponse.certificate;
            var RSACertPEM = new StringBuilder();
            RSACertPEM.AppendLine("-----BEGIN CERTIFICATE-----");
            RSACertPEM.AppendLine(RSACertB64);
            RSACertPEM.AppendLine("-----END CERTIFICATE-----");

            string RSAPrivPEM = RSAKeyPair.ExportRSAPrivateKeyPem();
            X509Certificate2 RSACert = X509Certificate2.CreateFromPem(RSACertPEM.ToString(), RSAPrivPEM);
            RSACert.FriendlyName = $"{UUID.ToString()}.clients.mizuki.ltd";

            var keyStoreFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet;
            var RawCert = X509Certificate2.CreateFromPem(RSACertPEM.ToString());
            var copiedWithPrivKeyCert = RawCert.CopyWithPrivateKey(RSAKeyPair);
            var exportOfCopiedPrivKeyCert = copiedWithPrivKeyCert.Export(X509ContentType.Pkcs12);
            var withFlagsReimportedOfCopiedWithPrivKeyCert = new X509Certificate2(exportOfCopiedPrivKeyCert, (SecureString)null, keyStoreFlags);
            withFlagsReimportedOfCopiedWithPrivKeyCert.FriendlyName = $"{UUID}.clients.mizuki.ltd";

            InstallCert(withFlagsReimportedOfCopiedWithPrivKeyCert, 3);
        }

        private static CertificateRequest GenerateCSR(Guid UUID, RSA RSAPrivateKey)
        {
            CertificateRequest csrequest = new CertificateRequest
            (
                $"CN={UUID.ToString()}.clients.mizuki.ltd, O=Mizuki Clients, C=SG",
                RSAPrivateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            csrequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            csrequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2"), new Oid("1.3.6.1.5.5.7.3.4") }, false));

            return csrequest;
        }

        private static async Task<CSRJSONResponse> RequestCertificate(Guid UUID, string CSR)
        {
            var HTTPHandler = new HttpClientHandler();
            HTTPHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            HTTPHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls13;
            HTTPHandler.ClientCertificates.Add(new X509Certificate2(LoadRegistrationCert().Export(X509ContentType.Pkcs12)));
            HTTPHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, SslPolicyErrors) => { return true; };

            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                certificate_request = $"{CSR}",
                certificate_profile_name = "MizukiTLSClientsRSA",
                end_entity_profile_name = "MizukiTLSClients-RSA",
                certificate_authority_name = "MizukiClientsSubCA-G2",
                username = $"{UUID.ToString()}",
                password = "123",
                include_chain = true
            }), Encoding.UTF8, "application/json");

            var HTTPRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
            };
            HTTPRequestMessage.Content = jsonContent;

            var HTTPClient = new HttpClient(HTTPHandler);
            HTTPClient.BaseAddress = new Uri("https://cert.mizuki.ltd/ejbca/ejbca-rest-api/v1/certificate/pkcs10enroll/");
            HTTPClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = await HTTPClient.PostAsync("", jsonContent);
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.Write(responseJson);

            CSRJSONResponse jsonResponse = JsonSerializer.Deserialize<CSRJSONResponse>(responseJson);

            return jsonResponse;
        }

        private static void InstallCert(X509Certificate2 certToInstall, int depthOfCert)
        {
            X509Store store = depthOfCert switch
            {
                1 => new X509Store(StoreName.Root, StoreLocation.CurrentUser),
                2 => new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser),
                3 => new X509Store(StoreName.My, StoreLocation.CurrentUser)
            };

            store.Open(OpenFlags.ReadWrite);
            store.Add(certToInstall);
            store.Close();
        }

        public byte[] GenerateSubKey(Guid fileInstanceUUID)
        {
            byte[] key = _certificate.GetRSAPrivateKey().ExportRSAPrivateKey();
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(key, fileInstanceUUID.ToByteArray(), 500, HashAlgorithmName.SHA256, 32);
            return derivedKey;
        }

        public class Criterion
        {
            public string property { get; set; }
            public string value { get; set; }
            public string operation { get; set; }
        }

        public class CertInnerJSONResponse
        {
            public string certificate { get; set; }
            public string serial_number { get; set; }
            public string response_format { get; set; }
            public string certificate_profile { get; set; }
            public string end_entity_profile { get; set; }
        }

        public class CertJSONResponse
        {
            public List<CertInnerJSONResponse> certificates { get; set; }
            public bool more_results { get; set; }
        }

        public static async Task<X509Certificate2> GetCertFromGuid(Guid peerGUID)
        {
            var HTTPHandler = new HttpClientHandler();
            HTTPHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            HTTPHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls13;
            HTTPHandler.ClientCertificates.Add(new X509Certificate2(LoadRegistrationCert().Export(X509ContentType.Pkcs12)));

            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                max_number_of_results = 1,
                criteria = new List<Criterion>
                {
                    new Criterion
                    {
                        property = "QUERY",
                        value = $"{peerGUID.ToString()}",
                        operation = "EQUAL"
                    },
                    new Criterion
                    {
                        property = "STATUS",
                        value = "CERT_ACTIVE",
                        operation = "EQUAL"
                    }
                }
            }), Encoding.UTF8, "application/json");

            var HTTPRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
            };
            HTTPRequestMessage.Content = jsonContent;

            var HTTPClient = new HttpClient(HTTPHandler);
            HTTPClient.BaseAddress = new Uri("https://cert.mizuki.ltd/ejbca/ejbca-rest-api/v1/certificate/search/");
            HTTPClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = await HTTPClient.PostAsync("", jsonContent);
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.Write(responseJson);

            CertJSONResponse jsonResponse = JsonSerializer.Deserialize<CertJSONResponse>(responseJson);

            if (jsonResponse.more_results == true)
            {
                string der = jsonResponse.certificates[0].certificate;
                var certSB = new StringBuilder();
                certSB.AppendLine("-----BEGIN CERTIFICATE-----");
                certSB.AppendLine(Encoding.UTF8.GetString(Convert.FromBase64String(der)));
                certSB.AppendLine("-----END CERTIFICATE-----");
                X509Certificate2 cert = X509Certificate2.CreateFromPem(certSB.ToString());
                return cert;
            }
            return null;
        }

        public static byte[] EncryptWithCert(X509Certificate2 cert, byte[] plainBytes)
        {
            try
            {
                byte[] encryptedBytes = new byte[0];
                using (RSA rsaEncryptionObject = cert.GetRSAPublicKey())
                {
                    // Calculate the maximum block size for the RSA encryption
                    int keySizeBytes = rsaEncryptionObject.KeySize / 8;
                    int blockSize = keySizeBytes - 2 * (256 / 8) - 2; // Subtract OAEP overhead
                    int blocksCount = (plainBytes.Length + blockSize - 1) / blockSize; // Calculate the number of blocks needed

                    encryptedBytes = new byte[blocksCount * keySizeBytes];

                    for (int j = 0; j < blocksCount; j++)
                    {
                        int i = j;
                        int offset = i * blockSize;
                        int blockLength = Math.Min(blockSize, plainBytes.Length - offset);

                        byte[] buffer = new byte[blockLength];
                        Buffer.BlockCopy(plainBytes, offset, buffer, 0, blockLength);

                        byte[] encryptedBlock = rsaEncryptionObject.Encrypt(buffer, RSAEncryptionPadding.OaepSHA256);
                        Buffer.BlockCopy(encryptedBlock, 0, encryptedBytes, i * keySizeBytes, encryptedBlock.Length);
                    }
                }
                return encryptedBytes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public byte[] DecryptWithMyCert(byte[] encryptedBytes)
        {
            try
            {
                byte[] decryptedBytes = new byte[0];
                using (RSA rsaDecryptionObject = _certificate.GetRSAPrivateKey())
                {
                    int keySizeBytes = rsaDecryptionObject.KeySize / 8;
                    int blockSize = keySizeBytes - 2 * (256 / 8) - 2;
                    int blocksCount = encryptedBytes.Length / keySizeBytes;

                    decryptedBytes = new byte[blocksCount * blockSize];

                    for (int j = 0; j < blocksCount; j++)
                    {
                        int i = j;
                        byte[] encryptedBlock = new byte[keySizeBytes];
                        Buffer.BlockCopy(encryptedBytes, i * keySizeBytes, encryptedBlock, 0, keySizeBytes);

                        byte[] decryptedBlock = rsaDecryptionObject.Decrypt(encryptedBlock, RSAEncryptionPadding.OaepSHA256);
                        Buffer.BlockCopy(decryptedBlock, 0, decryptedBytes, i * blockSize, decryptedBlock.Length);
                    }
                }
                return decryptedBytes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string GetSha256OfPrivateKey()
        {
            using (SHA256 sha256Obj = SHA256.Create())
            {
                RSA rsaObj = (RSA)_certificate.PrivateKey;
                byte[] hashedBA = rsaObj.SignHash(sha256Obj.ComputeHash(Encoding.UTF8.GetBytes("MIZUKI")), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                //byte[] hashedBA = sha256Obj.ComputeHash(privKeyBA);
                string hash = string.Empty;
                foreach (byte b in hashedBA)
                {
                    hash += b.ToString("x2");
                }
                return hash;
            }
        }
    }
}
