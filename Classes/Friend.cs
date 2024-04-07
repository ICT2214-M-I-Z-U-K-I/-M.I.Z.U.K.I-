using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Mizuki.Classes
{
    internal class Friend
    {
        public class Person
        {
            public Guid FriendUUID { get; set; }
            public String Name { get; set; }
            public X509Certificate2 FriendPublicKey { get; set; }
        }

        public Guid MyUUID { get; set; }
        public List<Person> FriendList { get; set; }
        public Friend(Guid myGuid)
        {
            MyUUID = myGuid;
            FriendList = new List<Person>();
        }


        public void CreateFriend(Person friendObject)
        {
            FriendList.Add(friendObject);
        }

        public Person ? FindFriendWithUUID(Guid friendUUID)
        {
            for (int i = 0; i < FriendList.Count; i++)
            {
                if (FriendList[i].FriendUUID == friendUUID)
                {
                    return FriendList[i];
                }
            }
            return null;
        }
    }
}
