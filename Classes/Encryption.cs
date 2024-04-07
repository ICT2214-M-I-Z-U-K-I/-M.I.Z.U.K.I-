using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Math;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.NTRU;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode;

namespace Mizuki.Classes
{
    public class Encryption
    {
        private byte[]? _masterKey;
        private byte[]? _encryptedShare;
        private string _instanceID;
        private string _uuid;
        private NTRUKeyPair? _NTRUKeyPair;
        private byte[]? _NTRUPublicKey;
        private Dictionary<string, byte[]>? _instanceMembers;
        private byte[]? _encryptedData;

        public string InstanceID
        {
            get => _instanceID;
            set => _instanceID = value;
        }
        public string UUID
        {
            get => _uuid;
            set => _uuid = value;
        }
        public byte[] EncryptedData
        {
            get => _encryptedData;
        }
        //this is only for testing, will remove b4 final//
        public byte[] PublicKey
        {
            get => _NTRUPublicKey;
        }

        public Encryption(string instanceID, string uuid, byte[]? encryptedShare = null)
        {
            _encryptedShare = encryptedShare;
            InstanceID = instanceID;
            UUID = uuid;
        }

        public void InitializeMasterkey(byte[]? masterKey = null)
        {
            if (masterKey == null)
            {
                using (Aes keygen = Aes.Create())
                {
                    keygen.KeySize = 256;
                    keygen.GenerateKey();
                    _masterKey = keygen.Key;
                }
            }
            else
            {
                _masterKey = masterKey;
            }
        }

        internal byte[] GenerateSubKey()
        {
            if (_masterKey == null)
            {
                InitializeMasterkey();
            }
            byte[] encoded_instanceid = Encoding.UTF8.GetBytes(InstanceID);
            byte[] encoded_uuid = Encoding.UTF8.GetBytes(UUID);
            byte[] derived_key = SP800108HmacCounterKdf.DeriveBytes(_masterKey, HashAlgorithmName.SHA256, encoded_instanceid, encoded_uuid, 32);
            return derived_key;
        }

        public void LoadMembersKey(string memberUUID, byte[] memberKey)
        {
            if (_instanceMembers == null)
            {
                _instanceMembers = new Dictionary<string, byte[]>();
            }
            _instanceMembers.Add(memberUUID, memberKey);
        }

        public Dictionary<string, byte[]>? Encrypt(byte[] data, int threshold)
        {
            byte[] subKey = GenerateSubKey();
            byte[] encryptedData = AesEncrypt(data, subKey);
            _encryptedData = encryptedData;
            var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
            var sss = new ShamirsSecretSharing<BigInteger>(gcd);

            if (_instanceMembers != null)
            {
                var shares = sss.MakeShares(threshold, _instanceMembers.Count + 1, subKey, 127);
                var ownerShare = shares.ToList().First();
                var memberShares = shares.ToList().Skip(1);
                var linkedShares = memberShares.Zip(_instanceMembers, (share, member) => new { Share = share, Member = member });

                Dictionary<string, byte[]> encryptedParties = new Dictionary<string, byte[]>();

                byte[] ownerShareEncoded = Encoding.UTF8.GetBytes(ownerShare.ToString());
                var ownerShareEncrypted = NtruEncrypt(ownerShareEncoded, _NTRUPublicKey);
                _encryptedShare = ownerShareEncrypted;
                encryptedParties.Add(this.UUID, ownerShareEncrypted);
                foreach (var linkedShare in linkedShares)
                {
                    string member_id = linkedShare.Member.Key;
                    var member_value = linkedShare.Member.Value;
                    byte[] encoded_share = Encoding.UTF8.GetBytes(linkedShare.Share.ToString());
                    var encrypted_share = NtruEncrypt(encoded_share, member_value);
                    encryptedParties.Add(member_id, encrypted_share);
                }
                return encryptedParties;
            }
            else
            {
                return null;
            }
        }

        public byte[]? Decrypt(byte[] encryptedData, List<string> decryptedShares)
        {
            var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
            if (_encryptedShare != null)
            {
                var personalShare = NtruDecrypt(_encryptedShare);
                string personalShareDecoded = Encoding.UTF8.GetString(personalShare);
                decryptedShares.Add(personalShareDecoded);
                var reconstructor = new ShamirsSecretSharing<BigInteger>(gcd);
                var decryptedAES = reconstructor.Reconstruction(decryptedShares.ToArray());
                byte[] decryptedData = AesDecrypt(encryptedData, decryptedAES);
                return decryptedData;
            }
            else
            {
                return null;
            }
        }

