using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace CarRentalHttpServer
{
	public static class HtmlGenerator
	{
		public static string GenerateHomePage(List<Car> cars, List<Rental> rentals, User user)
		{
			var availableCarsCount = cars.Count(c => c.Available);
			int activeRentalsCount = 0;

			if (user != null)
			{
				if (user.Role == "Admin")
				{
					activeRentalsCount = rentals.Count(r =>
					                                   r.StartDate <= DateTime.Today &&
					                                   r.EndDate >= DateTime.Today
					                                  );
				}
				else
				{
					activeRentalsCount = rentals.Count(r =>
					                                   r.Customer.Equals(user.Email, StringComparison.OrdinalIgnoreCase) &&
					                                   r.StartDate <= DateTime.Today &&
					                                   r.EndDate >= DateTime.Today
					                                  );
				}
			}

			string rentalStatBlock = "";
			if (user != null)
			{
				rentalStatBlock = string.Format(@"
        <div class='stat-card'>
            <h3>{0}</h3>
            <p>Aktive Vermietungen</p>
            <a href='/rentals' class='btn small'>Details</a>
        </div>",
				                                activeRentalsCount);
			}
			string recentRentalsHtml = "";
			if (user != null && user.Role == "Admin")
			{
				recentRentalsHtml = string.Format(@"
        <div class='recent-rentals'>
            <h2>Letzte Vermietungen</h2>
            {0}
        </div>", GenerateRecentRentalsSection(rentals, cars, user));
			}
			return string.Format(@"
        <div class='stats-grid'>
            <div class='stat-card'>
                <h3>{0}</h3>
                <p>Verfügbare Fahrzeuge</p>
                <a href='/cars' class='btn small'>Alle anzeigen</a>
            </div>

            {1}
        </div>

        <div class='actions'>
            <h2>Schnellaktionen</h2>
            <div class='action-buttons'>
                <a href='/rent' class='btn large'>
                    <span class='icon'>🚗</span>
                    Auto mieten
                </a>

                <a href='/cars' class='btn large secondary'>
                    <span class='icon'>📋</span>
                    Fahrzeugliste
                </a>
            </div>
        </div>

        {2}",
			                     availableCarsCount,
			                     rentalStatBlock,
			                     recentRentalsHtml);
		}

		public static string GenerateRecentRentalsSection(List<Rental> rentals, List<Car> cars, User user)
		{
			if (rentals.Count == 0)
				return "<p class='notice'>Noch keine Vermietungen vorhanden</p>";

			IEnumerable<Rental> filteredRentals = rentals;

			if (user != null && user.Role != "Admin")
			{
				filteredRentals = rentals.Where(r => r.Customer.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
			}
			var recentRentals = filteredRentals.OrderByDescending(r => r.StartDate).Take(3).ToList();
			var sb = new StringBuilder("<div class='rental-cards'>");
			foreach (var rental in recentRentals)
			{
				var car = cars.First(c => c.Id == rental.CarId);
				sb.Append(string.Format(@"
            <div class='rental-card'>
                <div class='car-info'>
                    <span class='car-model'>{0} {1}</span>
                    <span class='customer'>{2}</span>
                </div>
                <div class='rental-dates'>
                    <span class='date-range'>{3:dd.MM.yyyy} - {4:dd.MM.yyyy}</span>
                    <span class='price'>{5:C}</span>
                </div>
            </div>",
				                        car.Brand, car.Model,
				                        rental.Customer,
				                        rental.StartDate, rental.EndDate,
				                        rental.TotalPrice));
			}
			sb.Append("</div>");
			return sb.ToString();
		}		
		
		public static string GenerateCarListPage(List<Car> cars, User user)
		{
			var tableRows = new StringBuilder();
			bool isAdmin = user != null && user.Role == "Admin";
			
			foreach (var car in cars)
			{
				string actions = "";
				if (isAdmin)
				{
					actions = string.Format(@"
                <form method='post' action='/delete-car' style='display:inline;' onsubmit='return confirm(""Möchtest du dieses Fahrzeug wirklich löschen?"");'>
                    <input type='hidden' name='id' value='{0}' />
                    <button class='btn small danger' type='submit'>Löschen</button>
                </form>", car.Id);
				}

				bool isRentedToday = car.BookedDates != null && car.BookedDates.Any(date => date.Date == DateTime.Today);
				string status = isRentedToday ? "❌ Heute vermietet" : "✅ Verfügbar";
				
				// Hinweis auf kommende Reservierung
				if (!isRentedToday && car.BookedDates != null)
				{
					var upcomingDate = car.BookedDates
						.Where(date => date.Date > DateTime.Today && date.Date <= DateTime.Today.AddDays(7))
						.OrderBy(date => date)
						.FirstOrDefault();

					if (upcomingDate != default(DateTime))
					{
						status += string.Format("<br/><small style='color:orange;'>Ab {0:dd.MM.yyyy} reserviert</small>", upcomingDate);
					}
				}
				
				// Only include actions cell if user is admin
				string rowContent = isAdmin
					? string.Format(
						"<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3:C}</td><td>{4}</td><td>{5}</td></tr>",
						car.Id, car.Brand, car.Model, car.DailyPrice, status, actions)
					: string.Format(
						"<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3:C}</td></tr>",
						car.Brand, car.Model, car.DailyPrice, status);
				
				tableRows.Append(rowContent);
			}
			
			string addCarForm = "";
			if (isAdmin)
			{
				addCarForm = @"
        <button class='btn' onclick='openAddCarModal()'>+ Fahrzeug hinzufügen</button>
        
        <div id='addCarModal' class='modal' style='display:none;'>
            <div class='modal-content'>
                <span class='close' onclick='closeAddCarModal()'>&times;</span>
                <h3>Neues Fahrzeug</h3>
                <form method='post' action='/add-car'>
                    <input type='text' name='brand' placeholder='Marke' required />
                    <input type='text' name='model' placeholder='Modell' required />
                    <input type='number' name='price' placeholder='Tagespreis (€)' step='0.01' required />
                    <button class='btn' type='submit'>Hinzufügen</button>
                </form>
            </div>
        </div>
        <style>
            .modal {
                display: none;
                position: fixed;
                z-index: 999;
                left: 0;
                top: 0;
                width: 100%;
                height: 100%;
                background-color: rgba(0, 0, 0, 0.5);
            }
            .modal-content {
                background-color: #fff;
                margin: 10% auto;
                padding: 20px;
                border-radius: 8px;
                width: 300px;
                box-shadow: 0 0 10px rgba(0, 0, 0, 0.3);
            }
            .close {
                float: right;
                font-size: 24px;
                cursor: pointer;
            }
        </style>
        <script>
            function openAddCarModal() {
                document.getElementById('addCarModal').style.display = 'block';
            }
            function closeAddCarModal() {
                document.getElementById('addCarModal').style.display = 'none';
            }
            window.onclick = function(event) {
                var modal = document.getElementById('addCarModal');
                if (event.target == modal) {
                    modal.style.display = 'none';
                }
            }
        </script>";
			}
			
			string idHeader = isAdmin ? "<th>Id</th>" : "";
			string actionHeader = isAdmin ? "<th>Aktion</th>" : "";
			
			return string.Format(@"
    <h2>Fahrzeugliste</h2>
    <div class='nav'>
        <a href='/rent' class='btn'>Neue Vermietung</a>
    </div>

    {1}

    <table>
        <thead>
            <tr>
                {3}
                <th>Marke</th>
                <th>Modell</th>
                <th>Tagespreis</th>
                <th>Status</th>
                {2}
            </tr>
        </thead>
        <tbody>
            {0}
        </tbody>
    </table>", tableRows.ToString(), addCarForm, actionHeader, idHeader);
		}
		
		public static string GenerateRentalForm(List<Car> cars, User user)
		{
			string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
			string nextDay = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

			return string.Format(@"
        <h2>Neue Fahrzeugmiete</h2>
        <form method='post' action='/rent' id='rentalForm'>
			<div class='form-group'>
    			<label for='customer'>Kunde</label>
    			<input type='text' id='customer' name='customer' value='{0}' readonly />
			</div>
            
            <div class='form-group'>
                <label for='startDate'>Mietbeginn</label>
                <input type='date' id='startDate' name='startDate' required
                       min='{2}' value='{2}'
                       onchange='updateCarList()'>
            </div>
            
            <div class='form-group'>
                <label for='endDate'>Mietende</label>
                <input type='date' id='endDate' name='endDate' required
                       min='{3}' value='{3}'
                       onchange='updateCarList()'>
            </div>
            
            <div class='form-group'>
                <label for='carId'>Verfügbare Fahrzeuge</label>
                <select id='carId' name='carId' required>
                    <option value=''>-- Bitte wählen --</option>
                </select>
            </div>
            
            <button type='submit' class='btn'>Mieten</button>
        </form>
        
        <script>
        function updateCarList() {{
            var startDateInput = document.getElementById('startDate');
            var endDateInput = document.getElementById('endDate');
            var carSelect = document.getElementById('carId');
            
            if (!startDateInput.value || !endDateInput.value) return;
            
            var startDate = new Date(startDateInput.value);
            var endDate = new Date(endDateInput.value);
            
            if (startDate >= endDate) {{
                carSelect.innerHTML = '';
                var invalidOption = document.createElement('option');
                invalidOption.value = '';
                invalidOption.textContent = 'Enddatum muss nach dem Startdatum liegen';
                carSelect.appendChild(invalidOption);
                carSelect.disabled = true;
                return;
            }}
            
            carSelect.innerHTML = '';
            var defaultOption = document.createElement('option');
            defaultOption.value = '';
            defaultOption.textContent = '-- Bitte wählen --';
            carSelect.appendChild(defaultOption);
            
            var allCars = {1};
            var availableModels = {{}};
            
            allCars.forEach(function(car) {{
                var isAvailable = true;
                for (var i = 0; i < car.bookedDates.length; i++) {{
                    var bookedDate = new Date(car.bookedDates[i]);
                    if (bookedDate >= startDate && bookedDate <= endDate) {{
                        isAvailable = false;
                        break;
                    }}
                }}
                
                if (isAvailable) {{
                    var modelKey = car.brand + '|' + car.model + '|' + car.dailyPrice.toFixed(2);
                    if (!availableModels[modelKey]) {{
                        availableModels[modelKey] = {{
                            brand: car.brand,
                            model: car.model,
                            dailyPrice: car.dailyPrice,
                            count: 0,
                            ids: []
                        }};
                    }}
                    availableModels[modelKey].count++;
                    availableModels[modelKey].ids.push(car.id);
                }}
            }});
            
            if (Object.keys(availableModels).length === 0) {{
                var noCarOption = document.createElement('option');
                noCarOption.value = '';
                noCarOption.textContent = 'Keine verfügbaren Fahrzeuge für diesen Zeitraum';
                carSelect.appendChild(noCarOption);
                carSelect.disabled = true;
            }} else {{
                for (var modelKey in availableModels) {{
                    var model = availableModels[modelKey];
                    var option = document.createElement('option');
                    option.value = model.ids[0];
                    option.textContent = model.brand + ' ' + model.model +
                                      ' (' + model.dailyPrice.toFixed(2) + '€/Tag)' +
                                      ' - ' + model.count + ' verfügbar';
                    option.dataset.ids = JSON.stringify(model.ids);
                    carSelect.appendChild(option);
                }}
                carSelect.disabled = false;
            }}
        }}
        
        document.getElementById('rentalForm').addEventListener('submit', function(e) {{
            var carSelect = document.getElementById('carId');
            if (carSelect.selectedOptions[0].dataset.ids) {{
                var availableIds = JSON.parse(carSelect.selectedOptions[0].dataset.ids);
                carSelect.value = availableIds[0];
            }}
        }});
        
        window.onload = updateCarList;
        </script>
        
        <div class='nav'>
            <a href='/' class='btn'>Startseite</a>
            <a href='/cars' class='btn'>Zurück zur Liste</a>
        </div>",
			                     user.Email,
			                     GetCarsJson(cars),
			                     currentDate,
			                     nextDay);
		}

		private static string GetCarsJson(List<Car> cars)
		{
			var sb = new StringBuilder("[");
			bool first = true;
			
			foreach (var car in cars)
			{
				if (!first) sb.Append(",");
				sb.Append("{");
				sb.AppendFormat("\"id\":{0},", car.Id);
				sb.AppendFormat("\"brand\":\"{0}\",", car.Brand);
				sb.AppendFormat("\"model\":\"{0}\",", car.Model);
				sb.AppendFormat("\"dailyPrice\":{0},", car.DailyPrice.ToString(CultureInfo.InvariantCulture));
				sb.Append("\"bookedDates\":[");
				bool firstDate = true;
				foreach (var date in car.BookedDates)
				{
					if (!firstDate) sb.Append(",");
					sb.AppendFormat("\"{0:yyyy-MM-dd}\"", date);
					firstDate = false;
				}
				sb.Append("]");
				
				sb.Append("}");
				first = false;
			}
			sb.Append("]");
			return sb.ToString();
		}
		
		public static string GenerateRentalListPage(List<Rental> rentals, List<Car> cars, User user)
		{
			IEnumerable<Rental> filteredRentals = rentals;

			if (user != null && user.Role == "Admin")
			{
				filteredRentals = rentals;
			}
			else if (user != null)
			{
				filteredRentals = rentals.Where(r => r.Customer.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
			}
			var tableRows = new StringBuilder();
			foreach (var rental in filteredRentals)
			{
				var car = cars.First(c => c.Id == rental.CarId);
				tableRows.Append(string.Format(
					"<tr>" +
					"<td>{0}</td>" +
					"<td><span class='customer-badge'>{1}</span></td>" +
					"<td><strong>{2} {3}</strong></td>" +
					"<td class='date-cell'>{4:dd.MM.yyyy}</td>" +
					"<td class='date-cell'>{5:dd.MM.yyyy}</td>" +
					"<td class='price-cell'>{6:C}</td>" +
					"<td><a href='/invoice?id={0}' class='btn small'>Rechnung</a></td>" +
					"</tr>",
					rental.Id,
					rental.Customer,
					car.Brand,
					car.Model,
					rental.StartDate,
					rental.EndDate,
					rental.TotalPrice));
			}

			string summaryHtml = "";
			if (user != null && user.Role == "Admin")
			{
				summaryHtml = string.Format(@"
		<div class='summary'>
			<div class='summary-item'>
				<span>Anzahl:</span>
				<strong>{0}</strong>
			</div>
			<div class='summary-item'>
				<span>Gesamtumsatz:</span>
				<strong>{1:C}</strong>
			</div>
		</div>", filteredRentals.Count(), filteredRentals.Sum(r => r.TotalPrice));
			}

			return string.Format(@"
		<div class='page-header'>
			<h2>Aktive Vermietungen</h2>
			<div class='header-actions'>
				<a href='/rent' class='btn'>Neue Vermietung</a>
				<a href='/cars' class='btn secondary'>Fahrzeuge anzeigen</a>
			</div>
		</div>

		<div class='responsive-table'>
			<table>
				<thead>
					<tr>
						<th>ID</th>
						<th>Kunde</th>
						<th>Fahrzeug</th>
						<th>Start</th>
						<th>Ende</th>
						<th>Gesamtpreis</th>
						<th>Rechnung</th>
					</tr>
				</thead>
				<tbody>
					{0}
				</tbody>
			</table>
		</div>
		{1}", tableRows.ToString(), summaryHtml);
		}

		public static string GenerateSuccessPage()
		{
			return @"
        <div class='success-message'>
            <h3>Vielen Dank für Ihre Buchung!</h3>
            <p>Ihre Fahrzeugmiete wurde erfolgreich registriert.</p>
        </div>
        
        <div class='nav'>
            <a href='/rentals' class='btn'>Zu den Vermietungen</a>
            <a href='/' class='btn'>Zurück zur Startseite</a>
        </div>";
		}

		public static string GenerateStyledHtml(string title, string bodyContent, User user)
		{
			return string.Format(@"
		<!DOCTYPE html>
			<html>
			<head>
    			<title>{0}</title>
    			<meta charset='UTF-8'>
    			<meta name='viewport' content='width=device-width, initial-scale=1.0'>
    			<style>
        			{1}
    			</style>
			</head>
			<body>
    			<div class='container'>
        			<header>
            			<h1>Rent & Roll</h1>
            			{3}
        			</header>
        			<div class='card'>
            			{2}
       				</div>
   				</div>
			</body>
		</html>",
			                     title,
			                     CssStyles.GetStyles(),
			                     bodyContent,
			                     GenerateNav(user));
		}
		
		public static string GenerateNav(User user)
		{
			var sb = new StringBuilder("<div class='nav'>");

			if (user != null)
			{
				sb.Append("<span>Willkommen, <strong>" + user.Name + " " + user.Nachname+ "</strong></span>");
				if (user.Role == "Admin")
				{
					sb.Append(" | <a href='/admin'>Adminbereich</a>");
				}
				sb.Append(" | <a href='/Startseite'>Startseite</a>");
				sb.Append(" | <a href='/logout'>Logout</a>");
			}
			else
			{
				sb.Append("<a href='/Startseite'>Startseite</a>");
				sb.Append(" | <a href='/login'>Login</a>");
				sb.Append(" | <a href='/register'>Registrieren</a>");
			}
			sb.Append("</div>");
			return sb.ToString();
		}
		
		public static string GenerateInvoice(Rental rental, Car car)
		{
			return string.Format(@"
        <div class='invoice'>
            <h2>Rechnung für Fahrzeug Nummer {0}</h2>
            <p><strong>Kunde:</strong> {1}</p>
            <p><strong>Fahrzeug:</strong> {2} {3}</p>
            <p><strong>Zeitraum:</strong> {4:dd.MM.yyyy} – {5:dd.MM.yyyy}</p>
            <p><strong>Tagespreis:</strong> {6:C}</p>
            <p><strong>Gesamt:</strong> <strong>{7:C}</strong></p>
            <div class='nav'>
                <a href='/rentals' class='btn'>Zurück</a>
            </div>
        </div>",
			                     rental.CarId,
			                     rental.Customer,
			                     car.Brand,
			                     car.Model,
			                     rental.StartDate,
			                     rental.EndDate,
			                     car.DailyPrice,
			                     rental.TotalPrice
			                    );
		}
		
		public static string GenerateUserManagementPage(List<User> users, User currentUser)
		{
			var sb = new StringBuilder();

			sb.AppendLine("<h2>Benutzerverwaltung</h2>");
			sb.AppendLine("<div class='user-actions'>");
			sb.AppendLine("<button class='btn' onclick='openAddUserModal()'>+ Benutzer hinzufügen</button>");
			sb.AppendLine("</div>");

			sb.AppendLine("<table>");
			sb.AppendLine("<thead><tr>" +
			              "<th>Name</th>" +
			              "<th>Nachname</th>" +
			              "<th>E-Mail</th>" +
			              "<th>Rolle</th>" +
			              "<th>Aktionen</th>" +
			              "</tr></thead>");
			sb.AppendLine("<tbody>");

			foreach (var user in users)
			{
				string rowId = "row_" + user.Email.Replace("@", "_").Replace(".", "_");
				string roleFormId = "roleForm_" + rowId;
				string actionsId = "actions_" + rowId;
				string passwordFormId = "pwForm_" + rowId;
				string roleButtonId = "btnRole_" + rowId;
				string passwordButtonId = "btnPw_" + rowId;

				sb.AppendLine("<tr>");
				sb.AppendFormat("<td>{0}</td>", user.Name);
				sb.AppendFormat("<td>{0}</td>", user.Nachname);
				sb.AppendFormat("<td>{0}</td>", user.Email);

				string roleClass = user.Role == "Admin" ? "role-admin" :
					user.Role == "Ban" ? "role-ban" : "role-user";
				sb.AppendFormat("<td class='{0}'>{1}</td>", roleClass, user.Role);
				sb.AppendLine("<td>");

				// --- Rollen verwalten Button ---
				sb.AppendFormat("<button id='{0}' class='btn small' onclick='toggleRoleForm(\"{1}\", \"{2}\", \"{3}\", \"{4}\")'>Rollen verwalten</button>",
				                roleButtonId, roleFormId, passwordFormId, actionsId, passwordButtonId);

				// Rollen Dropdown (versteckt)
				sb.AppendLine("<div id='" + roleFormId + "' style='display:none; margin-top:5px;'>");
				sb.AppendLine("<form method='post' action='/change-role'>");
				sb.AppendFormat("<input type='hidden' name='email' value='{0}' />", user.Email);
				sb.AppendLine("<select name='role'>");
				sb.AppendLine("<option value='Admin'" + (user.Role == "Admin" ? " selected" : "") + ">Admin</option>");
				sb.AppendLine("<option value='User'" + (user.Role == "User" ? " selected" : "") + ">User</option>");
				sb.AppendLine("<option value='Ban'" + (user.Role == "Ban" ? " selected" : "") + ">Ban</option>");
				sb.AppendLine("</select>");
				sb.AppendLine("<br/>");
				sb.AppendLine("<button class='btn small' type='submit'>Speichern</button>");
				sb.AppendLine("</form>");
				sb.AppendLine("</div>");

				// --- Passwort ändern Button ---
				sb.AppendFormat("<button id='{0}' class='btn small' onclick='togglePasswordForm(\"{1}\", \"{2}\", \"{3}\", \"{4}\")'>Passwort ändern</button>",
				                passwordButtonId, passwordFormId, roleFormId, actionsId, roleButtonId);
				
				// Passwort-Formular (versteckt)
				sb.AppendLine("<div id='" + passwordFormId + "' style='display:none; margin-top:5px;'>");
				sb.AppendLine("<form method='post' action='/change-password'>");
				sb.AppendFormat("<input type='hidden' name='email' value='{0}' />", user.Email);
				sb.AppendLine("<input type='password' name='newPassword' placeholder='Neues Passwort' required />");
				sb.AppendLine("<button class='btn small' type='submit'>Speichern</button>");
				sb.AppendLine("</form>");
				sb.AppendLine("</div>");

				// --- Aktionen (löschen etc.) ---
				sb.AppendFormat("<div id='{0}'>", actionsId);
				sb.AppendLine("<form method='post' action='/delete-user' style='display:inline; margin-left:5px;' onsubmit='return confirm(\"Möchtest du diesen Benutzer wirklich löschen?\");'>");
				sb.AppendFormat("<input type='hidden' name='email' value='{0}' />", user.Email);
				sb.AppendLine("<button class='btn small danger' type='submit'>Löschen</button>");
				sb.AppendLine("</form>");
				sb.AppendLine("</div>"); // Ende Aktionen

				sb.AppendLine("</td></tr>");
			}

			sb.AppendLine("</tbody></table>");

			// Benutzer hinzufügen Modal
			sb.AppendLine(@"
	<div id='addUserModal' class='modal' style='display:none;'>
		<div class='modal-content'>
			<span class='close' onclick='closeAddUserModal()'>&times;</span>
			<h3>Neuen Benutzer erstellen</h3>
			<form method='post' action='/add-user'>
				<input type='text' name='name' placeholder='Vorname' required />
				<input type='text' name='nachname' placeholder='Nachname' required />
				<input type='email' name='email' placeholder='E-Mail' required />
				<input type='password' name='password' placeholder='Passwort' required />
    			<label>Sicherheitsfrage</label>
    			<select name='securityQuestion' required>
        				<option value='schule'>Was war der Name deiner ersten Grundschule?</option>
        				<option value='haustier'>Wie hieß dein erstes Haustier?</option>
        				<option value='geburtsort'>Wo bist du geboren?</option>
    			</select>
    			<label>Antwort</label>
    			<input type='text' name='securityAnswer' required />
				<select name='role'>
					<option value='User'>User</option>
					<option value='Admin'>Admin</option>
					<option value='Ban'>Ban</option>
				</select>
				<button class='btn' type='submit'>Erstellen</button>
			</form>
		</div>
	</div>

	<script>
	function openAddUserModal() {
		document.getElementById('addUserModal').style.display = 'block';
	}
	function closeAddUserModal() {
		document.getElementById('addUserModal').style.display = 'none';
	}
	window.onclick = function(event) {
		var modal = document.getElementById('addUserModal');
		if (event.target == modal) {
			modal.style.display = 'none';
		}
	}

function toggleRoleForm(formId, passwordFormId, actionsId, passwordButtonId) {
	var roleForm = document.getElementById(formId);
	var passwordForm = document.getElementById(passwordFormId);
	var actions = document.getElementById(actionsId);
	var pwBtn = document.getElementById(passwordButtonId);

	// Verstecke Passwortformular und Passwort-Button
	if (passwordForm) passwordForm.style.display = 'none';
	if (pwBtn) pwBtn.style.display = 'inline';

	if (roleForm.style.display === 'none') {
		roleForm.style.display = 'block';
		if (actions) actions.style.display = 'none';
		if (pwBtn) pwBtn.style.display = 'none'; // Verstecke den Button
	} else {
		roleForm.style.display = 'none';
		if (actions) actions.style.display = 'inline';
		if (pwBtn) pwBtn.style.display = 'inline';
	}
}

function togglePasswordForm(formId, roleFormId, actionsId, roleButtonId) {
	var passwordForm = document.getElementById(formId);
	var roleForm = document.getElementById(roleFormId);
	var actions = document.getElementById(actionsId);
	var roleBtn = document.getElementById(roleButtonId);

	// Verstecke Rollenformular und Rollen-Button
	if (roleForm) roleForm.style.display = 'none';
	if (roleBtn) roleBtn.style.display = 'inline';

	if (passwordForm.style.display === 'none') {
		passwordForm.style.display = 'block';
		if (actions) actions.style.display = 'none';
		if (roleBtn) roleBtn.style.display = 'none';
	} else {
		passwordForm.style.display = 'none';
		if (actions) actions.style.display = 'inline';
		if (roleBtn) roleBtn.style.display = 'inline';
	}
}
	</script>");

			return sb.ToString();
		}

	}
}