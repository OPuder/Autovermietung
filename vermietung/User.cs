/*
 * Created by SharpDevelop.
 * User: puder
 * Date: 28.03.2025
 * Time: 10:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace CarRentalHttpServer
{
	public class User
	{
		public string Name { get; set; }
		public string Nachname { get; set; }
		public string Email { get; set; }
		public string Salt { get; set; }
		public string PasswordHash { get; set; }
		public string Role { get; set; }
		public string SecurityQuestion { get; set; }
		public string SecurityAnswer { get; set; }
	}
}