        internal byte[] AesEncrypt(byte[] data, byte[] key)
        {
            byte[] IV = new byte[16];
            byte[] encryptedData;
            string dataB64 = Convert.ToBase64String(data);
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(key, IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cs))
                            streamWriter.Write(dataB64);
                        encryptedData = ms.ToArray();
                    }
                }
            }
            return encryptedData;
        }

        internal byte[] AesDecrypt(byte[] data, byte[] key)
        {
            byte[] IV = new byte[16];
            byte[] decryptedData;
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(key, IV);
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cs))
                            decryptedData = Convert.FromBase64String(streamReader.ReadToEnd());
                    }
                }
            }
            return decryptedData;
        }

        public string GenerateKeyPair(string? ntruCertSupplied = null)
        {
            if (ntruCertSupplied == null)
            {
                NTRUParameters ntruParams = NTRUParamSets.FromName(NTRUParamSets.NTRUParamNames.E1087EP2);
                NTRUKeyGenerator ntruInstance = new NTRUKeyGenerator(ntruParams);
                NTRUKeyPair ntruInstanceKeyPair = (NTRUKeyPair)ntruInstance.GenerateKeyPair();
                string publicKeyB64 = System.Convert.ToBase64String(ntruInstanceKeyPair.PublicKey.ToBytes());
                string privateKeyB64 = System.Convert.ToBase64String(ntruInstanceKeyPair.PrivateKey.ToBytes());

                string ntruCert = privateKeyB64 + publicKeyB64;

                //string ntruCert = $"-----BEGIN NTRU PRIVATE KEY-----{privateKeyB64}-----END NTRU PRIVATE KEY----------BEGIN NTRU PUBLIC KEY-----{publicKeyB64}-----END NTRU PUBLIC KEY-----";
                //Regex.Replace(ntruCert, @"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+", String.Empty);
                //ntruCert = ntruCert.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(((char) 0x2028).ToString(), string.Empty).Replace(((char) 0x2029).ToString(), string.Empty);
                _NTRUPublicKey = ntruInstanceKeyPair.PublicKey.ToBytes();
                _NTRUKeyPair = ntruInstanceKeyPair;
                return ntruCert;
            }
            else
            {
                //Regex.Replace(ntruCertSupplied, @"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+", String.Empty);
                //ntruCertSupplied = ntruCertSupplied.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(((char)0x2028).ToString(), string.Empty).Replace(((char)0x2029).ToString(), string.Empty);
                /*
                string privateKeySection = ntruCertSupplied.Substring(ntruCertSupplied.IndexOf("-----BEGIN NTRU PRIVATE KEY-----"), ntruCertSupplied.IndexOf("-----BEGIN NTRU PUBLIC KEY-----") - ntruCertSupplied.IndexOf("-----BEGIN NTRU PRIVATE KEY-----"));
                string publicKeySection = ntruCertSupplied.Substring(ntruCertSupplied.IndexOf("-----BEGIN NTRU PUBLIC KEY-----"));
                string privateKeyB64 = privateKeySection.Replace("-----BEGIN NTRU PRIVATE KEY-----", "").Replace("-----END NTRU PRIVATE KEY-----", "");
                string publicKeyB64 = publicKeySection.Replace("-----BEGIN NTRU PUBLIC KEY-----", "").Replace("-----END NTRU PUBLIC KEY-----", "");
                */
                string privateKeyB64 = ntruCertSupplied.Substring(0, 296);
                string publicKeyB64 = ntruCertSupplied.Substring(296, 2000);

                byte[] privateKey = System.Convert.FromBase64String(privateKeyB64);
                byte[] publicKey = System.Convert.FromBase64String(publicKeyB64);
                NTRUKeyPair ntruKeyPair = new NTRUKeyPair(new NTRUPublicKey(publicKey), new NTRUPrivateKey(privateKey));
                _NTRUKeyPair = ntruKeyPair;
                _NTRUPublicKey = publicKey;
                return ntruCertSupplied;
            }
        }

        public byte[] NtruEncrypt(byte[] data, byte[] publicKey)
        {
            using (NTRUParameters ps = NTRUParamSets.FromName(NTRUParamSets.NTRUParamNames.E1087EP2))
            {
                using (NTRUPublicKey pubkey = new NTRUPublicKey(publicKey))
                {
                    byte[] ciphertext;
                    using (NTRUEncrypt engine = new NTRUEncrypt(ps))
                    {
                        engine.Initialize(pubkey);
                        ciphertext = engine.Encrypt(data);
                        return ciphertext;
                    }
                }
            }
        }

        public byte[] NtruDecrypt(byte[] encryptedData)
        {
            using (NTRUParameters ps = NTRUParamSets.FromName(NTRUParamSets.NTRUParamNames.E1087EP2))
            {
                byte[] decryptedSecret;
                using (NTRUEncrypt engine = new NTRUEncrypt(ps))
                {
                    engine.Initialize(_NTRUKeyPair);
                    decryptedSecret = engine.Decrypt(encryptedData);
                }
                return decryptedSecret;
            }
        }
    }
}
