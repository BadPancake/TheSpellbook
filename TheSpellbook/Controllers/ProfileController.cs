using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure.MappingViews;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TheSpellbook.Models.Data;
using TheSpellbook.Models.ViewModels.Profile;

namespace TheSpellbook.Controllers
{
    public class ProfileController : Controller
    {
        // GET: /
        public ActionResult Index()
        {
            return View();
        }

        // POST: Profile/LiveSearch
        [HttpPost]
        public JsonResult LiveSearch(string searchVal)
        {
            //Initialize the database
            var db = new Db();

            //Create a list of search results
            var usernames = db.Users.Where(x => x.Username.Contains(searchVal) && x.Username != User.Identity.Name)
                .ToArray().Select(x => new LiveSearchUserVM(x)).ToList();

            //Return json
            return Json(usernames);
        }

        // POST: Profile/AddFriend
        [HttpPost]
        public void AddFriend(string friend)
        {
            //Initialize the database
            var db = new Db();

            //Get User's UserID
            var userDTO = db.Users.Where(x => x.Username.Equals(User.Identity.Name)).FirstOrDefault();
            var userId = userDTO.Id;

            //Get friend-to-be's ID
            var userDTO2 = db.Users.Where(x => x.Username.Equals(friend)).FirstOrDefault();
            var friendId = userDTO2.Id;

            //Add DTO
            var friendDTO = new FriendDTO();

            friendDTO.User1 = userId;
            friendDTO.User2 = friendId;
            friendDTO.Active = false;

            db.Friends.Add(friendDTO);

            db.SaveChanges();
        }

        //POST: Profile/DisplayFriendRequests
        [HttpPost]
        public JsonResult DisplayFriendRequests()
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.Where(x => x.Username.Equals(User.Identity.Name)).FirstOrDefault();
            var userId = userDTO.Id;

            //Create a list of friend requests
            var list = db.Friends.Where(x => x.User2 == userId && x.Active == false).ToArray()
                .Select(x => new FriendRequestVM(x)).ToList();

            //Initialize the list of involved users
            var users = new List<UserDTO>();

            foreach (var item in list)
            {
                var user = db.Users.Where(x => x.Id == item.User1).FirstOrDefault();
                users.Add(user);
            }

            //Return Json
            return Json(users);
        }

        // POST: Profile/AcceptFriendRequest
        [HttpPost]
        public void AcceptFriendRequest(int friendId)
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.Where(x => x.Username.Equals(User.Identity.Name)).FirstOrDefault();
            var userId = userDTO.Id;

            //Make friends
            var friendDTO = db.Friends.Where(x => x.User1 == friendId && x.User2 == userId).FirstOrDefault();
            friendDTO.Active = true;

            //Save changes to the database
            db.SaveChanges();
        }

        // POST: Profile/DeclineFriendRequest
        [HttpPost]
        public void DeclineFriendRequest(int friendId)
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(User.Identity.Name));
            var userId = userDTO.Id;

            //Delete the friend request
            var friendDTO = db.Friends.FirstOrDefault(x => x.User1 == friendId && x.User2 == userId);
            db.Friends.Remove(friendDTO);

            //Save changes to the database
            db.SaveChanges();
        }

        // POST: Profile/SendMessage
        [HttpPost]
        public void SendMessage(string friend, string message)
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(User.Identity.Name));
            var userId = userDTO.Id;

            //Get friend ID
            var userDTO2 = db.Users.FirstOrDefault(x => x.Username.Equals(friend));
            var userId2 = userDTO2.Id;

            //Save the message
            var dto = new MessageDTO();
            dto.From = userId;
            dto.To = userId2;
            dto.Message = message;
            dto.DateSent = DateTime.Now;
            dto.Read = false;

            db.Messages.Add(dto);
            db.SaveChanges();
        }

        // POST: Profile/DisplayUnreadMessages
        [HttpPost]
        public JsonResult DisplayUnreadMessages()
        {
            //Initialize the database
            var db = new Db();

            //Get user ID
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(User.Identity.Name));
            var userId = userDTO.Id;

            //Create a list of unread messages
            var list = db.Messages.Where(x => x.To == userId && x.Read == false).ToArray()
                .Select(x => new MessageVM(x)).ToList();

            //Mark read messages as read
            db.Messages.Where(x => x.To == userId && x.Read == false).ToList().ForEach(x => x.Read = true);
            db.SaveChanges();

            //Return Json
            return Json(list);
        }

        // POST: Profile/UpdateWallMessage
        [HttpPost]
        public void UpdateWallMessage(int id, string message)
        {
            //Initialize the database
            var db = new Db();

            //Update the wall
            var wall = db.Wall.Find(id);
            if (wall != null)
            {
                wall.Message = message;
                wall.DateEdited = DateTime.Now;
            }

            db.SaveChanges();

        }
    }
}