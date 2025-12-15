// Models/RoomModel.cs
namespace IhsanRoomWise.Models
{
    public class RoomModel
    {
        public int room_id { get; set; }
        public string room_code { get; set; } = string.Empty;
        public string room_name { get; set; } = string.Empty;
        public int room_location_id { get; set; }
        public int room_capacity { get; set; }
        public string? room_description { get; set; }
        public string? room_photo { get; set; }
        public bool room_has_projector { get; set; }
        public bool room_has_smart_screen { get; set; }
        public bool room_has_screenbeam { get; set; }
        public bool room_has_cisco_bar { get; set; }
        public string? room_other_facilities { get; set; }
        public string room_operational_status { get; set; } = "Available";
        public bool room_is_active { get; set; } = true;
        public DateTime room_created_at { get; set; }
        public DateTime? room_updated_at { get; set; }
    }
}