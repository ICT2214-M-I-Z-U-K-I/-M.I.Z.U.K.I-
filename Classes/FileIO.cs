using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Org.BouncyCastle.Utilities;
using System.Drawing.Text;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text.Unicode;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Collections;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Paddings;
using static System.Runtime.InteropServices.JavaScript.JSType;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Tls;

namespace Mizuki.Classes
{
    [RequiresPreviewFeatures]
    internal class FileIO
    {
        private FileInfo _fileInfo;
        private MizukiFileContent _mzkContent;

        public class MizukuiShareContent
        {
            private short _uuidCount;
            private byte[] _uuids;        //take _uuidCount * 16
            private byte[] _encryptedNRTUContent; //SSS Key that was encrypted with NTRU.

            public int Length
            {
                get { return GetByteForm.Length; }
            }

            public byte[] GetByteForm
            {
                get
                {
                    return AppendData(AppendData(BitConverter.GetBytes(_uuidCount), _uuids), _encryptedNRTUContent);
                }
            }

            public byte[] InnerShare
            {
                get { return _encryptedNRTUContent; }
            }

            public MizukuiShareContent() { }

            public MizukuiShareContent(short uuidCount, byte[] uuids, byte[] encryptedNRTUContent)
            {
                _uuidCount = uuidCount;
                _uuids = uuids;
                _encryptedNRTUContent = encryptedNRTUContent;
            }
        }

        public class MizukiFileContent
        {
            private Guid _fileInstanceID;
            private short _peerCount;
            private short _threshold;
            private int _shareLength;
            private List<MizukuiShareContent> _shares;
            private byte[] _encryptedShares;
            private int _encryptedDataLength;
            private byte[] _encryptedData;
            private byte[] _shaDigest;

            public Guid FileInstanceID
            {
                get { return _fileInstanceID; }
                set { _fileInstanceID = value; }
            }

            public byte[] ShaGUID
            {
                get
                {
                    SHA1 sha1Obj = SHA1.Create();
                    return sha1Obj.ComputeHash(FileInstanceID.ToByteArray());
                }
            }

            public short PeerCount
            {
                get { return _peerCount; }
                set { _peerCount = value; }
            }

            public short Threshold
            {
                get { return _threshold; }
                set { _threshold = value; }
            }

            public int ShareLength
            {
                get { return _shareLength; }
                set { _shareLength = value; }
            }

            public List<MizukuiShareContent> Shares
            {
                get { return _shares; }
                set { _shares = value; }
            }

            public byte[] EncryptedShares
            {
                get { return _encryptedShares; }
                set { _encryptedShares = value; }
            }

            public int EncryptedDataLength
            {
                get { return _encryptedDataLength; }
                set { _encryptedDataLength = value; }
            }

            public byte[] EncryptedData
            {
                get { return _encryptedData; }
                set { _encryptedData = value; }
            }

            public byte[] ShaDigest
            {
                get { return _shaDigest; }
                set { _shaDigest = value; }
            }
        }

        public FileInfo FileInfo
        {
            get { return _fileInfo; }
            set { _fileInfo = value; }
        }

        public MizukiFileContent MZKStructure
        {
            get { return _mzkContent; }
            set { _mzkContent = value; }
        }

        public FileIO (FileInfo fileInformation)
        {
            _fileInfo = fileInformation;
        }


        public static byte[] AppendData(byte[] A, byte[] B)
        {
            byte[] result = new byte[A.Length + B.Length];
            Buffer.BlockCopy(A, 0, result, 0, A.Length);
            Buffer.BlockCopy(B, 0, result, A.Length, B.Length);
            return result;
        }
        

        public void WriteMizuki()
        {
            using (FileStream fileStreamW = _fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                byte[] mzk = Encoding.UTF8.GetBytes("MZK");
                fileStreamW.Write(mzk); //3
                fileStreamW.Write(_mzkContent.FileInstanceID.ToByteArray()); //16
                fileStreamW.Write(_mzkContent.ShaGUID); //20

                short count = (short)_mzkContent.Shares.Count;
                byte[] countBytes = BitConverter.GetBytes(count);
                fileStreamW.Write(countBytes); //2

                short threshold = (short)_mzkContent.Threshold;
                byte[] thresholdBytes = BitConverter.GetBytes(threshold);
                fileStreamW.Write(thresholdBytes);

                fileStreamW.Write(BitConverter.GetBytes(_mzkContent.ShareLength));
                fileStreamW.Write(_mzkContent.EncryptedShares);

                fileStreamW.Write(BitConverter.GetBytes(_mzkContent.EncryptedDataLength)); //4
                fileStreamW.Write(_mzkContent.EncryptedData); // determined by dataLength
            }

            /*
            using (var sha1 = SHA1.Create())
            {
                // Compute SHA1 hash of the file content
                byte[] hash = sha1.ComputeHash();
                using (StreamWriter fileStreamWriter = new StreamWriter(fileStreamW))
                {
                    // Append sha1hash to the end of the file
                    await fileStreamWriter.WriteAsync(hash); //20
                    fileStreamWriter.Flush(); // Ensure it's written
                }
            }
            */
        }

