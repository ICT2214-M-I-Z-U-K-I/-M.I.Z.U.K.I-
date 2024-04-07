using Mizuki.Classes.QuicCommunication;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Quic;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.NTRU;
using VTDev.Libraries.CEXEngine.Crypto.Generator;
using static Mizuki.Classes.FileIO;

namespace Mizuki.Classes
{
    [RequiresPreviewFeatures]
    internal class Controller
    {
        private Guid _myUUID;
        private Certificates _myCertificate;
        private Database _myDatabase;
        private QuicController _myQuicController;
        private bool _relayed;
        private bool _caed;

        public bool HasProfile
        {
            get
            {
                if (_myUUID != null)
                {
                    return true;
                }
                return false;
            }
        }

        public QuicController quicController
        {
            get { return _myQuicController; }
        }
        public string UUID
        {
            get { return _myUUID.ToString(); }
        }

        public Database Database
        {
            get { return _myDatabase; }
            set { _myDatabase = value; }
        }

        public Certificates Certificate
        {
            get { return _myCertificate; }
            set { _myCertificate = value; }
        }

        public Controller()
        {
            _caed = true;
            _relayed = true;

            CheckFirst();
            LoadConfig();
        }

        public void CheckFirst()
        {
            if (Certificates.CheckMizukiCertificate() == 0)
            {
                RegisterDevice();
            }
        }

        public void LoadConfig()
        {
            _myCertificate = new Certificates();
            _myUUID = _myCertificate.UUID;
            _myDatabase = new Database(_myUUID, _myCertificate.GetSha256OfPrivateKey());
            _myQuicController = new QuicController(_myUUID, _myCertificate.Certificate, _relayed, _caed);
        }

        public void RegisterDevice()
        {
            //Should Have a two flows, CA mode or Standalone Mode
            _myUUID = Guid.NewGuid();

            if (_caed == true)
            {
                Certificates.CreateSignedCertificate(_myUUID);
            }
            else
            {
                Certificates.CreateSelfSignedCertificate(_myUUID);
            }
        }

        public Dictionary<string, string> GetFriendsList()
        {
            Dictionary<string, string> friendListRetuner = new Dictionary<string, string>();

            List<List<string>> friendsList = _myDatabase.GetFriends();
            foreach (List<string> friend in friendsList)
            {
                friendListRetuner.Add(friend[0], friend[1]);
            }
            return friendListRetuner;
        }

        public X509Certificate2 GetPeerRSACertificate(Guid peerUUID)
        {
            if (peerUUID == _myUUID) return EnderChest.controller.Certificate.Certificate;
            X509Certificate2 peerCertificate = _myDatabase.GetCertFromUUID(peerUUID);
            if (peerCertificate == null)
            {
                if (_caed)
                {
                    var peerRequestCATask = Task.Run(async () => await Certificates.GetCertFromGuid(peerUUID));
                    if (peerRequestCATask.Result != null)
                    {
                        peerCertificate = peerRequestCATask.Result;
                    }
                }
                else
                {
                    var peerRequestCATask = Task.Run(async () => await _myQuicController.RequestPeerRSAKey(peerUUID));
                    if (peerRequestCATask.Result != null)
                    {
                        byte[] peerCertByteArray = peerRequestCATask.Result;
                        peerCertificate = X509Certificate2.CreateFromPem(Encoding.UTF8.GetString(peerCertByteArray));
                    }
                }

                _myDatabase.InsertFriend(peerUUID.ToString(), null, Convert.ToBase64String(peerCertificate.Export(X509ContentType.Cert)), null);
            }

            return peerCertificate;
        }

