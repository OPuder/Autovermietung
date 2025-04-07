/*
 * Created by SharpDevelop.
 * User: aluca
 * Date: 03.04.2025
 * Time: 03:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CarRentalHttpServer
{
	public static class AuthRoutes
	{
		public static bool TryHandle(HttpListenerRequest request, HttpListenerResponse response, User currentUser, Dictionary<string, User> sessions, List<User> users, out string html, out bool responseHandled)
		{
			html = "";
			responseHandled = false;
			bool isPost;
			byte[] buffer;

			if (request.Url.AbsolutePath == "/login")
			{
				var body = AuthController.HandleLogin(request, response, out isPost, users, sessions);
				html = HtmlGenerator.GenerateStyledHtml("Login", body, null);
				return true;
			}
			else if (request.Url.AbsolutePath == "/logout")
			{
				var body = AuthController.HandleLogout(request, response, sessions);
				html = HtmlGenerator.GenerateStyledHtml("Logout", body, null);
				return true;
			}
			else if (request.Url.AbsolutePath == "/register")
			{
				var body = AuthController.HandleRegister(request, out isPost, users);
				html = HtmlGenerator.GenerateStyledHtml("Registrieren", body, null);
				return true;
			}
			else if (request.Url.AbsolutePath == "/check-email" && request.HttpMethod == "POST")
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
					bool exists = users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

					string responseText = exists ? "taken" : "available";
					buffer = Encoding.UTF8.GetBytes(responseText);
					response.ContentLength64 = buffer.Length;
					response.ContentType = "text/plain";
					response.OutputStream.Write(buffer, 0, buffer.Length);
					response.OutputStream.Close();
					responseHandled = true;
					return true;
				}
			}

			return false;
		}
	}
}
