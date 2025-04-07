using System;

namespace CarRentalHttpServer
{
    public class Rental
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string Customer { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
    }
}