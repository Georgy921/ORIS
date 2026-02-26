using System.Collections.Generic;

namespace MiniHttpServer.Model
{
    // ==========================================
    // 1. Классы, соответствующие таблицам БД
    // ==========================================

    public class Tour
    {
        public int id { get; set; }
        public string? image_url { get; set; }
        public string? name { get; set; }
        public string? departure_city { get; set; }
        public string? arrival_city { get; set; }
        public string? departure_date { get; set; }
        public int nights_count { get; set; }
        public int adults_count { get; set; }
        public double tour_price { get; set; }
        public string? hotel_name { get; set; }
        public int rating { get; set; } = 5;
        public string? meal_plan { get; set; }
        public int bonus { get; set; }
        public string? wifi { get; set; }
        public int distance_to_airport { get; set; }
        public string? general_description { get; set; }
        public bool has_kids_club { get; set; }
        public bool has_aquapark { get; set; }
        public bool has_spa { get; set; }
        public string? room_type { get; set; }
        public string? popular_filters { get; set; }
        public string? region { get; set; }
        public  int monthly_payment { get { return (int)tour_price / 6; } }



        public string StarsDisplay
        {
            get
            {
                var stars = "";
                for (int i = 1; i <= 5; i++)
                    stars += i <= rating ? "★" : "☆";
                return stars;
            }
        }
    }
}