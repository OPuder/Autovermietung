/*
 * Created by SharpDevelop.
 * User: aluca
 * Date: 03.04.2025
 * Time: 03:22
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
	public static class Router
	{
		public static bool TryHandleRequest(HttpListenerRequest request, HttpListenerResponse response,
		                                    User currentUser, List<Car> cars, List<Rental> rentals, List<User> users,
		                                    Dictionary<string, User> sessions, out string html, out bool responseHandled)
		{
			html = "";
			responseHandled = false;

			// Reihenfolge: Spezifischere Routen zuerst
			if (AuthRoutes.TryHandle(request, response, currentUser, sessions, users, out html, out responseHandled)) return true;
			if (request.Url.AbsolutePath == "/admin")
			{
				html = HtmlGenerator.GenerateStyledHtml("Admin", AdminController.HandleAdminPage(request, response, sessions), currentUser);
				return true;
			}
			if (AdminRoutes.TryHandle(request, response, currentUser, users, sessions, out html, out responseHandled)) return true;
			if (CarRoutes.TryHandle(request, response, currentUser, cars, out html, out responseHandled)) return true;
			if (RentalRoutes.TryHandle(request, response, currentUser, cars, rentals, out html)) return true;
			if (PasswordRoutes.TryHandle(request, response, currentUser, users, out html, out responseHandled)) return true;

			// Fallback → Startseite
			html = HtmlGenerator.GenerateStyledHtml("Startseite", HtmlGenerator.GenerateHomePage(cars, rentals, currentUser), currentUser);
			return true;
		}
	}
}

