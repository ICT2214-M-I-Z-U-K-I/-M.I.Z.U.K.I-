using Microsoft.VisualBasic.ApplicationServices;
using Org.BouncyCastle.Crypto.Paddings;
using SecretSharingDotNet.Cryptography;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mizuki.Classes.QuicCommunication
{
    [RequiresPreviewFeatures]
    internal class MizukiHeader
    {
        public byte Opcode { get; set; } // 1
        public Guid SelfUuid { get; set; } // 16
        public Guid TargetUuid { get; set; } // 16
        public uint DataLength { get; set; } // 4
        public string FileName = null; // 0
    }

    [RequiresPreviewFeatures]
    internal class QuicConnectioner
    {
        private QuicConnection _connection;
        private Guid _ownUuid;
        private Guid _clientUuid;
        private IPEndPoint _endPoint;

        public Guid ClientUUID
        {
            get { return _clientUuid; }
            set { _clientUuid = value; }
        }

        public QuicConnection Connection
        {
            get { return _connection; }
        }

        //Instanciate for Direct-Direct
        public QuicConnectioner(QuicConnection connection, Guid myUuid)
        {
            _connection = connection;
            _ownUuid = myUuid;
        }

        public async Task<QuicStream> CreateStream()
        {
            return await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        }
        public async Task HandleConnectionAsync()
        {
            Console.WriteLine($"Client [{_connection.RemoteEndPoint}]: connected");
            _endPoint = _connection.RemoteEndPoint;
            try
            {
                // Continuously listen for and handle incoming streams
                while (true)
                {
                    await using var stream = await _connection.AcceptInboundStreamAsync();
                    await HandleStreamAsync(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during connection handling: {ex.Message}");
            }
        }

        public async Task HandleStreamAsync(QuicStream stream)
        {
            byte[] secret = [];
            bool encrypted = false;
            

            if (_connection.RemoteEndPoint != _endPoint)
            {
                _endPoint = _connection.RemoteEndPoint;
            }
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                _clientUuid = header.SelfUuid;
                
                if (EnderChest.controller.quicController.ClientKeys.ContainsKey(header.SelfUuid))
                {
                    encrypted = true;
                    secret = EnderChest.controller.quicController.ClientKeys[header.SelfUuid];
                }

                switch (header.Opcode)
                {

                    case 1:
                        await HandleNTRUPublicKeyRequestAsync(stream, header, encrypted, secret);
                        Console.WriteLine($"Public Key request from {_connection.RemoteEndPoint} by {header.SelfUuid}");
                        break;

                    case 3:
                        await HandleFileShareRequestAsync(stream, header);
                        Console.WriteLine($"Encrypted File Sharing request from {_connection.RemoteEndPoint} by {header.SelfUuid}");
                        break;

                    case 5:
                        await HandleDecryptRequestAsync(stream, header, encrypted, secret);
                        Console.WriteLine($"Decryption request from {_connection.RemoteEndPoint} by {header.SelfUuid}");
                        break;

                    case 7:
                        secret = await HandleECDHRequestAsync(stream, header);
                        encrypted = true;
                        break;

                }
            }
        }

        public static byte[] EncryptData(byte[] plaintext, byte[] key)
        {
            byte[] encryptedData;
            byte[] nonce = new byte[12]; // AES-GCM standard nonce size (96 bits)
            byte[] tag = new byte[16]; // AES-GCM standard tag size

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce); // Generate a secure nonce
            }

            using (var aesGcm = new AesGcm(key, 16))
            {
                encryptedData = new byte[plaintext.Length];
                aesGcm.Encrypt(nonce, plaintext, encryptedData, tag);
            }

            // Combine nonce, ciphertext, and tag into a single byte array
            var combinedData = new byte[nonce.Length + encryptedData.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, combinedData, 0, nonce.Length);
            Buffer.BlockCopy(encryptedData, 0, combinedData, nonce.Length, encryptedData.Length);
            Buffer.BlockCopy(tag, 0, combinedData, nonce.Length + encryptedData.Length, tag.Length);

            return combinedData;
        }

        public static byte[] DecryptData(byte[] combinedData, byte[] key)
        {
            // Extract the nonce, ciphertext, and tag from the combinedData
            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[combinedData.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(combinedData, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(combinedData, nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(combinedData, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

            byte[] decryptedData = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(key, 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, decryptedData);
            }

            return decryptedData;
        }

        //This function is used to append data B to data A, and is used for stacking data before transmission
        public byte[] AppendData(byte[] A, byte[] B)
        {
            byte[] result = new byte[A.Length + B.Length];
            Buffer.BlockCopy(A, 0, result, 0, A.Length);
            Buffer.BlockCopy(B, 0, result, A.Length, B.Length);
            return result;
        }

        //This function is used to send data over a stream
        public async Task SendMessageAsync(QuicStream stream, MizukiHeader header, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(header.Opcode);
                    writer.Write(header.SelfUuid.ToByteArray());
                    writer.Write(header.TargetUuid.ToByteArray());
                    writer.Write(header.DataLength);
                    if (data.Length != header.DataLength)
                    {
                        throw new ArgumentException("Data length does not match the specified DataLength in the message.");
                    }
                    if (data != null && data.Length > 0)
                    {
                        writer.Write(data);
                    }
                    // No need to close the writer here, as it's being disposed by the using block
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(stream);
            } // MemoryStream is disposed here, but all operations on it are done

            await stream.FlushAsync(); // Ensure all data is sent
        }

        //This function will be used to send only the header without data
        public async Task SendHeaderAsync(QuicStream stream, MizukiHeader header)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(header.Opcode);
                    writer.Write(header.SelfUuid.ToByteArray());
                    writer.Write(header.TargetUuid.ToByteArray());
                    writer.Write(header.DataLength);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(stream);
            } // MemoryStream is disposed here, but all operations on it are done

            await stream.FlushAsync(); // Ensure all data is sent
        }


        //This function will be used to request for a key exchange
        public async Task<byte[]> RequestECDHAsync(QuicStream stream, Guid target)
        {


            byte[] PublicKeyBytes;
            ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng()
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };

            // Export public key to share with the other party
            PublicKeyBytes = ecdh.PublicKey.ToByteArray();

            MizukiHeader requestHeader = new MizukiHeader
            {
                Opcode = 7,
                SelfUuid = _ownUuid,
                TargetUuid = target,
                DataLength = (uint)PublicKeyBytes.Length
            };

            // Send your public key to the responder
            await SendMessageAsync(stream, requestHeader, PublicKeyBytes);

            Console.WriteLine("Awaiting ECDH Response");
            // Await the responder's public key
            byte[] ResponderPubKeyBytes = await HandleECDHResponseAsync(stream);
            Console.WriteLine("ECDH Response received");
            string hexString1 = BitConverter.ToString(ResponderPubKeyBytes).Replace("-", "");
            Console.WriteLine(hexString1);
            // Import the responder's public 
            ECDiffieHellmanPublicKey responderPublicKey = ECDiffieHellmanCngPublicKey.FromByteArray(ResponderPubKeyBytes, CngKeyBlobFormat.EccPublicBlob);
            // Derive the shared secret using the responder's public key
            Console.WriteLine("Deriving Secret");
            byte[] secret = ecdh.DeriveKeyMaterial(responderPublicKey);
            string hexString = BitConverter.ToString(secret).Replace("-", "");
            Console.WriteLine(hexString);
            Console.WriteLine($"Secret: {hexString}");
            // Clean up the ECDH instance
            ecdh.Dispose();

            // Return the derived shared secret
            EnderChest.controller.quicController.ClientKeys[target] = secret;
            return secret;
        }


        //This function will be used to process and handle a public key request

        public async Task<byte[]> HandleECDHRequestAsync(QuicStream stream, MizukiHeader header)
        {
            Console.WriteLine("Handling ECDH request");
            Guid requesterUuid = header.SelfUuid;
            // Read the requester's public key bytes from the stream
            byte[] requesterPubKeyBytes = await ReadDataAsync(stream, header.DataLength);

            byte[] secret;
            byte[] OwnPubKey; // Variable to store the responder's (own) ECDH public key
            ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng()
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
            OwnPubKey = ecdh.PublicKey.ToByteArray();
            ECDiffieHellmanPublicKey requesterPublicKey = ECDiffieHellmanCngPublicKey.FromByteArray(requesterPubKeyBytes, CngKeyBlobFormat.EccPublicBlob);
            secret = ecdh.DeriveKeyMaterial(requesterPublicKey);
            //using (ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng())
            //{
            //    ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            //    ecdh.HashAlgorithm = CngAlgorithm.Sha256;

            //    // Export the own (responder's) public key to share with the requester
            //    OwnPubKey = ecdh.PublicKey.ToByteArray();

            //    // Import the requester's public key
            //    //CngKey cngKey = CngKey.Import(requesterPubKeyBytes, CngKeyBlobFormat.EccPublicBlob);
            //    ECDiffieHellmanPublicKey requesterPublicKey = ECDiffieHellmanCngPublicKey.FromByteArray(requesterPubKeyBytes, CngKeyBlobFormat.EccPublicBlob);


            //    // Derive the shared secret using the requester's public key
            //    secret = ecdh.DeriveKeyMaterial(requesterPublicKey);
            //}

            // Optionally, respond with the own public key or a confirmation message
            // Here's an example of how you might send OwnPubKey back to the requester:
            MizukiHeader responseHeader = new MizukiHeader
            {
                Opcode = 8, // Assuming opcode 8 indicates a public key response
                SelfUuid = _ownUuid,
                TargetUuid = requesterUuid,
                DataLength = (uint)OwnPubKey.Length
            };
            await SendMessageAsync(stream, responseHeader, OwnPubKey);
            Console.WriteLine("Response Sent");
            string hexString = BitConverter.ToString(secret).Replace("-", "");
            Console.WriteLine(hexString);
            Console.WriteLine($"Secret: {hexString}");
            EnderChest.controller.quicController.ClientKeys[requesterUuid] = secret;
            return secret; // return the derived shared secret
        }

        public async Task<byte[]> HandleECDHResponseAsync(QuicStream stream)
        {


            //Read headers
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                if (header.Opcode == 8)
                {
                    Guid responderUuid = header.SelfUuid;
                    byte[] ResponseData = await ReadDataAsync(stream, header.DataLength);

                    return ResponseData;
                }
                else
                {
                    throw new Exception("Invalid response Opcode");
                }
            }
            else
            {
                throw new Exception("Null Header");
            }


        }

        //This function will be used to create a public key request
        public async Task<byte[]> RequestNTRUPublicKeyAsync(byte[] EncryptedInstanceIDBytes, QuicStream stream, Guid target)
        {
            
            MizukiHeader requestHeader = new MizukiHeader
            {
                Opcode = 1,
                SelfUuid = _ownUuid,
                TargetUuid = target,
                DataLength = (uint)EncryptedInstanceIDBytes.Length
            };
            await SendMessageAsync(stream, requestHeader, EncryptedInstanceIDBytes);
            Console.WriteLine("NTRU REQUEST SENT");
            byte[] encyptedResponseData = await HandleNTRUPublicKeyResponseAsync(stream);
            Console.WriteLine("NTRU REPSONSE RECEIVED");
            return encyptedResponseData;

        }


       


        //This function will be used to process and handle a NTRU public key request

        public async Task HandleNTRUPublicKeyRequestAsync(QuicStream stream, MizukiHeader header, bool encrypted, byte[] key)
        {
            //Read headers
            Console.WriteLine("NTRU REQUEST RECEIVED");
            Guid requesterUuid = header.SelfUuid;
            byte[] instanceIDBytes;
            if (encrypted)
            {
                Console.WriteLine("DECRYPTING INSTANCE ID");
                byte[] instanceIDBytesEncrypted = await ReadDataAsync(stream, header.DataLength);
                instanceIDBytes = DecryptData(instanceIDBytesEncrypted, key);
                string hexString = BitConverter.ToString(instanceIDBytes).Replace("-", "");
                Console.WriteLine($"DECRYPTED INSTANCE ID: {hexString}");
            }
            else
            {
                instanceIDBytes = await ReadDataAsync(stream, header.DataLength);
                Console.WriteLine("INSTANCE ID READ");
            }
            Guid InstanceID = new Guid(instanceIDBytes);

            Console.WriteLine($"Decrypted Instance GUID {InstanceID.ToString()}, My Own UUID is {_ownUuid.ToString()}");

            Encryption encryptionObject = new Encryption(InstanceID.ToString(), _ownUuid.ToString());
            string NTRUKeyPair = encryptionObject.GenerateKeyPair();

            Console.WriteLine("NTRU KEY GENERATED");

            byte[] encryptedNTRUKeyPair = Certificates.EncryptWithCert(EnderChest.controller.Certificate.Certificate, Encoding.UTF8.GetBytes(NTRUKeyPair));
            Console.WriteLine("NTRU KEY ENCRYPTED");
            EnderChest.controller.Database.InsertFile(InstanceID, Convert.ToBase64String(encryptedNTRUKeyPair));
            Console.WriteLine("NTRU KEY INSERTED INTO DB");
            byte[] publicKey = encryptionObject.PublicKey; 
            Console.WriteLine($"NTRU PUBLIC KEY: {Encoding.UTF8.GetString(publicKey)}");
            byte[] dataPacket = AppendData(InstanceID.ToByteArray(), publicKey); 
                                                                                 

            if (encrypted)
            {
                dataPacket = EncryptData(dataPacket, key);
                Console.WriteLine("RESPONSE ENCRYPTED");
            }

            MizukiHeader replyHeader = new MizukiHeader
            {
                Opcode = 2,
                SelfUuid = _ownUuid,
                TargetUuid = requesterUuid,
                DataLength = (uint)dataPacket.Length
            };
            //Send public key repsonse
            await SendMessageAsync(stream, replyHeader, dataPacket);
            Console.WriteLine("RESPONSE SENT");

        }


        //This function will be used to process and handle a public key response
        public async Task<byte[]> HandleNTRUPublicKeyResponseAsync(QuicStream stream)
        {

            if (_connection.RemoteEndPoint != _endPoint)
            {
                _endPoint = _connection.RemoteEndPoint;
            }
            //Read headers
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                if (header.Opcode == 2)
                {
                    //Guid responderUuid = header.SelfUuid;
                    byte[] ResponseData = await ReadDataAsync(stream, header.DataLength);

                    return ResponseData;
                }
                else
                {
                    throw new Exception("Invalid response Opcode");
                }
            }
            else
            {
                throw new Exception("Null Header");
            }


        }

        //This function will be used to create a RSA public key request
        public async Task<byte[]> RequestRSAPublicKeyAsync(QuicStream stream, Guid target)
        {

            MizukiHeader requestHeader = new MizukiHeader
            {
                Opcode = 9,
                SelfUuid = _ownUuid,
                TargetUuid = target,
                DataLength = 0
            };
            await SendHeaderAsync(stream, requestHeader);
            byte[] encyptedResponseData = await HandleRSAPublicKeyResponseAsync(stream);
            return encyptedResponseData;

        }

        //This function will be used to process and handle a RSA public key request

        public async Task HandleRSAPublicKeyRequestAsync(QuicStream stream, MizukiHeader header)
        {
            //Read headers
            Guid requesterUuid = header.SelfUuid;

            string certPublicKey = EnderChest.controller.Certificate.Certificate.ExportCertificatePem();
            byte[] publicKey = Encoding.UTF8.GetBytes(certPublicKey);

            MizukiHeader replyHeader = new MizukiHeader
            {
                Opcode = 10,
                SelfUuid = _ownUuid,
                TargetUuid = requesterUuid,
                DataLength = (uint)publicKey.Length
            };
            //Send public key repsonse
            await SendMessageAsync(stream, replyHeader, publicKey);

        }


        //This function will be used to process and handle a RSA public key response
        public async Task<byte[]> HandleRSAPublicKeyResponseAsync(QuicStream stream)
        {

            if (_connection.RemoteEndPoint != _endPoint)
            {
                _endPoint = _connection.RemoteEndPoint;
            }
            //Read headers
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                if (header.Opcode == 10)
                {
                    //Guid responderUuid = header.SelfUuid;
                    byte[] ResponseData = await ReadDataAsync(stream, header.DataLength);

                    return ResponseData;
                }
                else
                {
                    throw new Exception("Invalid response Opcode");
                }
            }
            else
            {
                throw new Exception("Null Header");
            }


        }

        //This function will be used to create an Encrypted File Sharing request
        public async Task<bool> CreateFileShareRequestAsync(string fileName, string filePath, Guid targetUuid, Guid instanceID, QuicStream stream)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            uint fileSize = (uint)fileInfo.Length;
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] instanceIDBytes = instanceID.ToByteArray();
            MizukiHeader requestHeader = new MizukiHeader
            {
                Opcode = 3,
                SelfUuid = _ownUuid,
                TargetUuid = targetUuid,
                DataLength = fileSize + (uint)fileNameBytes.Length + (uint)instanceIDBytes.Length            
            };
            await SendHeaderAsync(stream, requestHeader);
            await stream.WriteAsync(instanceIDBytes);
            byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(fileNameLengthBytes);
            }
            await stream.WriteAsync(fileNameLengthBytes); //4 bytes
            await stream.WriteAsync(fileNameBytes);
            uint chunkSize = 4 * 1024 * 1024;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

            }
            
            bool result = await HandleFileShareResponseAsync(stream);
            return result;
        }


        //This function will be used to handle a Encrypted File Sharing request
        public async Task HandleFileShareRequestAsync(QuicStream stream, MizukiHeader header)
        {
            Guid requesterUuid = header.SelfUuid;
            byte[] instanceIDBytes = new byte[16];
            await stream.ReadAsync(instanceIDBytes.AsMemory(0, 16));
            Guid instanceID = new Guid(instanceIDBytes);
            byte[] fileNameLengthBytes = new byte[4];
            await stream.ReadAsync(fileNameLengthBytes.AsMemory(0,4));
            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[fileNameLength];
            await stream.ReadAsync(fileNameBytes.AsMemory(0, fileNameLength));

            // Determine file path
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fileName = $"{Encoding.UTF8.GetString(fileNameBytes)}.mzk";
            string fullPath = Path.Combine(filePath, fileName);
            byte[] receivedHash = new byte[20];
            // Prepare to receive the file and hash
            uint chunkSize = 4 * 1024 * 1024; // 4 MB
            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[chunkSize];
                long totalDataLength = header.DataLength - 16 - 20; // Exclude instanceID and SHA1 hash length
                while (totalDataLength > 0)
                {
                    int toRead = (int)Math.Min(chunkSize, totalDataLength);
                    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, toRead));
                    if (bytesRead == 0) break;
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalDataLength -= bytesRead;
                }
                await stream.ReadAsync(receivedHash.AsMemory(0, 20));
                await fileStream.WriteAsync(receivedHash.AsMemory(0, 20));
            }

            // Compute the SHA1 hash of the written data
            byte[] computedHash;
            using (SHA1 sha1 = SHA1.Create())
            using (FileStream fileStream = File.OpenRead(fullPath))
            {
                computedHash = sha1.ComputeHash(fileStream);
            }
            bool isValid = true;
            if (receivedHash.Length != computedHash.Length)  isValid = false;

            for (int i = 0; i < receivedHash.Length; i++)
            {
                if (receivedHash[i] != computedHash[i])
                {
                    isValid = false;
                    break;
                }
            }
            // Compare the hashes
            byte opcode;
            if (!isValid)
            {
                //File.Delete(fullPath);
                opcode = 0;
            }
            else
            {
                opcode = 4;
            }

            // Send response header
            MizukiHeader responseHeader = new MizukiHeader
            {
                Opcode = opcode,
                SelfUuid = _ownUuid,
                TargetUuid = requesterUuid,
                DataLength = 0
            };
            await SendHeaderAsync(stream, responseHeader);
        }

        public async Task<bool> HandleFileShareResponseAsync(QuicStream stream)
        {
            if (_connection.RemoteEndPoint != _endPoint)
            {
                _endPoint = _connection.RemoteEndPoint;
            }
            //Read headers
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                if (header.Opcode == 4)
                {
                    //Guid responderUuid = header.SelfUuid;

                    return true;
                }
                else if (header.Opcode == 0)
                {
                    return false;
                }
                else
                {
                    throw new Exception("Invalid response Opcode");
                }
            }
            else
            {
                throw new Exception("Null Header");
            }
        }


        //This function will be used to create a File Decrpytion request
        public async Task<byte[]> CreateDecryptRequestAsync(Guid instanceID, QuicStream stream, byte[] encryptedShare, Guid target)
        {
            byte[] data = AppendData(instanceID.ToByteArray(), encryptedShare);
            MizukiHeader requestHeader = new MizukiHeader
            {
                Opcode = 5,
                SelfUuid = _ownUuid,
                TargetUuid = target,
                DataLength = (uint)data.Length
            };
            await SendMessageAsync(stream, requestHeader, data);
            byte[] responderShareEncrypted = await HandleDecryptionResponseAsync(stream, target);
            // if (responderShareEncrypted.Length == 1 && responderShareEncrypted[0] == 0), means that target was unable to find share
            return responderShareEncrypted;
        }

        //This function will be used to handle a File Decryption request
        public async Task HandleDecryptRequestAsync(QuicStream stream, MizukiHeader header, bool encrypted, byte[] key)
        {
            //Read headers
            Guid requesterUuid = header.SelfUuid;
            (Guid instanceID, byte[] encryptedShare) = await ReadUuidAndDataAsync(stream, header.DataLength);
            bool readSuccessful = false;
            byte[] decryptedInnerShare = new byte[0];
            bool unknown = false;

            List<string> requesterInfo = EnderChest.controller.Database.GetFriendFromGuid(requesterUuid);
            if (requesterInfo != null)
            {
                List<string> instanceInformation = EnderChest.controller.Database.FindFileByInstanceID(instanceID);
                if (instanceInformation != null)
                {
                    DialogResult diagResult = MessageBox.Show($"{requesterInfo[1]} is trying to Decrypt {instanceInformation[2]}.", "Decryption Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    if (diagResult == DialogResult.OK)
                    {
                        MessageBox.Show("The Decryption Request was Allowed, Begin Decryption");
                        byte[] decryptedShare = EnderChest.controller.Certificate.DecryptWithMyCert(encryptedShare);
                        if (decryptedShare != null)
                        {
                            int offset = 0;

                            byte[] peerCountBytes = new byte[2];
                            Array.Copy(decryptedShare, 0, peerCountBytes, 0, 2);
                            offset += 2;
                            short peerCount = BitConverter.ToInt16(peerCountBytes);

                            byte[] guidS = new byte[peerCount * 16];
                            Array.Copy(decryptedShare, offset, guidS, 0, (peerCount * 16));
                            offset += peerCount * 16;

                            int encryptedSSSLength = decryptedShare.Length - offset;
                            byte[] encryptedSSSKey = new byte[encryptedSSSLength];
                            Array.Copy(decryptedShare, offset, encryptedSSSKey, 0, encryptedSSSLength);


                            string encryptedNTRUPrivPubKey = instanceInformation[1];
                            byte[] encryptedNTRUPrivPubKeyBytes = Convert.FromBase64String(encryptedNTRUPrivPubKey);
                            byte[] decryptedNTRUPrivPubKeyBytes = EnderChest.controller.Certificate.DecryptWithMyCert(encryptedNTRUPrivPubKeyBytes);
                            if (decryptedNTRUPrivPubKeyBytes != null)
                            {
                                string decryptedNTRUPrivPubKey = Encoding.UTF8.GetString(decryptedNTRUPrivPubKeyBytes);
                                Encryption encryptionObj = new Encryption(instanceID.ToString(), EnderChest.controller.UUID, encryptedSSSKey);
                                encryptionObj.GenerateKeyPair(decryptedNTRUPrivPubKey);
                                decryptedInnerShare = encryptionObj.NtruDecrypt(encryptedSSSKey);
                                readSuccessful = true;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("The Decryption request was rejected.");
                    }
                }
                else
                {
                    MessageBox.Show($"{requesterUuid.ToString()} (Known) Attempted to Decrypt a file not found in your DataBase");
                }
            }
            else
            {
                unknown = true;
            }

            byte opcode;

            if (readSuccessful)
            {
                opcode = 6;
            }
            else
            {
                opcode = 0;
            }

            //Encrypt share data if needed
            if (encrypted)
            {
                decryptedInnerShare = EncryptData(decryptedInnerShare, key);
            }
            MizukiHeader responseHeader = new MizukiHeader
            {
                Opcode = opcode,
                SelfUuid = _ownUuid,
                TargetUuid = requesterUuid,
                DataLength = (uint)decryptedInnerShare.Length
            };
            //Send File Decryption Response
            await SendMessageAsync(stream, responseHeader, decryptedInnerShare);

            if (unknown) MessageBox.Show($"{requesterUuid.ToString()} (Unknown) Attempted to Decrypt a file with you. This was automatically rejected.");
        }

        //This function will be used to handle a File Decryption Response
        public async Task<byte[]> HandleDecryptionResponseAsync(QuicStream stream, Guid targetUuid)
        {
            if (_connection.RemoteEndPoint != _endPoint)
            {
                _endPoint = _connection.RemoteEndPoint;
            }
            var header = await ReadHeaderAsync(stream);
            if (header != null)
            {
                if (header.Opcode == 6 && header.SelfUuid == targetUuid)
                {
                    //Guid responderUuid = header.SelfUuid;
                    byte[] responderShareEncrypted = await ReadDataAsync(stream, header.DataLength);
                    return responderShareEncrypted;
                }
                else if (header.Opcode == 0)
                {
                    
                    return [header.Opcode];
                }
                else
                {
                    throw new Exception("Invalid response");
                }
            }
            else
            {
                throw new Exception("Null header");
            }

        }



        public async Task<MizukiHeader> ReadHeaderAsync(QuicStream stream)
        {
            // Adjust the buffer size to accommodate the new header structure
            byte[] buffer = new byte[1 + 16 + 16 + 4]; // 1 byte for Opcode, 16 bytes for SelfUuid, 16 bytes for TargetUuid, 4 bytes for DataLength
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead < buffer.Length) throw new Exception("Stream ended before all header data could be read.");

            return new MizukiHeader
            {
                Opcode = buffer[0],
                SelfUuid = new Guid(new ReadOnlySpan<byte>(buffer, 1, 16)),
                TargetUuid = new Guid(new ReadOnlySpan<byte>(buffer, 17, 16)),
                DataLength = BitConverter.ToUInt32(buffer, 33)
            };
        }


        public async Task<byte[]> ReadDataAsync(QuicStream stream, uint dataLength)
        {
            byte[] dataBuffer = new byte[dataLength];
            await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length); // Read the remaining data
            return dataBuffer;
        }
        public async Task<(Guid targetUuid, byte[] data)> ReadUuidAndDataAsync(QuicStream stream, uint dataLength)
        {
            byte[] uuidBuffer = new byte[16];
            byte[] dataBuffer = new byte[dataLength];

            await stream.ReadAsync(uuidBuffer, 0, uuidBuffer.Length); // Read the target UUID
            await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length); // Read the remaining data

            return (new Guid(uuidBuffer), dataBuffer);
        }

        public async Task WriteDataToFileAsync(QuicStream stream, MizukiHeader header)
        {
            string tempPath = Path.GetTempPath();
            string fileName = $"{header.SelfUuid}_{DateTime.UtcNow:yyyyMMddHHmmss}_stream{stream.Id}.tmp";
            string fullPath = Path.Combine(tempPath, fileName);

            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[4096];
                uint totalRead = 0;
                while (totalRead < header.DataLength)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += (uint)bytesRead;
                }
            }

            Console.WriteLine($"OpCode: {header.Opcode}");
            Console.WriteLine($"UUID: {header.SelfUuid}");
            Console.WriteLine($"dataLength: {header.DataLength}");
            Console.WriteLine($"File: {fullPath}");
        }
    }
}



