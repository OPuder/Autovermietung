using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CarRentalHttpServer
{
	public static class AuthController
	{
		public static string HandleRegister(HttpListenerRequest request, out bool isPost, List<User> users)
		{
			isPost = request.HttpMethod == "POST";
			if (isPost)
			{
				using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					var body = reader.ReadToEnd();
					var formData = new NameValueCollection();
					foreach (var pair in body.Split('&'))
					{
						var kv = pair.Split('=');
						if (kv.Length == 2)
							formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
					}

					string name = formData["name"];
					string nachname = formData["nachname"];
					string email = formData["email"];
					string password = formData["password"];
					string passwordRepeat = formData["passwordRepeat"];

					string securityQuestion = formData["securityQuestion"].Replace('+', ' ');
					string securityAnswer = formData["securityAnswer"];

					if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
					{
						return "<h3>E-Mail bereits registriert!</h3><a href='/login'>Zum Login</a>";
					}
					if (password.Length < 6)
					{
						return string.Format(
							@"<h3>Passwort muss mindestens 6 Zeichen lang sein.</h3>
        					<a href='/register?name={0}&email={1}'>Zurück</a>",
							Uri.EscapeDataString(name),
							Uri.EscapeDataString(email));
					}
					if (password != passwordRepeat)
					{
						return string.Format(
							@"<h3>Passwörter stimmen nicht überein!</h3>
        					<a href='/register?name={0}&email={1}'>Zurück</a>",
							Uri.EscapeDataString(name),
							Uri.EscapeDataString(email));
					}
					else
					{
						string salt = SecurityHelper.GenerateSalt();
						string hash = SecurityHelper.HashPassword(password, salt);
						var user = new User
						{
							Name = name,
							Nachname = nachname,
							Email = email,
							Salt = salt,
							PasswordHash = hash,
							Role = "User",
							SecurityQuestion = securityQuestion,
							SecurityAnswer = securityAnswer
						};
						users.Add(user);
						DataService.SaveUsers(users);
						return @"
						<!DOCTYPE html>
							<html>
							<head>
    							<meta http-equiv='refresh' content='1;url=/login' />
    								<title>Registrierung erfolgreich</title>
							</head>
							<body>
    								<h3>Registrierung erfolgreich!</h3>
    								<p>Du wirst automatisch weitergeleitet...</p>
							</body>
						</html>";
					}
				}
			}
			else
			{
				return @"<h2>Registrieren</h2>
					<form method='post' action='/register'>
    					<div class='form-group'>
        					<label>Name</label>
        					<input type='text' name='name' required />
    					</div>
        				<div class='form-group'>
        					<label>Nachname</label>
        				<input type='text' name='nachname' required />
    					</div>
    					<div class='form-group'>
        					<label>E-Mail</label>
        					<input type='email' name='email' required onblur='checkEmailExists()'/>
        					<div id='emailFeedback' style='color:red; font-size:0.9em;'></div>
    					</div>
    						<div class='form-group'>
        					<label>Passwort</label>
        					<input type='password' name='password' id='password' required minlength='6' required oninput='validatePassword()'/>
    					</div>
    					<div class='form-group'>
							<label>Passwort wiederholen</label>
							<input type='password' name='passwordRepeat' id='passwordRepeat' required minlength='6' oninput='validatePassword()' />
							<div id='passwordMessage' style='color:red; font-size:0.9em; margin-top:5px;'></div>
						</div>
						<div class='form-group'>
    						<label>Sicherheitsfrage</label>
    						<select name='securityQuestion' required>
        							<option value='Was war der Name deiner ersten Grundschule?'>Was war der Name deiner ersten Grundschule?</option>
        							<option value='Wie hieß dein erstes Haustier?'>Wie hieß dein erstes Haustier?</option>
        							<option value='Wo bist du geboren?'>Wo bist du geboren?</option>
    						</select>
						</div>
						<div class='form-group'>
    						<label>Antwort</label>
    						<input type='text' name='securityAnswer' required />
						</div>
    						<button class='btn' type='submit'>Registrieren</button>
					</form>
					
<script>
    function validatePassword() {
        var pw = document.getElementById('password').value;
        var pwRepeat = document.getElementById('passwordRepeat').value;
        var msg = document.getElementById('passwordMessage');

        if (pw.length < 6) {
            msg.innerText = 'Das Passwort muss mindestens 6 Zeichen lang sein.';
        } else if (pw !== pwRepeat) {
            msg.innerText = 'Die Passwörter stimmen nicht überein.';
        } else {
            msg.innerText = '';
        }
    }
    </script>";
			}
		}
		
		public static string HandleLogin(HttpListenerRequest request, HttpListenerResponse response, out bool isPost, List<User> users, Dictionary<string, User> sessions)
		{
			isPost = request.HttpMethod == "POST";
			if (isPost)
			{
				using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					var body = reader.ReadToEnd();
					var formData = new NameValueCollection();
					foreach (var pair in body.Split('&'))
					{
						var kv = pair.Split('=');
						if (kv.Length == 2)
							formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
					}

					string email = formData["email"];
					string password = formData["password"];

					var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
					if (user == null || SecurityHelper.HashPassword(password, user.Salt) != user.PasswordHash)
					{
						return "<h3>Login fehlgeschlagen!</h3><a href='/login'>Erneut versuchen</a>";
					}
					else if (user.Role == "Ban")
					{
						return "<h3>Dein Konto wurde gesperrt.</h3>" +
							"<p>Wende dich an den Support!</p>" +
							"<a href='/login'>Zurück</a>";
					}
					else
					{
						string sessionId = Guid.NewGuid().ToString();
						sessions[sessionId] = user;

						var cookie = new Cookie("SessionId", sessionId);
						cookie.Path = "/";
						response.SetCookie(cookie);

						string redirectTarget = user.Role == "Admin" ? "/admin" : "/";

						string html = string.Format(@"
	<!DOCTYPE html>
	<html>
	<head>
		<meta http-equiv='refresh' content='1; url={2}' />
		<title>Willkommen</title>
	</head>
	<body>
		<h2>Willkommen, {0} {1}!</h2>
		<p>Du wirst weitergeleitet...</p>
	</body>
	</html>",
						                            user.Name,
						                            user.Nachname,
						                            redirectTarget
						                           );

						return html;
					}
				}
			}
			else
			{
				return @"<h2>Login</h2>
		<form method='post' action='/login'>
			<div class='form-group'>
				<label>E-Mail</label>
				<input type='email' name='email' required />
			</div>
			<div class='form-group'>
				<label>Passwort</label>
				<input type='password' name='password' required />
			</div>
			<button class='btn' type='submit'>Einloggen</button>
		</form>
		<p><a href='/forgot-password'>Passwort vergessen?</a></p>";
			}
		}
		
		public static string HandleLogout(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, User> sessions)
		{
			var sessionCookie = request.Cookies["SessionId"];
			if (sessionCookie != null && sessions.ContainsKey(sessionCookie.Value))
			{
				sessions.Remove(sessionCookie.Value);
			}
			var expiredCookie = new Cookie("SessionId", "")
			{
				Expires = System.DateTime.Now.AddDays(-1),
				Path = "/"
			};
			response.SetCookie(expiredCookie);
			
			string html = string.Format(@"
			<!DOCTYPE html>
				<html>
				<head>
    				<meta http-equiv='refresh' content='2; url=/Startseite' />
    				<title>Willkommen</title>
				</head>
				<body>
    				<h2>Du wurdest erfolgreich ausgeloggt!</h2>
    				<p>Du wirst gleich zur Startseite
geleitet...</p>
				</body>
				</html>");
			return html;
		}
	}
}
