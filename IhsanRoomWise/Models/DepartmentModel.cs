// Models/DepartmentModel.cs
namespace IhsanRoomWise.Models
{
    public class DepartmentModel
    {
        public int dept_id { get; set; }
        public string dept_code { get; set; } = string.Empty;
        public string dept_name { get; set; } = string.Empty;
        public string? dept_description { get; set; }
        public bool dept_is_active { get; set; } = true;
        public DateTime dept_created_at { get; set; }
        public DateTime? dept_updated_at { get; set; }
    }
}