# TheSpellbook
SignalR project developed in ASP.NET MVC as a social media platform.

Sign Up new users and Log In existing users maintained in a database.
Features:
- User walls and profile pictures
- Add friends and see their recent wall posts on your profile
- Send private messages to friends
- Real-time chat messaging service for chatting with friends in real-time
- Notifications of new messages and pending chat windows are delivered in real time without the need for page refreshes.

WARNING: User information is stored in plain text at this time and user inputs are not sanitized. THIS IS NOT SAFE TO DEPLOY IN THE WILD.
         I developed this specifically for exploring SignalR and have released it without encryption for user data or protections against
         SQL injection. I will release an update that includes these shortly.
