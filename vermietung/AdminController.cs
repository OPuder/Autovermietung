/*
 * Created by SharpDevelop.
 * User: puder
 * Date: 28.03.2025
 * Time: 11:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System;
using System.Text;

namespace CarRentalHttpServer
{
	public static class AdminController
	{
		public static string HandleAdminPage(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, User> sessions)
		{
			var sessionCookie = request.Cookies["SessionId"];
			if (sessionCookie == null || !sessions.ContainsKey(sessionCookie.Value))
			{
				response.StatusCode = 401; // Unauthorized
				return "<h3>Kein Zugriff</h3><p>Bitte zuerst <a href='/login'>einloggen</a>.</p>";
			}

			var user = sessions[sessionCookie.Value];
			if (user.Role != "Admin")
			{
				response.StatusCode = 403; // Forbidden
				return "<h3>Zugriff verweigert</h3><p>Nur Administratoren dürfen diese Seite sehen.</p>";
			}

			return @"<h2>Admin-Bereich</h2>
				<p>Willkommen, Admin <strong>" + user.Name + @"</strong>!</p>
					<ul>
    			<li><a href='/cars'>Fahrzeuge verwalten</a></li>
    			<li><a href='/rentals'>Vermietungen einsehen</a></li>
    			<li><a href='/admin/users'>Benutzerverwaltung</a></li>
					</ul>";
		}
		
		public static string HandleUserManagementPage(HttpListenerRequest request, User currentUser, List<User> users)
		{
			if (currentUser == null || currentUser.Role != "Admin")
				return "<h3>Kein Zugriff</h3>";

			return HtmlGenerator.GenerateUserManagementPage(users, currentUser);
		}
		
		public static void HandleUserAdd(HttpListenerRequest request, HttpListenerResponse response, List<User> users)
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
				string role = formData["role"];
				string question = formData["securityQuestion"];
				string answer = formData["securityAnswer"];

				string error = null;

				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
				{
					error = "Bitte fülle alle Pflichtfelder aus.";
				}
				else if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
				{
					error = "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.";
				}

				if (error != null)
				{
					string errorHtml = string.Format(@"
				<div class='error'>
					<h3>Fehler</h3>
					<p>{0}</p>
					<a class='btn' href='/admin/users'>Zurück</a>
				</div>", error);

					string fullHtml = HtmlGenerator.GenerateStyledHtml("Fehler beim Erstellen", errorHtml, null);

					byte[] buffer = Encoding.UTF8.GetBytes(fullHtml);
					response.ContentLength64 = buffer.Length;
					response.ContentType = "text/html";
					response.OutputStream.Write(buffer, 0, buffer.Length);
					response.Close();
					return;
				}

				string salt = SecurityHelper.GenerateSalt();
				string hash = SecurityHelper.HashPassword(password, salt);

				var newUser = new User
				{
					Name = name,
					Nachname = nachname,
					Email = email,
					Salt = salt,
					PasswordHash = hash,
					Role = role,
					SecurityQuestion = question,
					SecurityAnswer = answer
				};

				users.Add(newUser);
				DataService.SaveUsers(users);
				response.Redirect("/admin/users");
				response.Close();
			}
		}

		public static void HandleUserDelete(HttpListenerRequest request, HttpListenerResponse response, List<User> users)
		{
			var email = ReadFormValue(request, "email");
			var user = users.FirstOrDefault(u => u.Email == email);
			if (user != null)
			{
				users.Remove(user);
				DataService.SaveUsers(users);
			}
			response.Redirect("/admin/users");
			response.Close();
		}
		
		public static void HandleUserRoleChange(HttpListenerRequest request, HttpListenerResponse response, List<User> users)
		{
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				string body = reader.ReadToEnd();

				var formData = new NameValueCollection();
				foreach (var pair in body.Split('&'))
				{
					var kv = pair.Split('=');
					if (kv.Length == 2)
						formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
				}

				string email = formData["email"];
				string newRole = formData["role"];

				var user = users.FirstOrDefault(u => u.Email == email);
				if (user != null && (newRole == "Admin" || newRole == "User" || newRole == "Ban"))
				{
					user.Role = newRole;
					DataService.SaveUsers(users);
				}
				else
				{
					Console.WriteLine("? Benutzer nicht gefunden oder ungültige Rolle.");
				}
			}

			response.Redirect("/admin/users");
			response.Close();
		}

		public static void HandlePasswordChange(HttpListenerRequest request, HttpListenerResponse response, List<User> users)
		{
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				string body = reader.ReadToEnd();
				var formData = new NameValueCollection();

				foreach (var pair in body.Split('&'))
				{
					var kv = pair.Split('=');
					if (kv.Length == 2)
						formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
				}

				string email = formData["email"];
				string newPassword = formData["newPassword"];

				var user = users.FirstOrDefault(u => u.Email == email);
				if (user != null && !string.IsNullOrWhiteSpace(newPassword))
				{
					string salt = SecurityHelper.GenerateSalt();
					user.Salt = salt;
					user.PasswordHash = SecurityHelper.HashPassword(newPassword, salt);
					DataService.SaveUsers(users);
				}
			}

			response.Redirect("/admin/users");
			response.Close();
		}

		private static string ReadFormValue(HttpListenerRequest request, string key)
		{
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				string body = reader.ReadToEnd();
				var formData = new NameValueCollection();
				foreach (var pair in body.Split('&'))
				{
					var kv = pair.Split('=');
					if (kv.Length == 2)
						formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
				}
				return formData[key];
			}
		}
	}
}

