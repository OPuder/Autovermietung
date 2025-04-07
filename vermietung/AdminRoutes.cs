/*
 * Created by SharpDevelop.
 * User: aluca
 * Date: 03.04.2025
 * Time: 03:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace CarRentalHttpServer
{
	public static class AdminRoutes
	{
		public static bool TryHandle(HttpListenerRequest request, HttpListenerResponse response,
		                             User currentUser, List<User> users, Dictionary<string, User> sessions,
		                             out string html, out bool responseHandled)
		{
			html = "";
			responseHandled = false;

			if (request.Url.AbsolutePath == "/add-user" && request.HttpMethod == "POST")
			{
				AdminController.HandleUserAdd(request, response, users);
				responseHandled = true;
			}
			else if (request.Url.AbsolutePath == "/delete-user" && request.HttpMethod == "POST")
			{
				AdminController.HandleUserDelete(request, response, users);
				responseHandled = true;
			}
			else if (request.Url.AbsolutePath == "/change-role" && request.HttpMethod == "POST")
			{
				AdminController.HandleUserRoleChange(request, response, users);
				responseHandled = true;
			}
			else if (request.Url.AbsolutePath == "/change-password" && request.HttpMethod == "POST")
			{
				AdminController.HandlePasswordChange(request, response, users);
				responseHandled = true;
			}
			else if (request.Url.AbsolutePath == "/admin")
			{
				html = HtmlGenerator.GenerateStyledHtml("Admin", AdminController.HandleAdminPage(request, response, sessions), currentUser);
				responseHandled = false;
			}
			else if (request.Url.AbsolutePath == "/admin/users")
			{
				if (currentUser == null || currentUser.Role != "Admin")
				{
					html = HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser);
				}
				else
				{
					// Zur Sicherheit aktuelle User neu laden
					users = DataService.LoadUsers();

					html = HtmlGenerator.GenerateStyledHtml(
						"Benutzerverwaltung",
						AdminController.HandleUserManagementPage(request, currentUser, users),
						currentUser
					);
				}
				return true;
			}

			return responseHandled;
		}
	}
}