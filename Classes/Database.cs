using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using static System.Net.Mime.MediaTypeNames;

namespace Mizuki.Classes
{
    internal class Database
    {
        private Guid _myUUID;
        private string _dbFilePath;
        private string _connString;

        public Database(Guid myUuid,string password)
        {
            _myUUID = myUuid;
            _dbFilePath = $"{myUuid.ToString()}.db";
            _connString = new SqliteConnectionStringBuilder
            {
                DataSource = $"{myUuid.ToString()}.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = password
            }.ToString();

            this.CreateDatabaseFile();
        }

        private void CreateDatabaseFile()
        {
            if (File.Exists(_dbFilePath)) 
            {
                return;
            }
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"CREATE TABLE ""FileList""(
                    ""instanceID""  TEXT NOT NULL,
                    ""encryptedNRTUKey""    TEXT,
                    ""fileName""    TEXT);
                    CREATE TABLE ""FriendList"" (
                	""friendUUID""	TEXT NOT NULL,
	                ""friendName""  TEXT NOT NULL,
                    ""friendPublicKey"" TEXT,
                    ""friendIPAddress"" TEXT
                    );";
                command.ExecuteNonQuery();
            }
        }

        private static string SafeGetString(SqliteDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
            {
                return reader.GetString(colIndex);
            }
            return string.Empty;
        }

        /// 
        ///     DATABASE FUNCTIONS FOR FILE-TABLE
        /// 

        public List<string> FindFileByInstanceID(Guid instanceID) 
        { 
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM FileList WHERE instanceID = $instanceID;";
                command.Parameters.AddWithValue("$instanceID", instanceID.ToString());

                using (var reader = command.ExecuteReader()) 
                { 
                    while (reader.Read()) 
                    { 
                        var instanceIDRead = SafeGetString(reader, 0);
                        var encryptedNTRUKeyRead = SafeGetString(reader, 1);
                        var fileNameRead = SafeGetString(reader, 2);

                        return new List<string> {instanceIDRead, encryptedNTRUKeyRead, fileNameRead};
                    }
                }
            }   
            return null;
        }

        public void InsertFile(Guid instanceID, string encryptedNTRUKey, string fileName = null)
        {
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"INSERT INTO FileList (instanceID, encryptedNRTUKey, fileName)
                      values ($instanceID, $encryptedNTRUKey, $fileName);
                    ";
                command.Parameters.AddWithValue("$instanceID", instanceID.ToString());
                command.Parameters.AddWithValue("$encryptedNTRUKey", encryptedNTRUKey);
                if (fileName == null) { fileName = string.Empty; }
                command.Parameters.AddWithValue("$fileName", fileName);
                command.ExecuteNonQuery();
            }
        }

        /// 
        ///     DATABASE FUNCTIONS FOR FRIEND-TABLE
        /// 

        public List<List<string>> GetFriends()
        {
            List<List<string>> friendsList = new List<List<string>>();
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM FriendList";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var friendUUIDRead = SafeGetString(reader, 0);
                        var friendNameRead = SafeGetString(reader, 1);
                        var friendPublicKeyRead = SafeGetString(reader, 2);
                        var friendIPAddressRead = SafeGetString(reader, 3);

                        friendsList.Add(new List<string>() { friendUUIDRead, friendNameRead, friendPublicKeyRead, friendIPAddressRead});
                    }
                }
            }
            return friendsList;
        }

        public List<string> GetFriendFromGuid(Guid friendUUID)
        {
            List<string> friend = null;
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM FriendList WHERE friendUUID = $friendGUID";
                command.Parameters.AddWithValue("friendGUID", friendUUID.ToString());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var friendUUIDRead = SafeGetString(reader, 0);
                        var friendNameRead = SafeGetString(reader, 1);
                        var friendPublicKeyRead = SafeGetString(reader, 2);
                        var friendIPAddressRead = SafeGetString(reader, 3);

                        friend = new List<string>() { friendUUIDRead, friendNameRead, friendPublicKeyRead, friendIPAddressRead}; 
                    }
                }
            }
            return friend;
        }

        public void InsertFriend(string friendUUID, string friendName= null, string friendPublicKey = null, string friendIPAddress = null)
        {   
            if (GetFriendFromGuid(new Guid(friendUUID)) == null)
            {
                using (var connection = new SqliteConnection(_connString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        @"INSERT INTO FriendList (friendUUID, friendName, friendPublicKey, friendIPAddress)
                      values ($friendUUID, $friendName, $friendPublicKey, $friendIPAddress);
                    ";
                    command.Parameters.AddWithValue("$friendUUID", friendUUID);
                    if (friendName == null) { friendName = string.Empty; }
                    command.Parameters.AddWithValue("$friendName", friendName);
                    if (friendPublicKey == null) { friendPublicKey = string.Empty; }
                    command.Parameters.AddWithValue("$friendPublicKey", friendPublicKey);
                    if (friendIPAddress == null) { friendIPAddress = string.Empty; }
                    command.Parameters.AddWithValue("$friendIPAddress", friendIPAddress);
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                UpdateFriendWithUUID(new Guid(friendUUID), friendName, friendPublicKey, friendIPAddress);
            }
        }

        public X509Certificate2 GetCertFromUUID(Guid uuid)
        {
            using (var connection = new SqliteConnection(_connString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM FriendList WHERE friendUUID = $friendUUID;";
                command.Parameters.AddWithValue("$friendUUID", uuid.ToString());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var friendUUIDRead = SafeGetString(reader, 0);
                        var friendNameRead = SafeGetString(reader, 1);
                        var friendPublicKeyRead = SafeGetString(reader, 2);
                        var friendIPAddressRead = SafeGetString(reader, 3);

                        try
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("-----BEGIN CERTIFICATE-----");
                            sb.AppendLine(friendPublicKeyRead);
                            sb.AppendLine("-----BEGIN CERTIFICATE-----");
                            X509Certificate2 cert = X509Certificate2.CreateFromPem(sb.ToString());
                            return cert;
                        }
                        catch (Exception ex)
                        {
                            return null;
                        }
                        
                    }
                }
            }
            return null;
        }

        public void UpdateFriendWithUUID(Guid friendUUID, string friendName = null, string friendPublicKey = null, string friendIPAddress = null)
        {
            List<string> existingFriendInfo = GetFriendFromGuid(friendUUID);
            if (existingFriendInfo != null)
            {
                using (var connection = new SqliteConnection(_connString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"UPDATE    FriendList
                        SET     friendName = $friendName, 
                                friendPublicKey = $friendPublicKey,
                                friendIPAddress = $friendIPAddress
                        WHERE   friendUUID = $friendUUID;
                    ";
                    if (friendName == null) { friendName = existingFriendInfo[1]; }
                    command.Parameters.AddWithValue("$friendName", friendName);
                    if (friendPublicKey == null) { friendPublicKey = existingFriendInfo[2]; }
                    command.Parameters.AddWithValue("$friendPublicKey", friendPublicKey);
                    if (friendIPAddress == null) { friendIPAddress = existingFriendInfo[3]; }
                    command.Parameters.AddWithValue("$friendIPAddress", friendIPAddress);
                    command.Parameters.AddWithValue("$friendUUID", friendUUID.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteFriendWithUUID(Guid friendUUID)
        {
            List<string> existingFriendInfo = GetFriendFromGuid(friendUUID);
            if (existingFriendInfo != null)
            {
                using (var connection = new SqliteConnection(_connString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"DELETE FROM FriendList
                      WHERE friendUUID = $friendUUID;
                    ";
                    command.Parameters.AddWithValue("$friendUUID", friendUUID.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

