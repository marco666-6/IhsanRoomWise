// Models/FeedbackModel.cs
namespace IhsanRoomWise.Models
{
    public class FeedbackModel
    {
        public int feedback_id { get; set; }
        public int feedback_booking_id { get; set; }
        public int feedback_user_id { get; set; }
        public int feedback_room_id { get; set; }
        public byte feedback_rating { get; set; }
        public string? feedback_room_condition { get; set; }
        public string? feedback_facility_condition { get; set; }
        public string? feedback_issues_reported { get; set; }
        public string? feedback_photo { get; set; }
        public string? feedback_admin_response { get; set; }
        public int? feedback_admin_responded_by { get; set; }
        public DateTime? feedback_admin_responded_at { get; set; }
        public DateTime feedback_created_at { get; set; }
        public DateTime? feedback_updated_at { get; set; }
    }
}