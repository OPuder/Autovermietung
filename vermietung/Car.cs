using System;
using System.Collections.Generic;

namespace CarRentalHttpServer
{
	public class Car
	{
		public int Id { get; set; }
		public string Brand { get; set; }
		public string Model { get; set; }
		public decimal DailyPrice { get; set; }
		public bool Available { get; set; }		
		public List<DateTime> BookedDates { get; set; }
		public DateTime? UpcomingRentalDate { get; set; }

		public Car()
		{
			BookedDates = new List<DateTime>();
			Available = true;
		}
	}
	
}