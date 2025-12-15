// Models/LocationModel.cs
namespace IhsanRoomWise.Models
{
    public class LocationModel
    {
        public int location_id { get; set; }
        public string location_code { get; set; } = string.Empty;
        public string location_plant_name { get; set; } = string.Empty;
        public byte location_block { get; set; }
        public byte location_floor { get; set; }
        public string? location_description { get; set; }
        public bool location_is_active { get; set; } = true;
        public DateTime location_created_at { get; set; }
        public DateTime? location_updated_at { get; set; }
    }
}