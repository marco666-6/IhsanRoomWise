// Models/User.cs
namespace IhsanRoomWise.Models
{
    public class User
    {
        public int user_id { get; set; }
        public string user_employee_id { get; set; }
        public string user_email { get; set; }
        public string user_password { get; set; }
        public string user_full_name { get; set; }
        public string user_role { get; set; }
        public string? user_dept_name { get; set; }
        public bool user_is_active { get; set; }
        public DateTime user_created_at { get; set; }
        public DateTime user_updated_at { get; set; }
    }
}

// Models/Location.cs
namespace IhsanRoomWise.Models
{
    public class Location
    {
        public int location_id { get; set; }
        public string location_code { get; set; }
        public string location_plant_name { get; set; }
        public byte location_block { get; set; }
        public byte location_floor { get; set; }
        public bool location_is_active { get; set; }
        public DateTime location_created_at { get; set; }
    }
}

// Models/Room.cs
namespace IhsanRoomWise.Models
{
    public class Room
    {
        public int room_id { get; set; }
        public string room_code { get; set; }
        public string room_name { get; set; }
        public int room_location_id { get; set; }
        public int room_capacity { get; set; }
        public string? room_facilities { get; set; }
        public string room_status { get; set; }
        public bool room_is_active { get; set; }
        public DateTime room_created_at { get; set; }
        public DateTime room_updated_at { get; set; }
    }
}

// Models/Booking.cs
namespace IhsanRoomWise.Models
{
    public class Booking
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public int booking_user_id { get; set; }
        public int booking_room_id { get; set; }
        public string booking_title { get; set; }
        public string? booking_description { get; set; }
        public DateTime booking_date { get; set; }
        public TimeSpan booking_start_time { get; set; }
        public TimeSpan booking_end_time { get; set; }
        public string booking_status { get; set; }
        public string? booking_cancel_reason { get; set; }
        public int? booking_cancelled_by { get; set; }
        public DateTime booking_created_at { get; set; }
        public DateTime booking_updated_at { get; set; }
    }
}

// Models/Feedback.cs
namespace IhsanRoomWise.Models
{
    public class Feedback
    {
        public int feedback_id { get; set; }
        public int feedback_booking_id { get; set; }
        public int feedback_user_id { get; set; }
        public byte feedback_rating { get; set; }
        public string? feedback_comments { get; set; }
        public string? feedback_admin_response { get; set; }
        public int? feedback_admin_responded_by { get; set; }
        public DateTime? feedback_responded_at { get; set; }
        public DateTime feedback_created_at { get; set; }
    }
}
