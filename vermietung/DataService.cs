using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CarRentalHttpServer
{
	public static class DataService
	{
		private static readonly string BasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\assets"));

		public static readonly string UsersFilePath = Path.Combine(BasePath, "users.csv");
		public static readonly string CarsFilePath = Path.Combine(BasePath, "cars.csv");
		public static readonly string RentalsFilePath = Path.Combine(BasePath, "rentals.csv");
		private const string DateFormat = "MM/dd/yyyy";
		private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

		public static List<Car> LoadCars(List<Rental> rentals)
		{
			List<Car> loadedCars;
			
			if (!File.Exists(CarsFilePath))
			{
				loadedCars = GetDefaultCars();
			}
			else
			{
				var lines = File.ReadAllLines(CarsFilePath);
				loadedCars = new List<Car>();

				foreach (var line in lines.Skip(1))
				{
					var values = line.Split(',');
					loadedCars.Add(new Car
					               {
					               	Id = int.Parse(values[0]),
					               	Brand = values[1],
					               	Model = values[2],
					               	DailyPrice = decimal.Parse(values[3], CultureInfo.InvariantCulture),
					               	Available = bool.Parse(values[4])
					               });
				}
			}

			// Populate booked dates from rentals
			foreach (var car in loadedCars)
			{
				var carRentals = rentals.Where(r => r.CarId == car.Id);
				foreach (var rental in carRentals)
				{
					car.BookedDates.AddRange(GetDateRange(rental.StartDate, rental.EndDate));
				}
				
				// Update availability status
				car.Available = !rentals.Any(r =>
				                             r.CarId == car.Id &&
				                             r.StartDate.Date <= DateTime.Today &&
				                             r.EndDate.Date >= DateTime.Today
				                            );
				// Set upcoming rental date (für Anzeige "ab xx.xx. reserviert")
				var upcomingRental = rentals
					.Where(r => r.CarId == car.Id && r.StartDate > DateTime.Today)
					.OrderBy(r => r.StartDate)
					.FirstOrDefault();

				if (upcomingRental != null)
				{
					car.UpcomingRentalDate = upcomingRental.StartDate;
				}
			}
			return loadedCars;
		}

		public static List<Rental> LoadRentals()
		{
			if (!File.Exists(RentalsFilePath))
				return new List<Rental>();

			var lines = File.ReadAllLines(RentalsFilePath);
			var rentals = new List<Rental>();

			foreach (var line in lines.Skip(1))
			{
				var values = line.Split(',');
				rentals.Add(new Rental
				            {
				            	Id = int.Parse(values[0]),
				            	CarId = int.Parse(values[1]),
				            	Customer = values[2],
				            	StartDate = DateTime.ParseExact(values[3], DateFormat, Culture),
				            	EndDate = DateTime.ParseExact(values[4], DateFormat, Culture),
				            	TotalPrice = decimal.Parse(values[5], Culture)
				            });
			}
			return rentals;
		}

		public static void SaveCars(List<Car> cars)
		{
			var lines = new List<string> { "Id,Brand,Model,DailyPrice,Available" };
			lines.AddRange(cars.Select(c =>
			                           string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}",
			                                         c.Id, c.Brand, c.Model, c.DailyPrice, c.Available)));

			File.WriteAllLines(CarsFilePath, lines);
		}

		public static void SaveRentals(List<Rental> rentals)
		{
			var lines = new List<string> { "Id,CarId,Customer,StartDate,EndDate,TotalPrice" };
			lines.AddRange(rentals.Select(r =>
			                              string.Format(Culture, "{0},{1},{2},{3},{4},{5}",
			                                            r.Id,
			                                            r.CarId,
			                                            r.Customer,
			                                            r.StartDate.ToString(DateFormat, Culture),
			                                            r.EndDate.ToString(DateFormat, Culture),
			                                            r.TotalPrice)));

			File.WriteAllLines(RentalsFilePath, lines);
		}
		
		public static List<User> LoadUsers()
		{
			var users = new List<User>();
			if (!File.Exists(UsersFilePath)) return users;

			var lines = File.ReadAllLines(UsersFilePath);
			foreach (var line in lines.Skip(1)) // Erste Zeile = Header
			{
				var parts = line.Split(';');
				if (parts.Length >= 8)
				{
					users.Add(new User
					          {
					          	Name = parts[0],
					          	Nachname = parts [1],
					          	Email = parts[2],
					          	Salt = parts[3],
					          	PasswordHash = parts[4],
					          	Role = parts[5],
					          	SecurityQuestion = parts[6],
					          	SecurityAnswer = parts[7]
					          });
				}
			}
			return users;
		}

		public static void SaveUsers(List<User> users)
		{
			var lines = new List<string> { "Name;Nachname;Email;Salt;PasswordHash;Role;SecurityQuestion;SecurityAnswer" };
			lines.AddRange(users.Select(u =>
			                            string.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
			                                          u.Name, u.Nachname, u.Email, u.Salt, u.PasswordHash, u.Role, u.SecurityQuestion, u.SecurityAnswer)));

			File.WriteAllLines(UsersFilePath, lines);
		}

		private static List<Car> GetDefaultCars()
		{
			return new List<Car>
			{
				new Car { Id = 1, Brand = "Audi", Model = "A4", DailyPrice = 59.99m, Available = true },
				new Car { Id = 2, Brand = "VW", Model = "Passat", DailyPrice = 69.99m, Available = true },
				new Car { Id = 3, Brand = "BMW", Model = "3er", DailyPrice = 49.99m, Available = true },
			};
		}

		private static IEnumerable<DateTime> GetDateRange(DateTime start, DateTime end)
		{
			for (var date = start; date <= end; date = date.AddDays(1))
			{
				yield return date;
			}
		}
	}
}