        public async Task<string> EncryptFile(List<Guid> members, int threshold, string filePath)
        {
            Guid fileInstanceID = Guid.NewGuid();
            Encryption encryptionInstance = new Encryption(fileInstanceID.ToString(), _myUUID.ToString());
            string NTRUPubPrivKeyPair = encryptionInstance.GenerateKeyPair();
            byte[] NTRUPubPrivKeyPairBytes = Encoding.UTF8.GetBytes(NTRUPubPrivKeyPair);
            byte[] encryptedNTRUPrivPubKeyPairBytes = Certificates.EncryptWithCert(_myCertificate.Certificate, NTRUPubPrivKeyPairBytes);

            if (encryptedNTRUPrivPubKeyPairBytes != null)
            {
                string encryptedNTRUPrivPubKey = Convert.ToBase64String(encryptedNTRUPrivPubKeyPairBytes);
                _myDatabase.InsertFile(fileInstanceID, encryptedNTRUPrivPubKey, Path.GetFileName(filePath));

                List<Task> tasks = new List<Task>();
                //ConcurrentDictionary<string, byte[]> memberGUIDKeyDict = new ConcurrentDictionary<string, byte[]>();
                for (int i = 1; i < members.Count; i++)
                {
                    int k = i;
                    string memberUUID = members[k].ToString();
                    tasks.Add(Task.Run(async () =>
                    {
                        byte[] peerNTRUPubkey = await _myQuicController.RequestPeerNTRUKey(members[k], fileInstanceID.ToByteArray());
                        //memberGUIDKeyDict[memberUUID] = peerNTRUPubkey;
                        encryptionInstance.LoadMembersKey(memberUUID, peerNTRUPubkey);
                    }
                    ));
                }
                await Task.WhenAll(tasks);
                /*
                foreach(KeyValuePair<string, byte[]> kvp in memberGUIDKeyDict)
                {
                    encryptionInstance.LoadMembersKey(kvp.Key, kvp.Value);
                }*/

                byte[] rawData = File.ReadAllBytes(filePath);
                Console.WriteLine($"Read the File : {rawData}");

                Dictionary<string, byte[]> encryptedInnerShares = encryptionInstance.Encrypt(rawData, threshold);

                FileInfo encryptedMZKInfo = new FileInfo(Path.GetFullPath(filePath) + ".mzk");
                FileIO encryptedMZKFileIO = new FileIO(encryptedMZKInfo);
                FileIO.MizukiFileContent encryptedMZKFileStructure = new FileIO.MizukiFileContent();

                encryptedMZKFileIO.MZKStructure = encryptedMZKFileStructure;
                encryptedMZKFileIO.MZKStructure.FileInstanceID = fileInstanceID;
                encryptedMZKFileIO.MZKStructure.EncryptedData = encryptionInstance.EncryptedData;
                encryptedMZKFileIO.MZKStructure.EncryptedDataLength = encryptionInstance.EncryptedData.Length;
                encryptedMZKFileIO.MZKStructure.Shares = new List<FileIO.MizukuiShareContent>();
                encryptedMZKFileIO.MZKStructure.Threshold = (short)threshold;
                encryptedMZKFileIO.MZKStructure.PeerCount = (short)members.Count;

                byte[] uuidsStacked = new byte[0];
                foreach (string uuid in encryptedInnerShares.Keys)
                {
                    Guid GuidU = new Guid(uuid);
                    uuidsStacked = FileIO.AppendData(uuidsStacked, GuidU.ToByteArray());
                }

                foreach (KeyValuePair<string, byte[]> kvp in encryptedInnerShares)
                {
                    FileIO.MizukuiShareContent outerShare = new FileIO.MizukuiShareContent((short)members.Count, uuidsStacked, kvp.Value);
                    encryptedMZKFileIO.MZKStructure.Shares.Add(outerShare);
                }

                byte[] encryptedSharesStacked = new byte[0];
                int j = 0;
                foreach (string uuid in encryptedInnerShares.Keys)
                {
                    string uuidI = uuid;
                    X509Certificate2 peerCert = GetPeerRSACertificate(new Guid(uuidI));
                    if (peerCert != null)
                    {
                        byte[] encryptedShare = new byte[0];
                        FileIO.MizukuiShareContent share = encryptedMZKFileIO.MZKStructure.Shares[j];
                        encryptedShare = Certificates.EncryptWithCert(peerCert, share.GetByteForm);
                        encryptedMZKFileIO.MZKStructure.ShareLength = encryptedShare.Length;
                        encryptedSharesStacked = FileIO.AppendData(encryptedSharesStacked, encryptedShare);
                        j++;
                    }
                    else
                    {
                        return $"Unable to Attain Certificate of {uuidI}";
                    }
                }
                encryptedMZKFileIO.MZKStructure.EncryptedShares = encryptedSharesStacked;
                encryptedMZKFileIO.WriteMizuki();

                Console.WriteLine("Encrypted MZK");

                return "Encrypted File!";
            }
            else
            {
                return "Unable to Encrypt File, Certificate Might have Error.";
            }
        }

