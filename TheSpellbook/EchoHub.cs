using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using TheSpellbook.Models.Data;

namespace TheSpellbook
{
    [HubName("echo")]
    public class EchoHub : Hub
    {
        public void Hello(string message)
        {
            //Clients.All.hello();
            Trace.WriteLine(message);

            //Set clients
            var clients = Clients.All;

            //Call jsfunction
            clients.test("This is a test.");
        }

        public void Notify(string friend)
        {
            //Initialize the database
            var db = new Db();

            //Get friend's ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(friend));
            var friendId = userDTO.Id;

            //Get friend request count
            var frCount = db.Friends.Count(x => x.User2 == friendId && x.Active == false);

            //Set clients
            var clients = Clients.Others;

            //Call the JS function
            clients.frnotify(friend, frCount);
        }

        public void GetFrcount()
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Get friend request count
            var friendReqCount = db.Friends.Count(x => x.User2 == userId && x.Active == false);

            //Set clients
            var clients = Clients.Caller;

            //Call the JS function
            clients.frcount(Context.User.Identity.Name, friendReqCount);
        }

        public void GetFcount(int friendId)
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Get friend count for user
            var friendCount1 = db.Friends.Count(x =>
                x.User2 == userId && x.Active == true || x.User1 == userId && x.Active == true);

            //Get user2 username
            var userDTO2 = db.Users.FirstOrDefault(x => x.Id == friendId);
            if (userDTO2 != null)
            {
                var username = userDTO2.Username;

                //Get friend count for user2
                var friendCount2 = db.Friends.Count(x =>
                    x.User2 == friendId && x.Active == true || x.User1 == friendId && x.Active == true);

                //Update chat
                UpdateChat();

                //Set clients
                var clients = Clients.All;

                //Call the JS function
                clients.fcount(Context.User.Identity.Name, username, friendCount1, friendCount2);
            }
        }

        public void NotifyOfMessage(string friend)
        {
            //Initialize the database
            var db = new Db();

            //Get friend ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(friend));
            var friendId = userDTO.Id;

            //Get message count
            var messageCount = db.Messages.Count(x => x.To == friendId && x.Read == false);

            //Set clients
            var clients = Clients.Others;

            //Call the JS function
            clients.msgcount(friend, messageCount);
        }

        public void NotifyOfMessageOwner()
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Get message count
            var messageCount = db.Messages.Count(x => x.To == userId && x.Read == false);

            //Set clients
            var clients = Clients.Caller;

            //Call the JS function
            clients.msgcount(Context.User.Identity.Name, messageCount);
        }

        public override Task OnConnected()
        {
            //Log user connection
            Trace.WriteLine("Here I am - " + Context.ConnectionId);

            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Get the connection ID
            var connectionId = Context.ConnectionId;

            //Add to OnlineDTO
            if (! db.Online.Any(x => x.Id == userId))
            {
                var online = new OnlineDTO();
                online.Id = userId;
                online.ConnectionId = connectionId;
                db.Online.Add(online);
                db.SaveChanges();
            }

            //Get all online IDs
            var onlineIds = db.Online.ToArray().Select(x => x.Id).ToList();

            //Get all friend IDs
            var friendIds1 = db.Friends.Where(x => x.User1 == userId && x.Active == true).ToArray()
                .Select(x => x.User2).ToList();

            var friendIds2 = db.Friends.Where(x => x.User2 == userId && x.Active == true).ToArray()
                .Select(x => x.User1).ToList();

            var allFriendIds = friendIds1.Concat(friendIds2).ToList();

            //Get final set of online friend IDs
            var resultList = onlineIds.Where((i) => allFriendIds.Contains(i)).ToList();

            //Create a dictionary of online friend IDs and usernames
            var dictFriends = new Dictionary<int, string>();
            foreach (var id in resultList)
            {
                var users = db.Users.Find(id);
                if (users != null)
                {
                    var friend = users.Username;

                    if (!dictFriends.ContainsKey(id))
                    {
                        dictFriends.Add(id, friend);
                    }
                }
            }

            var transformed = from key in dictFriends.Keys
                              select new {id = key, friend = dictFriends[key]};

            var json = JsonConvert.SerializeObject(transformed);

            //Set clients
            var clients = Clients.Caller;

            //Call JS function
            clients.getonlinefriends(Context.User.Identity.Name, json);

            //Update chat
            UpdateChat();

            //Return
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            //Log Disconnect
            Trace.WriteLine("Gone - " + Context.ConnectionId + " " + Context.User.Identity.Name);

            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Remove the disconnecter user from the DB
            if (!db.Online.Any(x => x.Id == userId)) return base.OnDisconnected(stopCalled);
            var online = db.Online.Find(userId);
            if (online != null) db.Online.Remove(online);
            db.SaveChanges();

            //Update chat
            UpdateChat();

            //Return
            return base.OnDisconnected(stopCalled);
        }

        public void UpdateChat()
        {
            //Initialize the database
            var db = new Db();

            //Get all online IDs
            var onlineIds = db.Online.ToArray().Select(x => x.Id).ToList();

            //Loop through onlineIds and get friends
            foreach (var userId in onlineIds)
            {
                //Get username
                var user = db.Users.Find(userId);
                var username = user.Username;

                //Get all friend IDs
                var friendIds1 = db.Friends.Where(x => x.User1 == userId && x.Active == true).ToArray()
                    .Select(x => x.User2).ToList();

                var friendIds2 = db.Friends.Where(x => x.User2 == userId && x.Active == true).ToArray()
                    .Select(x => x.User1).ToList();

                var allFriendIds = friendIds1.Concat(friendIds2).ToList();

                //Get final set of online friend IDs
                var resultList = onlineIds.Where((i) => allFriendIds.Contains(i)).ToList();

                //Create a dictionary of online friend IDs and usernames
                var dictFriends = new Dictionary<int, string>();
                foreach (var id in resultList)
                {
                    var users = db.Users.Find(id);
                    if (users != null)
                    {
                        var friend = users.Username;

                        if (!dictFriends.ContainsKey(id))
                        {
                            dictFriends.Add(id, friend);
                        }
                    }
                }

                var transformed = from key in dictFriends.Keys
                    select new { id = key, friend = dictFriends[key] };

                var json = JsonConvert.SerializeObject(transformed);

                //Set clients
                var clients = Clients.All;

                //Call JS function
                clients.updatechat(username, json);
            }
        }

        public void SendChat(int friendId, string friendUsername, string message)
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(Context.User.Identity.Name));
            var userId = userDTO.Id;

            //Set clients
            var clients = Clients.All;

            //Call JS function
            clients.sendchat(userId, Context.User.Identity.Name, friendId, friendUsername, message);
        }

    }
}