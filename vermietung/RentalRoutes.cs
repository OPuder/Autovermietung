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
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Specialized;
using System.Text;

namespace CarRentalHttpServer
{
    public static class RentalRoutes
    {
        public static bool TryHandle(HttpListenerRequest request, HttpListenerResponse response, User currentUser, List<Car> cars, List<Rental> rentals, out string html)
        {
            html = "";

            if (request.Url.AbsolutePath == "/rent")
            {
                if (currentUser == null)
                {
                    response.StatusCode = 401;
                    html = HtmlGenerator.GenerateStyledHtml("Nicht eingeloggt",
                        "<h3>Bitte zuerst <a href='/login'>Einloggen</a> / <a href='/register'>Registrieren</a>, um ein Auto zu mieten.</h3>", null);
                }
                else if (request.HttpMethod == "POST")
                {
                    ProcessRentalForm(request, currentUser.Email, cars, rentals);
                    html = HtmlGenerator.GenerateStyledHtml("Erfolg", HtmlGenerator.GenerateSuccessPage(), currentUser);
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Auto mieten", HtmlGenerator.GenerateRentalForm(cars, currentUser), currentUser);
                }
                return true;
            }
            else if (request.Url.AbsolutePath == "/rentals")
            {
                if (currentUser == null)
                {
                    response.StatusCode = 401;
                    html = HtmlGenerator.GenerateStyledHtml("Zugriff verweigert", "<h3>Bitte zuerst <a href='/login'>einloggen</a>.</h3>", null);
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Vermietungen", HtmlGenerator.GenerateRentalListPage(rentals, cars, currentUser), currentUser);
                }
                return true;
            }
            else if (request.Url.AbsolutePath == "/invoice" && request.HttpMethod == "GET")
            {
                var idStr = request.QueryString["id"];
                int id;
                if (int.TryParse(idStr, out id))
                {
                    var rental = rentals.FirstOrDefault(r => r.Id == id);
                    if (rental != null && (currentUser.Role == "Admin" || rental.Customer.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        var car = cars.FirstOrDefault(c => c.Id == rental.CarId);
                        string invoiceHtml = HtmlGenerator.GenerateInvoice(rental, car);
                        html = HtmlGenerator.GenerateStyledHtml("Rechnung", invoiceHtml, currentUser);
                    }
                    else
                    {
                        html = HtmlGenerator.GenerateStyledHtml("Nicht gefunden", "<h3>Vermietung nicht gefunden.</h3>", currentUser);
                    }
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Ungültige ID", "<h3>Ungültige Rechnungs-ID.</h3>", currentUser);
                }
                return true;
            }

            return false;
        }

        private static void ProcessRentalForm(HttpListenerRequest request, string customerEmail, List<Car> cars, List<Rental> rentals)
        {
            if (!request.HasEntityBody)
                throw new Exception("Keine Formulardaten erhalten");

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
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
                DateTime rentalDate = DateTime.Parse(formData["startDate"]);
                int days = (int)(DateTime.Parse(formData["endDate"]) - rentalDate).TotalDays;

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
                    Customer = customerEmail,
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
    }
}