        public async Task<Tuple<string, string>> DecryptFile(string filePath)
        {
            FileInfo encryptedMZKInfo = new FileInfo(filePath);
            FileIO encryptedMZKFileIO = new FileIO(encryptedMZKInfo);
            encryptedMZKFileIO.ParseMizuki();

            Console.WriteLine("Parsed MZK");
            Dictionary<Guid, byte[]> GuidToShareDict = new Dictionary<Guid, byte[]>();
            MizukuiShareContent myShare = new MizukuiShareContent();
            (GuidToShareDict, myShare) = encryptedMZKFileIO.AcquireShares();

            if (myShare != null)
            {
                List<string> decryptedInnerSharesList = new List<string>();

                List<string> instanceInformation = _myDatabase.FindFileByInstanceID(encryptedMZKFileIO.MZKStructure.FileInstanceID);
                string encryptedNTRUPrivPubKey = instanceInformation[1];
                byte[] encryptedNTRUPrivPubKeyBytes = Convert.FromBase64String(encryptedNTRUPrivPubKey);
                byte[] decryptedNTRUPrivPubKeyBytes = _myCertificate.DecryptWithMyCert(encryptedNTRUPrivPubKeyBytes);

                if (decryptedNTRUPrivPubKeyBytes != null)
                {
                    string decryptedNTRUPrivPubKey = Encoding.UTF8.GetString(decryptedNTRUPrivPubKeyBytes);
                    Encryption encryptionObj = new Encryption(encryptedMZKFileIO.MZKStructure.FileInstanceID.ToString(), UUID, myShare.InnerShare);
                    encryptionObj.GenerateKeyPair(decryptedNTRUPrivPubKey);

                    foreach (KeyValuePair<Guid, byte[]> guidToShareObject in GuidToShareDict)
                    {
                        if (guidToShareObject.Key != _myUUID)
                        {
                            byte[] decryptedPeerInnerShare = await _myQuicController.RequestDecryptShare(guidToShareObject.Key, encryptedMZKFileIO.MZKStructure.FileInstanceID, guidToShareObject.Value);
                            if (decryptedPeerInnerShare != null)
                            {
                                decryptedInnerSharesList.Add(Encoding.UTF8.GetString(decryptedPeerInnerShare));
                            }
                            else
                            {
                                Console.WriteLine($"{guidToShareObject.Key.ToString()} is unreachable, skipping..");
                            }
                        }
                    }

                    if (decryptedInnerSharesList.Count + 1 >= encryptedMZKFileIO.MZKStructure.Threshold)
                    {
                        byte[] decryptedRawData = encryptionObj.Decrypt(encryptedMZKFileIO.MZKStructure.EncryptedData, decryptedInnerSharesList);
                        string decryptedString = Encoding.UTF8.GetString(decryptedRawData);
                        string newPath = filePath.Replace(".mzk", "");
                        File.WriteAllBytes(newPath, decryptedRawData);
                        return Tuple.Create("File Decrypted Successfully", decryptedString);
                    }
                    else
                    {
                        return Tuple.Create($"Unable to Decrypt, Threshold is {encryptedMZKFileIO.MZKStructure.Threshold} but only {decryptedInnerSharesList.Count + 1} is available.", string.Empty);
                    }
                }
                else
                {
                    return Tuple.Create("Unable to Decrypt NTRUKey with Certificate from Database", string.Empty);
                }
            }
            else
            {
                return Tuple.Create("MZK File does not belong to you.", string.Empty);
            }
        }
    }
}
