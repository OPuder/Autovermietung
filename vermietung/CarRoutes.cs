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
	public static class CarRoutes
	{
		public static bool TryHandle(HttpListenerRequest request, HttpListenerResponse response,User currentUser, List<Car> cars,out string html, out bool responseHandled)
		{
			html = "";
			responseHandled = false;
			
			if (request.Url.AbsolutePath == "/cars")
			{
				html = HtmlGenerator.GenerateStyledHtml("Fahrzeuge", HtmlGenerator.GenerateCarListPage(cars, currentUser), currentUser);
				return true;
			}
			else if (request.Url.AbsolutePath == "/add-car" && request.HttpMethod == "POST")
			{
				if (currentUser == null || currentUser.Role != "Admin")
				{
					html = HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser);
					return true;
				}

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

					string brand = formData["brand"];
					string model = formData["model"];
					string priceStr = formData["price"];

					decimal price;
					if (!string.IsNullOrWhiteSpace(brand) &&
					    !string.IsNullOrWhiteSpace(model) &&
					    decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price))
					{
						var newCar = new Car
						{
							Id = cars.Count > 0 ? cars.Max(c => c.Id) + 1 : 1,
							Brand = brand,
							Model = model,
							DailyPrice = price,
							Available = true,
							BookedDates = new List<DateTime>()
						};

						cars.Add(newCar);
						DataService.SaveCars(cars);
						response.Redirect("/cars");
						response.Close();
						return true;
					}
					else
					{
						html = HtmlGenerator.GenerateStyledHtml("Ungültige Eingaben", "<h3>Bitte gültige Daten eingeben.</h3><a href='/cars'>Zurück</a>", currentUser);
						return true;
					}
				}
			}
			else if (request.Url.AbsolutePath == "/delete-car" && request.HttpMethod == "POST")
			{
				if (currentUser == null || currentUser.Role != "Admin")
				{
					html = HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser);
					return true;
				}

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

					int id;
					if (formData["id"] != null && int.TryParse(formData["id"], out id))
					{
						var carToRemove = cars.FirstOrDefault(c => c.Id == id);
						if (carToRemove != null)
						{
							cars.Remove(carToRemove);
							DataService.SaveCars(cars);
						}
					}
				}

				response.Redirect("/cars");
				response.Close();
				return true;
			}

			return false;
		}
	}
}
