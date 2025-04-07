using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;

namespace CarRentalHttpServer
{
	class Program
	{
		static List<Car> cars;
		static List<Rental> rentals;
		static List<User> users;
		static Dictionary<string, User> sessions = new Dictionary<string, User>();

		static void Main(string[] args)
		{
			users = DataService.LoadUsers();
			
			// Optional: Admin automatisch anlegen
			if (!users.Any(u => u.Role == "Admin"))
			{
				string email = "admin@admin.de";
				string pass = "admin123";
				string salt = SecurityHelper.GenerateSalt();
				string hash = SecurityHelper.HashPassword(pass, salt);

				users.Add(new User {
				          	Name = "Admin",
				          	Email = email,
				          	Salt = salt,
				          	PasswordHash = hash,
				          	Role = "Admin"
				          });
				DataService.SaveUsers(users);
				Console.WriteLine("Kein Adminuser gefunden.");
				Console.WriteLine("Admin erstellt: {0} / {1}", email, pass);
				Console.WriteLine("Bitte das Standardpasswort beim ersten Login ändern!");
				Console.WriteLine();
			}

			rentals = DataService.LoadRentals();
			cars = DataService.LoadCars(rentals);

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:8080/");

			try
			{
				listener.Start();
				Console.WriteLine("Server läuft auf http://localhost:8080/");
				// Console.WriteLine("TODO: Design überarbeiten");
				// Console.WriteLine("Rechnung verlinken anstatt fehler meldung ");
				Console.WriteLine("");

			}
			catch (HttpListenerException hlex)
			{
				Console.WriteLine("Fehler: {0}", hlex.Message);
				return;
			}
			
			while (true)
			{
				
				string html = "";
				var context = listener.GetContext();
				var request = context.Request;
				var response = context.Response;
				bool responseHandled = false;

				try
				{
					byte[] buffer = null;
					User currentUser = null;
					var sessionCookie = request.Cookies["SessionId"];
					if (sessionCookie != null && sessions.ContainsKey(sessionCookie.Value))
					{
						currentUser = sessions[sessionCookie.Value];
					}
					if (Router.TryHandleRequest(request, response, currentUser, cars, rentals, users, sessions, out html, out responseHandled))
					{
						if (!responseHandled)
						{
							buffer = Encoding.UTF8.GetBytes(html);
							response.ContentLength64 = buffer.Length;
							response.ContentType = "text/html";
							response.OutputStream.Write(buffer, 0, buffer.Length);
							response.OutputStream.Close();
						}
						continue;
					}

					buffer = Encoding.UTF8.GetBytes(html);
					response.ContentLength64 = buffer.Length;
					response.ContentType = "text/html";
					response.OutputStream.Write(buffer, 0, buffer.Length);
				}
				catch (Exception ex)
				{
					byte[] buffer = Encoding.UTF8.GetBytes("<h1>Fehler: " + ex.Message + "</h1>");
					response.ContentLength64 = buffer.Length;
					response.OutputStream.Write(buffer, 0, buffer.Length);
				}
				finally
				{
					if (!responseHandled)
					{
						try { response.OutputStream.Close(); } catch { }
					}
				}
			}
		}

		public static void ProcessRentalForm(HttpListenerRequest request, string customerEmail)
		{
			if (!request.HasEntityBody)
				throw new Exception("Keine Formulardaten erhalten");

			using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
			{
				var formData = new NameValueCollection();
				var queryString = reader.ReadToEnd();

				foreach (var pair in queryString.Split('&'))
				{
					var keyValue = pair.Split('=');
					if (keyValue.Length == 2)
					{
						formData[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
					}
				}

				int carId = int.Parse(formData["carId"]);
				string customer = customerEmail;
				DateTime rentalDate = DateTime.Parse(formData["startDate"]);
				int days = (int)(DateTime.Parse(formData["endDate"]) - DateTime.Parse(formData["startDate"])).TotalDays;

				var car = cars.FirstOrDefault(c => c.Id == carId);
				if (car == null)
					throw new Exception("Auto nicht gefunden");

				for (var date = rentalDate; date < rentalDate.AddDays(days); date = date.AddDays(1))
				{
					if (car.BookedDates.Contains(date.Date))
					{
						throw new Exception(string.Format("Das Auto ist am {0:dd.MM.yyyy} bereits gebucht", date));
					}
				}

				var rental = new Rental
				{
					Id = rentals.Count + 1,
					CarId = carId,
					Customer = customer,
					StartDate = rentalDate,
					EndDate = rentalDate.AddDays(days),
					TotalPrice = car.DailyPrice * days
				};

				for (var date = rentalDate; date <= rentalDate.AddDays(days); date = date.AddDays(1))
				{
					car.BookedDates.Add(date.Date);
				}
				
				car.Available = !car.BookedDates.Any(d => d >= DateTime.Today);
				rentals.Add(rental);
				DataService.SaveRentals(rentals);
				DataService.SaveCars(cars);
			}
		}
		
		static void HandleAddCar(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser == null || currentUser.Role != "Admin")
			{
				string html = HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser);
				byte[] buffer = Encoding.UTF8.GetBytes(html);
				response.ContentLength64 = buffer.Length;
				response.ContentType = "text/html";
				response.OutputStream.Write(buffer, 0, buffer.Length);
				return;
			}

