using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TheSpellbook.Models.Data;
using TheSpellbook.Models.ViewModels.Account;
using TheSpellbook.Models.ViewModels.Profile;

namespace TheSpellbook.Controllers
{
    public class AccountController : Controller
    {
        // GET: /
        public ActionResult Index()
        {
            // Confirm user is not logged in.

            var username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
                return Redirect("~/" + username);

            //Return view
            return View();
        }

        // POST: Account/CreateAccount
        [HttpPost]
        public ActionResult CreateAccount(UserVM model, HttpPostedFileBase file) 
        {
            //Initialize the database
            var db = new Db();

            // Check model state
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            //Ensure the proposed username is unique
            if (db.Users.Any(x => x.Username.Equals(model.Username)))
            {
                ModelState.AddModelError("", "Username " + model.Username + " is already in use.");
                model.Username = "";
                return View("Index", model);
            }

            //Create the UserDTO
            var userDTO = new UserDTO()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailAddress = model.EmailAddress,
                Username = model.Username,
                //Password is stored as plain text. Add encryption soon.
                Password = model.Password
            };

            //Add the new UserDTO to the DTO
            db.Users.Add(userDTO);

            //Save the new account information to the database
            db.SaveChanges();

            //Get newly-generated ID
            var userId = userDTO.Id;

            //Login the new user
            FormsAuthentication.SetAuthCookie(model.Username, false);

            //Set uploads directory for the new user
            var imageUploadsDir = new DirectoryInfo(string.Format("{0}ImageUploads", Server.MapPath(@"\")));

            //Check if image file was uploaded during creation
            if (file == null || file.ContentLength <= 0) return Redirect("~/" + model.Username);
            //Get the file extension of uploaded image
            var ext = file.ContentType.ToLower();

            //Verify the extension as acceptable
            if (ext !="image/jpg" &&
                ext != "image/jpeg" &&
                ext != "image/pjpeg" &&
                ext != "image/gif" &&
                ext != "image/png" &&
                ext != "image/x-png")
            {
                ModelState.AddModelError("", "The image was not uploaded, due to an unrecognized extension. Please upload a .jpg, .jpeg, .pjpeg, .png, .x-png, or .gif only.");
                return View("Index", model);
            }

            //Set the name of the uploaded image file
            var imageName = userId + ".jpg";

            //Set the path for the image
            var path = string.Format("{0}\\{1}", imageUploadsDir, imageName);

            //Save the image
            file.SaveAs(path);

            //Add to wall
            var wall = new WallDTO();
            wall.Id = userId;
            wall.Message = "";
            wall.DateEdited = DateTime.Now;
            db.Wall.Add(wall);
            db.SaveChanges();
            
            //Redirect
            return Redirect("~/" + model.Username);
        }

        // GET: /{username}
        [Authorize]
        public ActionResult Username(string username = "")
        {
            //Initialize the database
            var db = new Db();

            //Check if the User exists within the user database.
            if (! db.Users.Any(x => x.Username.Equals(username)))
            {
                return Redirect("~/");
            }

            //ViewBag Username
            ViewBag.Username = username;

            //Get the username of the user who is currently logged in
            var user = User.Identity.Name;

            //Viewbag user's full name
            var userDTO = db.Users.FirstOrDefault(x => x.Username.Equals(user));
            ViewBag.FullName = userDTO.FirstName + " " + userDTO.LastName;

            // Get user's id
            var userId = userDTO.Id;

            // ViewBag user id
            ViewBag.UserId = userId;

            //Get viewing full name
            var userDTO2 = db.Users.FirstOrDefault(x => x.Username.Equals(username));
            ViewBag.ViewingFullName = userDTO2.FirstName + " " + userDTO2.LastName;

            //Get User's Profile Picture
            ViewBag.ProfilePicture = userDTO2.Id + ".jpg";

            //Viewbag user type
            var userType = "guest";

            if (username.Equals(user))
                userType = "owner";

            ViewBag.UserType = userType;

            //Check if they are friends
            if (userType == "guest")
            {
                var u1 = db.Users.FirstOrDefault(x => x.Username.Equals(user));
                var id1 = u1.Id;

                var u2 = db.Users.FirstOrDefault(x => x.Username.Equals(username));
                var id2 = u2.Id;

                var f1 = db.Friends.FirstOrDefault(x => x.User1 == id1 && x.User2 == id2);
                var f2 = db.Friends.FirstOrDefault(x => x.User2 == id1 && x.User1 == id2);

                if (f1 == null && f2 == null)
                {
                    ViewBag.NotFriends = "True";
                }

                if (f1 != null)
                {
                    if (!f1.Active)
                    {
                        ViewBag.NotFriends = "Pending";
                    }
                }

                if (f2 != null)
                {
                    if (!f2.Active)
                    {
                        ViewBag.NotFriends = "Pending";
                    }
                }

            }

            //Get friend request count
            var friendCount = db.Friends.Count(x => x.User2 == userId && x.Active == false);
            if (friendCount > 0)
            {
                ViewBag.FRCount = friendCount;
            }

            //Get viewbag friend count
            var uDTO = db.Users.FirstOrDefault(x => x.Username.Equals(username));
            var usernameId = uDTO.Id;
            var friendCount2 = db.Friends.Count(x =>
                x.User2 == usernameId && x.Active == true || x.User1 == usernameId && x.Active == true);
            ViewBag.FCount = friendCount2;

            //Get viewbag message count
            var messageCount = db.Messages.Count(x => x.To == userId && x.Read == false);
            ViewBag.MsgCount = messageCount;

            //Get viewbag user wall
            var wall = new WallDTO();
            ViewBag.WallMessage = db.Wall.Where(x => x.Id == userId).Select(x => x.Message).FirstOrDefault();
            
            //ViewBag friend walls
            var friendIds1 = db.Friends.Where(x => x.User1 == userId && x.Active == true).ToArray()
                .Select(x => x.User2).ToList();

            var friendIds2 = db.Friends.Where(x => x.User2 == userId && x.Active == true).ToArray()
                .Select(x => x.User1).ToList();

            var allFriendIds = friendIds1.Concat(friendIds2).ToList();

            var walls = db.Wall.Where(x => allFriendIds.Contains(x.Id)).ToArray().OrderByDescending(x => x.DateEdited).Select(x => new WallVM(x)).ToList();

            ViewBag.Walls = walls;
            
            //Return
            return View();
        }

        //GET: Account/Logout
        [Authorize]
        public ActionResult Logout()
        {
            //Sign out
            FormsAuthentication.SignOut();

            //Redirect
            return Redirect("~/");
        }

        public ActionResult LoginPartial()
        {
            return PartialView();
        }

        //POST: Account/Login
        [HttpPost]
        public string Login(string username, string password)
        {
            //Initialize the database
            var db = new Db();

            //Check if the User exists within the user database.
            if (!db.Users.Any(x => x.Username.Equals(username) && x.Password.Equals(password))) return "problem";
            //Log in
            FormsAuthentication.SetAuthCookie(username, false);
            return "ok";
        }
    }
}