        //Attempts to load the .mzk file into the structure.
        public void ParseMizuki()
        {
            _mzkContent = new MizukiFileContent();

            using (FileStream fileStreamR = _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] mzkHeader = new byte[3];
                fileStreamR.Read(mzkHeader, 0, mzkHeader.Length);
                string header = Encoding.UTF8.GetString(mzkHeader);
                if (header != "MZK")
                {
                    throw new Exception("Invalid file format");
                }

                byte[] fileInstanceIDBytes = new byte[16];
                fileStreamR.Read(fileInstanceIDBytes, 0, fileInstanceIDBytes.Length);
                _mzkContent.FileInstanceID = new Guid(fileInstanceIDBytes);

                byte[] ShaGUID = new byte[20];
                fileStreamR.Read(ShaGUID, 0, ShaGUID.Length);
                if (!ShaGUID.SequenceEqual(_mzkContent.ShaGUID))
                {
                    throw new Exception("fileInstance Sha1 mismatch.");
                }


                byte[] countBytes = new byte[2];
                fileStreamR.Read(countBytes, 0, countBytes.Length);
                _mzkContent.PeerCount = BitConverter.ToInt16(countBytes);

                byte[] thresholdBytes = new byte[2];
                fileStreamR.Read(thresholdBytes, 0, thresholdBytes.Length);
                _mzkContent.Threshold = BitConverter.ToInt16(thresholdBytes);

                byte[] shareLengthBytes = new byte[4];
                fileStreamR.Read(shareLengthBytes, 0, shareLengthBytes.Length);
                _mzkContent.ShareLength = BitConverter.ToInt32(shareLengthBytes, 0);

                byte[] encryptedSharesBytes = new byte[_mzkContent.ShareLength * _mzkContent.PeerCount];
                fileStreamR.Read(encryptedSharesBytes, 0, (_mzkContent.ShareLength * _mzkContent.PeerCount));
                _mzkContent.EncryptedShares = encryptedSharesBytes;

                byte[] dataLengthBytes = new byte[4];
                fileStreamR.Read(dataLengthBytes, 0, dataLengthBytes.Length);
                _mzkContent.EncryptedDataLength = BitConverter.ToInt32(dataLengthBytes, 0);

                byte[] encryptedDataBytes = new byte[_mzkContent.EncryptedDataLength];
                fileStreamR.Read(encryptedDataBytes, 0, _mzkContent.EncryptedDataLength);
                _mzkContent.EncryptedData = encryptedDataBytes;
            }
        }

        public Tuple<Dictionary<Guid, byte[]>, MizukuiShareContent> AcquireShares()
        {
            List<byte[]> sharesList = new List<byte[]>();
            List<Guid> shareGuid = new List<Guid>();
            bool decryptedAtLeastOne = false;
            MizukuiShareContent myShare = null;

            for (int offset = 0; offset < _mzkContent.EncryptedShares.Length; offset += _mzkContent.ShareLength)
            {
                byte[] encryptedSingularShare = new byte[2048];
                Buffer.BlockCopy(_mzkContent.EncryptedShares, offset, encryptedSingularShare, 0, _mzkContent.ShareLength);

                byte[] decryptedSingularShare = EnderChest.controller.Certificate.DecryptWithMyCert(encryptedSingularShare);

                if (decryptedSingularShare != null)
                {
                    
                    decryptedAtLeastOne = true;
                    byte[] peerCount = new byte[2];
                    Array.Copy(decryptedSingularShare, 0, peerCount, 0, 2);

                    if (peerCount.SequenceEqual(BitConverter.GetBytes(_mzkContent.PeerCount)))
                    {
                        byte[] guid = new byte[16];
                        int guidOffset;
                        if (offset == 0) guidOffset = offset + 2;
                        else guidOffset = 16 * (offset / _mzkContent.ShareLength) + 2;

                        Array.Copy(decryptedSingularShare, guidOffset, guid, 0, 16);
                        if (guid.SequenceEqual(EnderChest.controller.Certificate.UUID.ToByteArray()))
                        {
                            Console.WriteLine("My own share");
                            sharesList.Add(decryptedSingularShare);
                        }

                        byte[] uuidStacked = new byte[0];
                        for (int guidOffset2 = 2; guidOffset2 < (16 * _mzkContent.PeerCount + 2); guidOffset2 += 16)
                        {
                            byte[] guid2 = new byte[16];
                            AppendData(uuidStacked, guid2);
                            Array.Copy(decryptedSingularShare, guidOffset2, guid2, 0, 16);
                            Guid Guid2 = new Guid(guid2);
                            shareGuid.Add(Guid2);
                        }

                        int myNTRUContentOffset = (2 + _mzkContent.PeerCount * 16);
                        byte[] myEncryptedNTRUContent = new byte[decryptedSingularShare.Length - myNTRUContentOffset];
                        Array.Copy(decryptedSingularShare, myNTRUContentOffset, myEncryptedNTRUContent, 0, decryptedSingularShare.Length - myNTRUContentOffset);

                        myShare = new MizukuiShareContent(_mzkContent.PeerCount, uuidStacked, myEncryptedNTRUContent);
                    }
                }
                else sharesList.Add(encryptedSingularShare);
            }

            if (decryptedAtLeastOne)
            {
                return Tuple.Create(shareGuid.Zip(sharesList, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v), myShare);
            }


            Dictionary<Guid, byte[]> nullDict = new Dictionary<Guid, byte[]>();
            nullDict = null;
            MizukuiShareContent nullMZK = new MizukuiShareContent();
            nullMZK = null;
            return Tuple.Create(nullDict, nullMZK);
        }

        public async Task<byte[]> ReadFile()
        {
            using (FileStream fileStreamR = _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long size = _fileInfo.Length;
                byte[] wholeFile = new byte[size];
                await fileStreamR.ReadAsync(wholeFile);
                return wholeFile;
            }
        }
    }
}
