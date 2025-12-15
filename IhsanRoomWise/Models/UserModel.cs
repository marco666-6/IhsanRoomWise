// Models/UserModel.cs
namespace IhsanRoomWise.Models
{
    public class UserModel
    {
        public int user_id { get; set; }
        public string user_employee_id { get; set; } = string.Empty;
        public string user_email { get; set; } = string.Empty;
        public string user_password { get; set; } = string.Empty;
        public string user_full_name { get; set; } = string.Empty;
        public string? user_phone { get; set; }
        public string user_role { get; set; } = string.Empty;
        public int user_dept_id { get; set; }
        public string? user_profile_photo { get; set; }
        public bool user_is_active { get; set; } = true;
        public DateTime? user_last_login { get; set; }
        public string? user_reset_token { get; set; }
        public DateTime? user_reset_token_expiry { get; set; }
        public DateTime user_created_at { get; set; }
        public DateTime? user_updated_at { get; set; }
    }
}