			using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
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
					return;
				}
				else
				{
					string html = HtmlGenerator.GenerateStyledHtml("Ungültige Eingaben", "<h3>Bitte gültige Daten eingeben.</h3><a href='/cars'>Zurück</a>", currentUser);
					byte[] buffer = Encoding.UTF8.GetBytes(html);
					response.ContentLength64 = buffer.Length;
					response.ContentType = "text/html";
					response.OutputStream.Write(buffer, 0, buffer.Length);
				}
			}
		}
		
		static void HandleDeleteCar(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser == null || currentUser.Role != "Admin")
			{
				string html = HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser);
				byte[] buffer = Encoding.UTF8.GetBytes(html);
				response.ContentLength64 = buffer.Length;
				response.ContentType = "text/html";
				response.OutputStream.Write(buffer, 0, buffer.Length);
				return;
			}

			using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
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
		}
		
		static void HandleUserManagement(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser == null || currentUser.Role != "Admin")
			{
				ReturnHtml(response, HtmlGenerator.GenerateStyledHtml("Nicht erlaubt", "<h3>Keine Berechtigung</h3>", currentUser));
				return;
			}

			string html = HtmlGenerator.GenerateStyledHtml("Benutzerverwaltung", HtmlGenerator.GenerateUserManagementPage(users, currentUser), currentUser);
			ReturnHtml(response, html);
		}

		static void HandleChangeRole(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser.Role != "Admin")
			{
				response.StatusCode = 403;
				ReturnHtml(response, HtmlGenerator.GenerateStyledHtml("Zugriff verweigert", "<h3>Keine Berechtigung</h3>", currentUser));
				return;
			}

			var formData = ReadFormData(request);
			string email = formData["email"];
			string newRole = formData["role"];

			var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
			if (user != null && (newRole == "Admin" || newRole == "User"))
			{
				user.Role = newRole;
				DataService.SaveUsers(users);
			}
			response.Redirect("/admin-users");
			response.Close();
		}

		static void HandleDeleteUser(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser.Role != "Admin")
				return;

			var formData = ReadFormData(request);
			string email = formData["email"];
			var user = users.FirstOrDefault(u => u.Email == email);
			if (user != null)
			{
				users.Remove(user);
				DataService.SaveUsers(users);
			}
			response.Redirect("/admin-users");
			response.Close();
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
				string nachname = formData["nachname"]; // ⬅️ neu
				string email = formData["email"];
				string password = formData["password"];
				string role = formData["role"];

				if (!users.Any(u => u.Email == email))
				{
					string salt = SecurityHelper.GenerateSalt();
					string hash = SecurityHelper.HashPassword(password, salt);

					var newUser = new User
					{
						Name = name,
						Nachname = nachname, // ⬅️ neu
						Email = email,
						PasswordHash = hash,
						Salt = salt,
						Role = role
					};

					users.Add(newUser);
					DataService.SaveUsers(users);
				}
			}

			response.Redirect("/admin/users");
			response.Close();
		}

		static void HandleChangePassword(HttpListenerRequest request, HttpListenerResponse response, User currentUser)
		{
			if (currentUser.Role != "Admin")
				return;

			var formData = ReadFormData(request);
			string email = formData["email"];
			string newPassword = formData["newPassword"];

			var user = users.FirstOrDefault(u => u.Email == email);
			if (user != null && !string.IsNullOrWhiteSpace(newPassword))
			{
				string newSalt = SecurityHelper.GenerateSalt();
				string newHash = SecurityHelper.HashPassword(newPassword, newSalt);
				user.Salt = newSalt;
				user.PasswordHash = newHash;
				DataService.SaveUsers(users);
			}
			response.Redirect("/admin-users");
			response.Close();
		}

		static NameValueCollection ReadFormData(HttpListenerRequest request)
		{
			var formData = new NameValueCollection();
			using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
			{
				string body = reader.ReadToEnd();
				foreach (var pair in body.Split('&'))
				{
					var kv = pair.Split('=');
					if (kv.Length == 2)
						formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
				}
			}
			return formData;
		}

		static void ReturnHtml(HttpListenerResponse response, string html)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(html);
			response.ContentLength64 = buffer.Length;
			response.ContentType = "text/html";
			response.OutputStream.Write(buffer, 0, buffer.Length);
			response.OutputStream.Close();
		}

	}
}
