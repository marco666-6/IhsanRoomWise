// Models/BookingModel.cs
namespace IhsanRoomWise.Models
{
    public class BookingModel
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; } = string.Empty;
        public int booking_user_id { get; set; }
        public int booking_room_id { get; set; }
        public string booking_meeting_title { get; set; } = string.Empty;
        public string? booking_meeting_description { get; set; }
        public DateTime booking_date { get; set; }
        public TimeSpan booking_start_time { get; set; }
        public TimeSpan booking_end_time { get; set; }
        public string booking_status { get; set; } = "Pending";
        public string? booking_cancellation_reason { get; set; }
        public DateTime? booking_cancelled_at { get; set; }
        public int? booking_cancelled_by { get; set; }
        public DateTime? booking_actual_end_time { get; set; }
        public DateTime booking_created_at { get; set; }
        public DateTime? booking_updated_at { get; set; }
    }
}