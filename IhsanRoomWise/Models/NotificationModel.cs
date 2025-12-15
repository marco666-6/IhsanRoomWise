// Models/NotificationModel.cs
namespace IhsanRoomWise.Models
{
    public class NotificationModel
    {
        public int notification_id { get; set; }
        public string notification_type { get; set; } = string.Empty;
        public string notification_title { get; set; } = string.Empty;
        public string notification_message { get; set; } = string.Empty;
        public int? notification_target_user_id { get; set; }
        public string? notification_target_role { get; set; }
        public int? notification_related_booking_id { get; set; }
        public int? notification_related_feedback_id { get; set; }
        public bool notification_is_read { get; set; }
        public DateTime? notification_read_at { get; set; }
        public string notification_priority { get; set; } = "Normal";
        public int notification_created_by { get; set; }
        public DateTime notification_created_at { get; set; }
    }
}