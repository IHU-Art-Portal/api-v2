﻿namespace Theatrical.Data.Models
{
    public class Event
    {
        public int Id { get; set; }
        public int ProductionId { get; set; }
        public int VenueId { get; set; }
        public DateTime DateEvent { get; set; }
        public string PriceRange { get; set; } = null!;
        public int SystemId { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Production Production { get; set; } = null!;
        public virtual System System { get; set; } = null!;
        public virtual Venue Venue { get; set; } = null!;
    }
}
