using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;
using System.Reflection.Metadata;

namespace Mizuki.Classes.QuicCommunication
{
    [RequiresPreviewFeatures]
    internal class QuicController
    {
        private ConcurrentDictionary<Guid, QuicConnectioner> _clientConnections = new ConcurrentDictionary<Guid, QuicConnectioner>(); //This is a Entry for All Connections Globally.
        private ConcurrentDictionary<Guid, byte[]> _clientKeys = new ConcurrentDictionary<Guid, byte[]>();
        private Guid _myUUID;
        private X509Certificate2 _myCertificate;
        private bool _relayed;
        private bool _caed;
        private QuicHandler _quicHandler;


        public ConcurrentDictionary<Guid, byte[]> ClientKeys
        {
            get { return _clientKeys; }
        }
        public QuicController(Guid uuid, X509Certificate2 clientCertificate, bool relayed, bool caed)
        {
            _myUUID = uuid;
            _myCertificate = clientCertificate;
            _relayed = relayed;
            _caed = caed;
            _quicHandler = new QuicHandler(uuid, relayed, caed);
            if (_relayed)
            {
                EstablishConnectionToServer(_quicHandler);
            }
            else
            {
                _quicHandler.StartAsyncListener(_myCertificate);       
                //Establish connection to all peers
                /*
                List<List<string>> friendsInfo = new List<List<string>>();
                friendsInfo = EnderChest.controller.Database.GetFriends();
                foreach (var friendInfo in friendsInfo)
                {
                    _ = Task.Run(() => EstablishConnectionToPeer(new Guid(friendInfo[0]), IPAddress.Parse(friendInfo[3]), QuicHandle));
                }*/
            }
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///     
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        public async void EstablishConnectionToServer(QuicHandler QuicHandle)
        {
            
            
            string result = await EstablishConnectionToRelayServer(QuicHandle);
            while (result != "no err")
            {
                await Task.Delay(5000);
                result = await EstablishConnectionToRelayServer(QuicHandle);
            }
            _ = Task.Run(() => ListenerForRelayServer());
        }

        public async Task ListenerForRelayServer()
        {
            while (true)
            {
                await using var incStream = await _clientConnections[Guid.Empty].Connection.AcceptInboundStreamAsync();
                await _clientConnections[Guid.Empty].HandleStreamAsync(incStream);
            }
        }

        public async Task<string> EstablishConnectionToRelayServer(QuicHandler QuicHandle)
        {
            try
            {
                QuicConnection quicConn = await QuicHandle.createConnection(Dns.GetHostEntry("relay.mizuki.ltd").AddressList.First(), 31038, _myCertificate);
                QuicConnectioner quicConner = new QuicConnectioner(quicConn, _myUUID);
                quicConner.ClientUUID = Guid.Empty;
                _clientConnections[Guid.Empty] = quicConner;
                QuicStream stream = await quicConner.CreateStream();
                MizukiHeader helloWorldToRelay = new MizukiHeader()
                {
                    Opcode = 15,
                    SelfUuid = _myUUID,
                    TargetUuid = Guid.Empty,
                    DataLength = 0,
                };
                
                await quicConner.SendHeaderAsync(stream, helloWorldToRelay);
                await stream.DisposeAsync();

            }
            catch (Exception e)
            {
                //Exception to be used when deployed.

                string errcode = $"Exception Error was caused by: {e}";
                return errcode;
            }
            return "no err";
        }

        public async Task<bool> ConnectToPeer(Guid PeerGuid, IPAddress PeerIP, QuicHandler QuicHandle)
        {
            try
            {
                QuicConnection connection = await QuicHandle.createConnection(PeerIP, 31038, _myCertificate);
                QuicConnectioner connectionHandle = new QuicConnectioner(connection, _myUUID);
                connectionHandle.ClientUUID = PeerGuid;
                _clientConnections[PeerGuid] = connectionHandle;
                
            }
            catch (Exception e) 
            {
                Console.WriteLine($"Error while Connecting to Peer: {PeerGuid},  IP: {PeerIP}");
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }
        public async Task EstablishConnectionToPeer(Guid PeerGuid, IPAddress PeerIP, QuicHandler QuicHandle)
        {
            bool result = await ConnectToPeer(PeerGuid, PeerIP, QuicHandle);
            while (!result)
            {
                await Task.Delay(5000);
                result = await ConnectToPeer(PeerGuid, PeerIP, QuicHandle);
            }
        }
        
        public async Task<byte[]> RequestPeerNTRUKey(Guid peerGUID, byte[] instanceID)
        { 
            try
            {
                if (_relayed)
                {
                    Console.WriteLine("SANITY DONE");
                    QuicStream stream = await _clientConnections[Guid.Empty].CreateStream();
                    byte[] AesOneTimeKey = await _clientConnections[Guid.Empty].RequestECDHAsync(stream, peerGUID);
                    Console.WriteLine("ECDH DONE");
                    await stream.DisposeAsync();
                    byte[] encryptedInstanceID = QuicConnectioner.EncryptData(instanceID, AesOneTimeKey);
                    Console.WriteLine("Instance ID encrypted");
                    QuicStream stream2 = await _clientConnections[Guid.Empty].CreateStream();
                    Console.WriteLine("REQUESTING NTRU KEY");
                    byte[] encryptedNTRUKey = await _clientConnections[Guid.Empty].RequestNTRUPublicKeyAsync(encryptedInstanceID, stream2, peerGUID);
                    await stream2.DisposeAsync();
                    Console.WriteLine("REQ NTRU DONE");
                    byte[] decryptedPacket = QuicConnectioner.DecryptData(encryptedNTRUKey, AesOneTimeKey);
                    byte[] decryptedNTRUKey = new byte[decryptedPacket.Length - 16];
                    Array.Copy(decryptedPacket, 16, decryptedNTRUKey, 0, decryptedPacket.Length - 16);
                    Console.WriteLine($"NTRU Acquired {Encoding.UTF8.GetString(decryptedNTRUKey)}");
                    return decryptedNTRUKey;
                }
                else
                {
                    if (_clientConnections.TryGetValue(peerGUID, out _) == false)
                    {
                        List<string> peerInfo = EnderChest.controller.Database.GetFriendFromGuid(peerGUID);
                        await EstablishConnectionToPeer(peerGUID, IPAddress.Parse(peerInfo[3]), _quicHandler);
                    }
                    QuicStream stream = await _clientConnections[peerGUID].CreateStream();
                    byte[] NTRUKeyPacket = await _clientConnections[peerGUID].RequestNTRUPublicKeyAsync(instanceID, stream, peerGUID);
                    await stream.DisposeAsync();
                    byte[] NTRUKey = new byte[NTRUKeyPacket.Length - 16];
                    Array.Copy(NTRUKeyPacket, 16, NTRUKey, 0, NTRUKeyPacket.Length - 16);
                    Console.WriteLine($"NTRU Acquired {Encoding.UTF8.GetString(NTRUKeyPacket)}");
                    return NTRUKey;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            
        }

        public async Task<byte[]> RequestPeerRSAKey(Guid peerGuid)
        {
            try
            {
                if (_relayed)
                {
                    QuicStream stream = await _clientConnections[Guid.Empty].CreateStream();
                    byte[] peerRSApublicKey = await _clientConnections[Guid.Empty].RequestRSAPublicKeyAsync(stream, peerGuid);
                    await stream.DisposeAsync();
                    return peerRSApublicKey;
                }
                else
                {
                    QuicStream stream = await _clientConnections[peerGuid].CreateStream();
                    byte[] peerRSApublicKey = await _clientConnections[peerGuid].RequestRSAPublicKeyAsync(stream, peerGuid);
                    await stream.DisposeAsync();
                    return peerRSApublicKey;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<byte[]> RequestDecryptShare(Guid peerGUID, Guid instanceID, byte[] encryptedShare)
        {
            try
            {
                if (_relayed)
                {
                    Console.WriteLine("SANITY DONE");
                    QuicStream stream = await _clientConnections[Guid.Empty].CreateStream();
                    byte[] AesOneTimeKey = await _clientConnections[Guid.Empty].RequestECDHAsync(stream, peerGUID);
                    Console.WriteLine("ECDH DONE");
                    await stream.DisposeAsync();

                    QuicStream stream2 = await _clientConnections[Guid.Empty].CreateStream();
                    Console.WriteLine("REQUESTING Decypted Share");
                    byte[] encryptedDecryptedShare = await _clientConnections[Guid.Empty].CreateDecryptRequestAsync(instanceID, stream2, encryptedShare, peerGUID);
                    await stream2.DisposeAsync();
                    Console.WriteLine("REQ NTRU DONE");
                    byte[] decryptedDecryptedShare = QuicConnectioner.DecryptData(encryptedDecryptedShare, AesOneTimeKey);
                    Console.WriteLine($"Decrypted Share Acquired {Encoding.UTF8.GetString(decryptedDecryptedShare)}");
                    return decryptedDecryptedShare;
                }
                else
                {
                    if (_clientConnections.TryGetValue(peerGUID, out _) == false)
                    {
                        List<string> peerInfo = EnderChest.controller.Database.GetFriendFromGuid(peerGUID);
                        await EstablishConnectionToPeer(peerGUID, IPAddress.Parse(peerInfo[3]), _quicHandler);
                    }
                    QuicStream stream = await _clientConnections[peerGUID].CreateStream();
                    byte[] encryptedDecryptedShare = await _clientConnections[peerGUID].CreateDecryptRequestAsync(instanceID, stream, encryptedShare, peerGUID);
                    await stream.DisposeAsync();
                    return encryptedDecryptedShare;
                }
            }
            catch (Exception ex) 
            {
                return null;
            }
        }

        public async void RequestFileShare(Guid peerGUID, string fileName, string filePath, Guid instanceID)
        {
            if (_relayed)
            {
                QuicStream stream = await _clientConnections[Guid.Empty].CreateStream();
                bool sendFileResult = await _clientConnections[Guid.Empty].CreateFileShareRequestAsync(fileName, filePath, peerGUID, instanceID, stream);
                await stream.DisposeAsync();
            }
            else
            {
                if (_clientConnections.TryGetValue(peerGUID, out _) == false)
                {
                    List<string> peerInfo = EnderChest.controller.Database.GetFriendFromGuid(peerGUID);
                    await EstablishConnectionToPeer(peerGUID, IPAddress.Parse(peerInfo[3]), _quicHandler);
                }
                QuicStream stream = await _clientConnections[peerGUID].CreateStream();
                bool sendFileResult = await _clientConnections[peerGUID].CreateFileShareRequestAsync(fileName, filePath, peerGUID, instanceID, stream);
                await stream.DisposeAsync();
            }
        }
    }
}
