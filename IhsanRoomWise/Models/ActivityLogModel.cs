// Models/ActivityLogModel.cs
namespace IhsanRoomWise.Models
{
    public class ActivityLogModel
    {
        public long log_id { get; set; }
        public int log_user_id { get; set; }
        public string log_action_type { get; set; } = string.Empty;
        public string? log_entity_type { get; set; }
        public int? log_entity_id { get; set; }
        public string log_description { get; set; } = string.Empty;
        public string? log_old_values { get; set; }
        public string? log_new_values { get; set; }
        public DateTime log_created_at { get; set; }
    }
}