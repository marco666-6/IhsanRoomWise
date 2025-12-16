// Controllers\AdminController.cs (Part 1 - Users, Departments, Rooms, Locations)

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using IhsanRoomWise.Functions;
using IhsanRoomWise.Models;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace RoomWise.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString;

        public AdminController()
        {
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        private bool CheckAuth()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userId == null || userRole != "Admin")
            {
                return false;
            }
            return true;
        }

        private void LogActivity(string actionType, string? entityType, int? entityId, string description, string? oldValues = null, string? newValues = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"EXEC sp_log_user_activity 
                        @user_id = @UserId, 
                        @action_type = @ActionType, 
                        @entity_type = @EntityType, 
                        @entity_id = @EntityId, 
                        @description = @Description,
                        @old_values = @OldValues,
                        @new_values = @NewValues";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", HttpContext.Session.GetInt32("UserId"));
                        cmd.Parameters.AddWithValue("@ActionType", actionType);
                        cmd.Parameters.AddWithValue("@EntityType", (object?)entityType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityId", (object?)entityId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@OldValues", (object?)oldValues ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewValues", (object?)newValues ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // ==================== DASHBOARD ====================
        [HttpGet]
        public IActionResult DashboardView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            
            var dashboardData = new
            {
                // Basic Stats
                TotalRooms = 0,
                TotalUsers = 0,
                TodayBookings = 0,
                PendingFeedbacks = 0,
                ActiveBookingsNow = 0,
                AvailableRooms = 0,
                TotalBookingsThisMonth = 0,
                AverageRating = 0m,
                
                // Trend Data
                BookingTrendsData = new List<object>(),
                RoomUtilizationData = new List<object>(),
                PeakHoursData = new List<object>(),
                DepartmentUsageData = new List<object>(),
                MonthlyComparisonData = new List<object>(),
                RoomPopularityData = new List<object>(),
                
                // Tables Data
                UpcomingBookings = new List<object>(),
                RecentFeedbacks = new List<object>(),
                TopUsers = new List<object>(),
                MaintenanceAlerts = new List<object>()
            };
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // 1. BASIC STATISTICS
                    dashboardData = new
                    {
                        TotalRooms = GetScalarInt(conn, "SELECT COUNT(*) FROM rooms WHERE room_is_active = 1"),
                        TotalUsers = GetScalarInt(conn, "SELECT COUNT(*) FROM users WHERE user_is_active = 1"),
                        TodayBookings = GetScalarInt(conn, "SELECT COUNT(*) FROM bookings WHERE booking_date = CAST(GETDATE() AS DATE)"),
                        PendingFeedbacks = GetScalarInt(conn, "SELECT COUNT(*) FROM feedbacks WHERE feedback_admin_response IS NULL"),
                        ActiveBookingsNow = GetScalarInt(conn, @"
                            SELECT COUNT(*) FROM bookings 
                            WHERE booking_date = CAST(GETDATE() AS DATE) 
                            AND CAST(GETDATE() AS TIME) BETWEEN booking_start_time AND booking_end_time
                            AND booking_status = 'Confirmed'"),
                        AvailableRooms = GetScalarInt(conn, @"
                            SELECT COUNT(*) FROM rooms r
                            WHERE r.room_is_active = 1 
                            AND r.room_operational_status = 'Available'
                            AND NOT EXISTS (
                                SELECT 1 FROM bookings b
                                WHERE b.booking_room_id = r.room_id
                                AND b.booking_date = CAST(GETDATE() AS DATE)
                                AND CAST(GETDATE() AS TIME) BETWEEN b.booking_start_time AND b.booking_end_time
                                AND b.booking_status = 'Confirmed'
                            )"),
                        TotalBookingsThisMonth = GetScalarInt(conn, @"
                            SELECT COUNT(*) FROM bookings 
                            WHERE MONTH(booking_date) = MONTH(GETDATE()) 
                            AND YEAR(booking_date) = YEAR(GETDATE())"),
                        AverageRating = GetScalarDecimal(conn, "SELECT ISNULL(AVG(CAST(feedback_rating AS FLOAT)), 0) FROM feedbacks"),
                        
                        // 2. BOOKING TRENDS (Last 7 Days)
                        BookingTrendsData = GetListData(conn, @"
                            SELECT TOP 7
                                CONVERT(VARCHAR, DATEADD(DAY, -number, GETDATE()), 107) as date,
                                DATENAME(WEEKDAY, DATEADD(DAY, -number, GETDATE())) as day,
                                COUNT(b.booking_id) as count
                            FROM master.dbo.spt_values
                            LEFT JOIN bookings b ON CAST(b.booking_date AS DATE) = CAST(DATEADD(DAY, -number, GETDATE()) AS DATE)
                            WHERE type = 'P' AND number BETWEEN 0 AND 6
                            GROUP BY DATEADD(DAY, -number, GETDATE()), number
                            ORDER BY number DESC"),
                        
                        // 3. ROOM UTILIZATION STATUS
                        RoomUtilizationData = GetListData(conn, @"
                            SELECT 
                                CASE
                                    WHEN r.room_operational_status = 'Maintenance' THEN 'Maintenance'
                                    WHEN r.room_operational_status != 'Available' THEN 'Out of Service'
                                    WHEN b.booking_id IS NOT NULL THEN 'Occupied'
                                    ELSE 'Available'
                                END AS status,
                                COUNT(*) AS count
                            FROM rooms r
                            LEFT JOIN bookings b
                                ON b.booking_room_id = r.room_id
                                AND b.booking_date = CAST(GETDATE() AS DATE)
                                AND CAST(GETDATE() AS TIME) BETWEEN b.booking_start_time AND b.booking_end_time
                                AND b.booking_status = 'Confirmed'
                            WHERE r.room_is_active = 1
                            GROUP BY
                                CASE
                                    WHEN r.room_operational_status = 'Maintenance' THEN 'Maintenance'
                                    WHEN r.room_operational_status != 'Available' THEN 'Out of Service'
                                    WHEN b.booking_id IS NOT NULL THEN 'Occupied'
                                    ELSE 'Available'
                                END"),
                        
                        // 4. PEAK HOURS (Today's bookings by hour)
                        PeakHoursData = GetListData(conn, @"
                            SELECT 
                                DATEPART(HOUR, booking_start_time) as hour,
                                COUNT(*) as bookings
                            FROM bookings
                            WHERE booking_date = CAST(GETDATE() AS DATE)
                            GROUP BY DATEPART(HOUR, booking_start_time)
                            ORDER BY hour"),
                        
                        // 5. DEPARTMENT USAGE (This Month)
                        DepartmentUsageData = GetListData(conn, @"
                            SELECT TOP 5
                                d.dept_name as department,
                                COUNT(b.booking_id) as bookings
                            FROM bookings b
                            INNER JOIN users u ON b.booking_user_id = u.user_id
                            INNER JOIN departments d ON u.user_dept_id = d.dept_id
                            WHERE MONTH(b.booking_date) = MONTH(GETDATE())
                            AND YEAR(b.booking_date) = YEAR(GETDATE())
                            GROUP BY d.dept_name
                            ORDER BY COUNT(b.booking_id) DESC"),
                        
                        // 6. MONTHLY COMPARISON (Last 6 Months)
                        MonthlyComparisonData = GetListData(conn, @"
                            SELECT TOP 6
                                FORMAT(DATEADD(MONTH, -number, GETDATE()), 'MMM yyyy') as month,
                                COUNT(b.booking_id) as bookings,
                                AVG(CAST(f.feedback_rating AS FLOAT)) as avg_rating
                            FROM master.dbo.spt_values
                            LEFT JOIN bookings b ON 
                                MONTH(b.booking_date) = MONTH(DATEADD(MONTH, -number, GETDATE()))
                                AND YEAR(b.booking_date) = YEAR(DATEADD(MONTH, -number, GETDATE()))
                            LEFT JOIN feedbacks f ON b.booking_id = f.feedback_booking_id
                            WHERE type = 'P' AND number BETWEEN 0 AND 5
                            GROUP BY DATEADD(MONTH, -number, GETDATE()), number
                            ORDER BY number DESC"),
                        
                        // 7. ROOM POPULARITY
                        RoomPopularityData = GetListData(conn, @"
                            SELECT TOP 5
                                r.room_name,
                                COUNT(b.booking_id) as total_bookings,
                                AVG(CAST(f.feedback_rating AS FLOAT)) as avg_rating,
                                SUM(DATEDIFF(MINUTE, b.booking_start_time, b.booking_end_time)) as total_minutes
                            FROM rooms r
                            LEFT JOIN bookings b ON r.room_id = b.booking_room_id
                                AND b.booking_status = 'Confirmed'
                                AND b.booking_date >= DATEADD(MONTH, -1, GETDATE())
                            LEFT JOIN feedbacks f ON b.booking_id = f.feedback_booking_id
                            WHERE r.room_is_active = 1
                            GROUP BY r.room_name
                            ORDER BY COUNT(b.booking_id) DESC"),
                        
                        // 8. UPCOMING BOOKINGS (Next 5)
                        UpcomingBookings = GetListData(conn, @"
                            SELECT TOP 5
                                b.booking_id,
                                r.room_name,
                                u.user_full_name,
                                b.booking_meeting_title,
                                b.booking_date,
                                b.booking_start_time,
                                b.booking_end_time,
                                b.booking_status
                            FROM bookings b
                            INNER JOIN rooms r ON b.booking_room_id = r.room_id
                            INNER JOIN users u ON b.booking_user_id = u.user_id
                            WHERE b.booking_date >= CAST(GETDATE() AS DATE)
                            AND b.booking_status != 'Cancelled'
                            ORDER BY b.booking_date, b.booking_start_time"),
                        
                        // 9. RECENT FEEDBACKS (Last 5)
                        RecentFeedbacks = GetListData(conn, @"
                            SELECT TOP 5
                                f.feedback_id,
                                u.user_full_name,
                                r.room_name,
                                f.feedback_rating,
                                f.feedback_issues_reported,
                                f.feedback_created_at,
                                CASE WHEN f.feedback_admin_response IS NULL THEN 0 ELSE 1 END as is_responded
                            FROM feedbacks f
                            INNER JOIN users u ON f.feedback_user_id = u.user_id
                            INNER JOIN rooms r ON f.feedback_room_id = r.room_id
                            ORDER BY f.feedback_created_at DESC"),
                        
                        // 10. TOP USERS (Most Active This Month)
                        TopUsers = GetListData(conn, @"
                            SELECT TOP 5
                                u.user_full_name,
                                d.dept_name,
                                COUNT(b.booking_id) as total_bookings,
                                SUM(DATEDIFF(MINUTE, b.booking_start_time, b.booking_end_time)) as total_minutes
                            FROM users u
                            INNER JOIN departments d ON u.user_dept_id = d.dept_id
                            LEFT JOIN bookings b ON u.user_id = b.booking_user_id
                                AND MONTH(b.booking_date) = MONTH(GETDATE())
                                AND YEAR(b.booking_date) = YEAR(GETDATE())
                            WHERE u.user_is_active = 1
                            GROUP BY u.user_full_name, d.dept_name
                            HAVING COUNT(b.booking_id) > 0
                            ORDER BY COUNT(b.booking_id) DESC"),
                        
                        // 11. MAINTENANCE ALERTS
                        MaintenanceAlerts = GetListData(conn, @"
                            SELECT 
                                r.room_name,
                                r.room_operational_status,
                                COUNT(f.feedback_id) as issue_count,
                                MAX(f.feedback_created_at) as last_issue_date
                            FROM rooms r
                            LEFT JOIN feedbacks f ON r.room_id = f.feedback_room_id
                                AND f.feedback_rating <= 2
                                AND f.feedback_created_at >= DATEADD(DAY, -7, GETDATE())
                            WHERE r.room_operational_status != 'Available' 
                                OR EXISTS (
                                    SELECT 1 FROM feedbacks f2
                                    WHERE f2.feedback_room_id = r.room_id
                                    AND f2.feedback_rating <= 2
                                    AND f2.feedback_created_at >= DATEADD(DAY, -7, GETDATE())
                                )
                            GROUP BY r.room_name, r.room_operational_status
                            HAVING COUNT(f.feedback_id) > 0 OR r.room_operational_status != 'Available'")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard Error: {ex.Message}");
            }
            
            return View(dashboardData);
        }

        // Helper Methods
        private int GetScalarInt(SqlConnection conn, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        private decimal GetScalarDecimal(SqlConnection conn, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        private List<object> GetListData(SqlConnection conn, string query)
        {
            var list = new List<object>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    list.Add(item);
                }
            }
            return list;
        }

        // ==================== USERS MANAGEMENT ====================
        [HttpGet]
        public IActionResult UsersView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetUsers(string? search = null, string? role = null, int? deptId = null, bool? isActive = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT u.*, d.dept_name 
                        FROM users u 
                        LEFT JOIN departments d ON u.user_dept_id = d.dept_id 
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (!string.IsNullOrEmpty(search))
                    {
                        query.Append(" AND (u.user_full_name LIKE @Search OR u.user_email LIKE @Search OR u.user_employee_id LIKE @Search)");
                        parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                    }
                    
                    if (!string.IsNullOrEmpty(role))
                    {
                        query.Append(" AND u.user_role = @Role");
                        parameters.Add(new SqlParameter("@Role", role));
                    }
                    
                    if (deptId.HasValue)
                    {
                        query.Append(" AND u.user_dept_id = @DeptId");
                        parameters.Add(new SqlParameter("@DeptId", deptId.Value));
                    }
                    
                    if (isActive.HasValue)
                    {
                        query.Append(" AND u.user_is_active = @IsActive");
                        parameters.Add(new SqlParameter("@IsActive", isActive.Value));
                    }
                    
                    query.Append(" ORDER BY u.user_created_at DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var users = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            users.Add(new
                            {
                                user_id = row["user_id"],
                                user_employee_id = row["user_employee_id"],
                                user_email = row["user_email"],
                                user_full_name = row["user_full_name"],
                                user_phone = row["user_phone"],
                                user_role = row["user_role"],
                                dept_name = row["dept_name"],
                                user_dept_id = row["user_dept_id"],
                                user_is_active = row["user_is_active"],
                                user_last_login = row["user_last_login"],
                                user_created_at = row["user_created_at"]
                            });
                        }
                        
                        return Json(new { success = true, data = users });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserModel user)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                user.user_password = SecHelperFunction.HashPasswordMD5("Password123"); // Default password
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO users (
                            user_employee_id, user_email, user_password, user_full_name, 
                            user_phone, user_role, user_dept_id, user_is_active
                        ) VALUES (
                            @EmployeeId, @Email, @Password, @FullName, 
                            @Phone, @Role, @DeptId, @IsActive
                        );
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeId", user.user_employee_id);
                        cmd.Parameters.AddWithValue("@Email", user.user_email);
                        cmd.Parameters.AddWithValue("@Password", user.user_password);
                        cmd.Parameters.AddWithValue("@FullName", user.user_full_name);
                        cmd.Parameters.AddWithValue("@Phone", (object?)user.user_phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Role", user.user_role);
                        cmd.Parameters.AddWithValue("@DeptId", user.user_dept_id);
                        cmd.Parameters.AddWithValue("@IsActive", user.user_is_active);
                        
                        int userId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create User", "User", userId, $"Created user {user.user_full_name} ({user.user_email})");
                        
                        return Json(new { success = true, message = "User created successfully", userId = userId });
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627) // Duplicate key
            {
                return Json(new { success = false, message = "Employee ID or Email already exists" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateUser([FromBody] UserModel user)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                // First get old values for logging
                string oldValues = "";
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string getOldQuery = "SELECT user_full_name, user_email, user_role, user_dept_id, user_is_active FROM users WHERE user_id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(getOldQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", user.user_id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                oldValues = Newtonsoft.Json.JsonConvert.SerializeObject(new
                                {
                                    user_full_name = reader["user_full_name"],
                                    user_email = reader["user_email"],
                                    user_role = reader["user_role"],
                                    user_dept_id = reader["user_dept_id"],
                                    user_is_active = reader["user_is_active"]
                                });
                            }
                        }
                    }
                    
                    // Update user
                    string query = @"
                        UPDATE users SET 
                            user_full_name = @FullName,
                            user_email = @Email,
                            user_phone = @Phone,
                            user_role = @Role,
                            user_dept_id = @DeptId,
                            user_is_active = @IsActive,
                            user_updated_at = GETDATE()
                        WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", user.user_id);
                        cmd.Parameters.AddWithValue("@FullName", user.user_full_name);
                        cmd.Parameters.AddWithValue("@Email", user.user_email);
                        cmd.Parameters.AddWithValue("@Phone", (object?)user.user_phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Role", user.user_role);
                        cmd.Parameters.AddWithValue("@DeptId", user.user_dept_id);
                        cmd.Parameters.AddWithValue("@IsActive", user.user_is_active);
                        
                        cmd.ExecuteNonQuery();
                        
                        string newValues = Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            user.user_full_name,
                            user.user_email,
                            user.user_role,
                            user.user_dept_id,
                            user.user_is_active
                        });
                        
                        LogActivity("Update User", "User", user.user_id, $"Updated user {user.user_full_name}", oldValues, newValues);
                        
                        return Json(new { success = true, message = "User updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleUserStatus(int userId, bool isActive)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET user_is_active = @IsActive, user_updated_at = GETDATE() WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        
                        cmd.ExecuteNonQuery();
                        
                        string action = isActive ? "activated" : "deactivated";
                        LogActivity("Toggle User Status", "User", userId, $"User {action}");
                        
                        return Json(new { success = true, message = $"User {action} successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ResetUserPassword(int userId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                string newPassword = "Password123";
                string hashedPassword = SecHelperFunction.HashPasswordMD5(newPassword);
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET user_password = @Password, user_updated_at = GETDATE() WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Reset Password", "User", userId, "Password reset to default");
                        
                        return Json(new { success = true, message = $"Password reset to {newPassword}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportUsers(string? search = null, string? role = null, int? deptId = null, bool? isActive = null)
        {
            if (!CheckAuth()) return RedirectToAction("UsersView");
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT 
                            u.user_employee_id as 'Employee ID',
                            u.user_email as 'Email',
                            u.user_full_name as 'Full Name',
                            u.user_phone as 'Phone',
                            u.user_role as 'Role',
                            d.dept_name as 'Department',
                            CASE WHEN u.user_is_active = 1 THEN 'Active' ELSE 'Inactive' END as 'Status',
                            FORMAT(u.user_last_login, 'dd/MM/yyyy HH:mm') as 'Last Login',
                            FORMAT(u.user_created_at, 'dd/MM/yyyy') as 'Created Date'
                        FROM users u 
                        LEFT JOIN departments d ON u.user_dept_id = d.dept_id 
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (!string.IsNullOrEmpty(search))
                    {
                        query.Append(" AND (u.user_full_name LIKE @Search OR u.user_email LIKE @Search OR u.user_employee_id LIKE @Search)");
                        parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                    }
                    
                    if (!string.IsNullOrEmpty(role))
                    {
                        query.Append(" AND u.user_role = @Role");
                        parameters.Add(new SqlParameter("@Role", role));
                    }
                    
                    if (deptId.HasValue)
                    {
                        query.Append(" AND u.user_dept_id = @DeptId");
                        parameters.Add(new SqlParameter("@DeptId", deptId.Value));
                    }
                    
                    if (isActive.HasValue)
                    {
                        query.Append(" AND u.user_is_active = @IsActive");
                        parameters.Add(new SqlParameter("@IsActive", isActive.Value));
                    }
                    
                    query.Append(" ORDER BY u.user_created_at DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Users");
                            
                            // Add title
                            worksheet.Cell(1, 1).Value = "RoomWise - Users Report";
                            worksheet.Cell(1, 1).Style.Font.Bold = true;
                            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                            worksheet.Range(1, 1, 1, dt.Columns.Count).Merge();
                            
                            // Add export date
                            worksheet.Cell(2, 1).Value = $"Exported on: {DateTime.Now:dd/MM/yyyy HH:mm}";
                            worksheet.Range(2, 1, 2, dt.Columns.Count).Merge();
                            
                            // Add headers starting from row 4
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                worksheet.Cell(4, i + 1).Value = dt.Columns[i].ColumnName;
                                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                                worksheet.Cell(4, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                            
                            // Add data
                            for (int row = 0; row < dt.Rows.Count; row++)
                            {
                                for (int col = 0; col < dt.Columns.Count; col++)
                                {
                                    worksheet.Cell(row + 5, col + 1).Value = dt.Rows[row][col].ToString();
                                    worksheet.Cell(row + 5, col + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                    
                                    // Alternate row colors
                                    if (row % 2 == 0)
                                    {
                                        worksheet.Cell(row + 5, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                                    }
                                }
                            }
                            
                            // Auto-fit columns
                            worksheet.Columns().AdjustToContents();
                            
                            // Create memory stream
                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                var content = stream.ToArray();
                                
                                LogActivity("Export Data", "User", null, "Exported users to Excel");
                                
                                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                                    $"Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to export users";
                return RedirectToAction("UsersView");
            }
        }

        // ==================== DEPARTMENTS MANAGEMENT ====================
        [HttpGet]
        public IActionResult GetDepartments()
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM departments WHERE dept_is_active = 1 ORDER BY dept_name";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var departments = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            departments.Add(new
                            {
                                dept_id = row["dept_id"],
                                dept_code = row["dept_code"],
                                dept_name = row["dept_name"],
                                dept_description = row["dept_description"]
                            });
                        }
                        
                        return Json(new { success = true, data = departments });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateDepartment([FromBody] DepartmentModel department)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO departments (dept_code, dept_name, dept_description)
                        VALUES (@Code, @Name, @Description);
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", department.dept_code);
                        cmd.Parameters.AddWithValue("@Name", department.dept_name);
                        cmd.Parameters.AddWithValue("@Description", (object?)department.dept_description ?? DBNull.Value);
                        
                        int deptId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Department", "Department", deptId, $"Created department {department.dept_name}");
                        
                        return Json(new { success = true, message = "Department created successfully", deptId = deptId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Update Department
        [HttpPost]
        public IActionResult UpdateDepartment(int deptId, string deptCode, string deptName, string? deptDescription)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            if (string.IsNullOrWhiteSpace(deptCode) || string.IsNullOrWhiteSpace(deptName))
            {
                return Json(new { success = false, message = "Department code and name are required" });
            }
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Check if department exists
                    string checkQuery = "SELECT dept_code, dept_name, dept_description FROM departments WHERE dept_id = @DeptId";
                    string oldValues = "";
                    
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@DeptId", deptId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Department not found" });
                            }
                            oldValues = $"Code: {reader["dept_code"]}, Name: {reader["dept_name"]}, Description: {reader["dept_description"]}";
                        }
                    }
                    
                    // Check if new code already exists for different department
                    string duplicateQuery = "SELECT COUNT(*) FROM departments WHERE dept_code = @DeptCode AND dept_id != @DeptId";
                    using (SqlCommand duplicateCmd = new SqlCommand(duplicateQuery, conn))
                    {
                        duplicateCmd.Parameters.AddWithValue("@DeptCode", deptCode);
                        duplicateCmd.Parameters.AddWithValue("@DeptId", deptId);
                        int count = (int)duplicateCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return Json(new { success = false, message = "Department code already exists" });
                        }
                    }
                    
                    // Update department
                    string updateQuery = @"
                        UPDATE departments 
                        SET dept_code = @DeptCode,
                            dept_name = @DeptName,
                            dept_description = @DeptDescription,
                            dept_updated_at = GETDATE()
                        WHERE dept_id = @DeptId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@DeptId", deptId);
                        cmd.Parameters.AddWithValue("@DeptCode", deptCode);
                        cmd.Parameters.AddWithValue("@DeptName", deptName);
                        cmd.Parameters.AddWithValue("@DeptDescription", (object?)deptDescription ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                        
                        string newValues = $"Code: {deptCode}, Name: {deptName}, Description: {deptDescription}";
                        LogActivity("Update", "Department", deptId, $"Updated department: {deptName}", oldValues, newValues);
                        
                        return Json(new { success = true, message = "Department updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Delete Department (Soft Delete)
        [HttpPost]
        public IActionResult DeleteDepartment(int deptId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Check if department has active users
                    string checkUsersQuery = "SELECT COUNT(*) FROM users WHERE user_dept_id = @DeptId AND user_is_active = 1";
                    using (SqlCommand checkCmd = new SqlCommand(checkUsersQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@DeptId", deptId);
                        int userCount = (int)checkCmd.ExecuteScalar();
                        
                        if (userCount > 0)
                        {
                            return Json(new { success = false, message = $"Cannot delete department. {userCount} active user(s) are assigned to this department." });
                        }
                    }
                    
                    // Get department name for logging
                    string getDeptQuery = "SELECT dept_name FROM departments WHERE dept_id = @DeptId";
                    string deptName = "";
                    using (SqlCommand getCmd = new SqlCommand(getDeptQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@DeptId", deptId);
                        deptName = getCmd.ExecuteScalar()?.ToString() ?? "";
                    }
                    
                    // Soft delete department
                    string deleteQuery = @"
                        UPDATE departments 
                        SET dept_is_active = 0,
                            dept_updated_at = GETDATE()
                        WHERE dept_id = @DeptId";
                    
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@DeptId", deptId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            LogActivity("Delete", "Department", deptId, $"Deleted department: {deptName}");
                            return Json(new { success = true, message = "Department deleted successfully" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Department not found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Toggle Department Active Status
        [HttpPost]
        public IActionResult ToggleDepartmentStatus(int deptId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Get current status and name
                    string getQuery = "SELECT dept_name, dept_is_active FROM departments WHERE dept_id = @DeptId";
                    string deptName = "";
                    bool currentStatus = false;
                    
                    using (SqlCommand getCmd = new SqlCommand(getQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@DeptId", deptId);
                        using (SqlDataReader reader = getCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Department not found" });
                            }
                            deptName = reader["dept_name"].ToString() ?? "";
                            currentStatus = (bool)reader["dept_is_active"];
                        }
                    }
                    
                    // If activating, check if users will be affected
                    if (!currentStatus)
                    {
                        // Activating department - no issues
                    }
                    else
                    {
                        // Deactivating department - check for active users
                        string checkUsersQuery = "SELECT COUNT(*) FROM users WHERE user_dept_id = @DeptId AND user_is_active = 1";
                        using (SqlCommand checkCmd = new SqlCommand(checkUsersQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@DeptId", deptId);
                            int userCount = (int)checkCmd.ExecuteScalar();
                            
                            if (userCount > 0)
                            {
                                return Json(new { success = false, message = $"Cannot deactivate department. {userCount} active user(s) are assigned to this department." });
                            }
                        }
                    }
                    
                    // Toggle status
                    string updateQuery = @"
                        UPDATE departments 
                        SET dept_is_active = @NewStatus,
                            dept_updated_at = GETDATE()
                        WHERE dept_id = @DeptId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        bool newStatus = !currentStatus;
                        cmd.Parameters.AddWithValue("@DeptId", deptId);
                        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                        
                        cmd.ExecuteNonQuery();
                        
                        string action = newStatus ? "Activated" : "Deactivated";
                        LogActivity("Toggle Status", "Department", deptId, $"{action} department: {deptName}");
                        
                        return Json(new { 
                            success = true, 
                            message = $"Department {action.ToLower()} successfully",
                            newStatus = newStatus
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== ROOMS MANAGEMENT ====================
        [HttpGet]
        public IActionResult RoomsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetRooms(string? search = null, string? status = null, int? locationId = null, 
            int? minCapacity = null, int? maxCapacity = null, bool? hasProjector = null, 
            bool? hasSmartScreen = null, bool? hasScreenbeam = null, bool? hasCiscoBar = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT r.*, l.location_code, l.location_plant_name, l.location_block, l.location_floor
                        FROM rooms r
                        INNER JOIN locations l ON r.room_location_id = l.location_id
                        WHERE r.room_is_active = 1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (!string.IsNullOrEmpty(search))
                    {
                        query.Append(" AND (r.room_name LIKE @Search OR r.room_code LIKE @Search OR r.room_description LIKE @Search)");
                        parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                    }
                    
                    if (!string.IsNullOrEmpty(status))
                    {
                        query.Append(" AND r.room_operational_status = @Status");
                        parameters.Add(new SqlParameter("@Status", status));
                    }
                    
                    if (locationId.HasValue)
                    {
                        query.Append(" AND r.room_location_id = @LocationId");
                        parameters.Add(new SqlParameter("@LocationId", locationId.Value));
                    }
                    
                    if (minCapacity.HasValue)
                    {
                        query.Append(" AND r.room_capacity >= @MinCapacity");
                        parameters.Add(new SqlParameter("@MinCapacity", minCapacity.Value));
                    }
                    
                    if (maxCapacity.HasValue)
                    {
                        query.Append(" AND r.room_capacity <= @MaxCapacity");
                        parameters.Add(new SqlParameter("@MaxCapacity", maxCapacity.Value));
                    }
                    
                    if (hasProjector.HasValue)
                    {
                        query.Append(" AND r.room_has_projector = @HasProjector");
                        parameters.Add(new SqlParameter("@HasProjector", hasProjector.Value));
                    }
                    
                    if (hasSmartScreen.HasValue)
                    {
                        query.Append(" AND r.room_has_smart_screen = @HasSmartScreen");
                        parameters.Add(new SqlParameter("@HasSmartScreen", hasSmartScreen.Value));
                    }
                    
                    if (hasScreenbeam.HasValue)
                    {
                        query.Append(" AND r.room_has_screenbeam = @HasScreenbeam");
                        parameters.Add(new SqlParameter("@HasScreenbeam", hasScreenbeam.Value));
                    }
                    
                    if (hasCiscoBar.HasValue)
                    {
                        query.Append(" AND r.room_has_cisco_bar = @HasCiscoBar");
                        parameters.Add(new SqlParameter("@HasCiscoBar", hasCiscoBar.Value));
                    }
                    
                    query.Append(" ORDER BY r.room_name");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var rooms = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            rooms.Add(new
                            {
                                room_id = row["room_id"],
                                room_code = row["room_code"],
                                room_name = row["room_name"],
                                room_capacity = row["room_capacity"],
                                room_operational_status = row["room_operational_status"],
                                room_description = row["room_description"],
                                room_has_projector = row["room_has_projector"],
                                room_has_smart_screen = row["room_has_smart_screen"],
                                room_has_screenbeam = row["room_has_screenbeam"],
                                room_has_cisco_bar = row["room_has_cisco_bar"],
                                room_other_facilities = row["room_other_facilities"],
                                location_code = row["location_code"],
                                location_plant_name = row["location_plant_name"],
                                location_block = row["location_block"],
                                location_floor = row["location_floor"],
                                facilities = new
                                {
                                    projector = Convert.ToBoolean(row["room_has_projector"]),
                                    smart_screen = Convert.ToBoolean(row["room_has_smart_screen"]),
                                    screenbeam = Convert.ToBoolean(row["room_has_screenbeam"]),
                                    cisco_bar = Convert.ToBoolean(row["room_has_cisco_bar"])
                                }
                            });
                        }
                        
                        return Json(new { success = true, data = rooms });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateRoom([FromBody] RoomModel room)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Get location details for room code generation
                    string locationQuery = "SELECT location_plant_name, location_block, location_floor FROM locations WHERE location_id = @LocationId";
                    string plantName = "";
                    int block = 0, floor = 0;
                    
                    using (SqlCommand cmd = new SqlCommand(locationQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@LocationId", room.room_location_id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                plantName = reader["location_plant_name"].ToString();
                                block = Convert.ToInt32(reader["location_block"]);
                                floor = Convert.ToInt32(reader["location_floor"]);
                            }
                        }
                    }
                    
                    // Generate room code
                    room.room_code = AbrvHelperFunction.GenerateRoomCode(room.room_name, plantName, block, floor);
                    
                    string query = @"
                        INSERT INTO rooms (
                            room_code, room_name, room_location_id, room_capacity, room_description,
                            room_has_projector, room_has_smart_screen, room_has_screenbeam, room_has_cisco_bar,
                            room_other_facilities, room_operational_status
                        ) VALUES (
                            @Code, @Name, @LocationId, @Capacity, @Description,
                            @HasProjector, @HasSmartScreen, @HasScreenbeam, @HasCiscoBar,
                            @OtherFacilities, @Status
                        );
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", room.room_code);
                        cmd.Parameters.AddWithValue("@Name", room.room_name);
                        cmd.Parameters.AddWithValue("@LocationId", room.room_location_id);
                        cmd.Parameters.AddWithValue("@Capacity", room.room_capacity);
                        cmd.Parameters.AddWithValue("@Description", (object?)room.room_description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HasProjector", room.room_has_projector);
                        cmd.Parameters.AddWithValue("@HasSmartScreen", room.room_has_smart_screen);
                        cmd.Parameters.AddWithValue("@HasScreenbeam", room.room_has_screenbeam);
                        cmd.Parameters.AddWithValue("@HasCiscoBar", room.room_has_cisco_bar);
                        cmd.Parameters.AddWithValue("@OtherFacilities", (object?)room.room_other_facilities ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", room.room_operational_status);
                        
                        int roomId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Room", "Room", roomId, $"Created room {room.room_name} ({room.room_code})");
                        
                        return Json(new { success = true, message = "Room created successfully", roomId = roomId, roomCode = room.room_code });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateRoomStatus(int roomId, string status)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE rooms SET room_operational_status = @Status, room_updated_at = GETDATE() WHERE room_id = @RoomId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.Parameters.AddWithValue("@Status", status);
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Update Room Status", "Room", roomId, $"Updated room status to {status}");
                        
                        return Json(new { success = true, message = "Room status updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== LOCATIONS MANAGEMENT ====================
        [HttpGet]
        public IActionResult GetLocations()
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM locations WHERE location_is_active = 1 ORDER BY location_plant_name, location_block, location_floor";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var locations = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            locations.Add(new
                            {
                                location_id = row["location_id"],
                                location_code = row["location_code"],
                                location_plant_name = row["location_plant_name"],
                                location_block = row["location_block"],
                                location_floor = row["location_floor"],
                                location_description = row["location_description"],
                                display_name = $"{row["location_plant_name"]} - Block {row["location_block"]} - Floor {row["location_floor"]}"
                            });
                        }
                        
                        return Json(new { success = true, data = locations });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateLocation([FromBody] LocationModel location)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                // Generate location code
                location.location_code = $"Plant{location.location_plant_name.Split(' ').LastOrDefault()}-{location.location_block}-{location.location_floor}";
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO locations (location_code, location_plant_name, location_block, location_floor, location_description)
                        VALUES (@Code, @PlantName, @Block, @Floor, @Description);
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", location.location_code);
                        cmd.Parameters.AddWithValue("@PlantName", location.location_plant_name);
                        cmd.Parameters.AddWithValue("@Block", location.location_block);
                        cmd.Parameters.AddWithValue("@Floor", location.location_floor);
                        cmd.Parameters.AddWithValue("@Description", (object?)location.location_description ?? DBNull.Value);
                        
                        int locationId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Location", "Location", locationId, $"Created location {location.location_code}");
                        
                        return Json(new { success = true, message = "Location created successfully", locationId = locationId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Update Location
        [HttpPost]
        public IActionResult UpdateLocation(int locationId, string locationCode, string locationPlantName, 
            byte locationBlock, byte locationFloor, string? locationDescription)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            if (string.IsNullOrWhiteSpace(locationCode) || string.IsNullOrWhiteSpace(locationPlantName))
            {
                return Json(new { success = false, message = "Location code and plant name are required" });
            }
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Check if location exists
                    string checkQuery = @"
                        SELECT location_code, location_plant_name, location_block, 
                               location_floor, location_description 
                        FROM locations 
                        WHERE location_id = @LocationId";
                    string oldValues = "";
                    
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@LocationId", locationId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Location not found" });
                            }
                            oldValues = $"Code: {reader["location_code"]}, Plant: {reader["location_plant_name"]}, " +
                                      $"Block: {reader["location_block"]}, Floor: {reader["location_floor"]}, " +
                                      $"Description: {reader["location_description"]}";
                        }
                    }
                    
                    // Check if new code already exists for different location
                    string duplicateQuery = "SELECT COUNT(*) FROM locations WHERE location_code = @LocationCode AND location_id != @LocationId";
                    using (SqlCommand duplicateCmd = new SqlCommand(duplicateQuery, conn))
                    {
                        duplicateCmd.Parameters.AddWithValue("@LocationCode", locationCode);
                        duplicateCmd.Parameters.AddWithValue("@LocationId", locationId);
                        int count = (int)duplicateCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return Json(new { success = false, message = "Location code already exists" });
                        }
                    }
                    
                    // Update location
                    string updateQuery = @"
                        UPDATE locations 
                        SET location_code = @LocationCode,
                            location_plant_name = @LocationPlantName,
                            location_block = @LocationBlock,
                            location_floor = @LocationFloor,
                            location_description = @LocationDescription,
                            location_updated_at = GETDATE()
                        WHERE location_id = @LocationId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        cmd.Parameters.AddWithValue("@LocationCode", locationCode);
                        cmd.Parameters.AddWithValue("@LocationPlantName", locationPlantName);
                        cmd.Parameters.AddWithValue("@LocationBlock", locationBlock);
                        cmd.Parameters.AddWithValue("@LocationFloor", locationFloor);
                        cmd.Parameters.AddWithValue("@LocationDescription", (object?)locationDescription ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                        
                        string newValues = $"Code: {locationCode}, Plant: {locationPlantName}, " +
                                         $"Block: {locationBlock}, Floor: {locationFloor}, " +
                                         $"Description: {locationDescription}";
                        LogActivity("Update", "Location", locationId, $"Updated location: {locationPlantName} - Block {locationBlock} - Floor {locationFloor}", oldValues, newValues);
                        
                        return Json(new { success = true, message = "Location updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Delete Location (Soft Delete)
        [HttpPost]
        public IActionResult DeleteLocation(int locationId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Check if location has active rooms
                    string checkRoomsQuery = "SELECT COUNT(*) FROM rooms WHERE room_location_id = @LocationId AND room_is_active = 1";
                    using (SqlCommand checkCmd = new SqlCommand(checkRoomsQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@LocationId", locationId);
                        int roomCount = (int)checkCmd.ExecuteScalar();
                        
                        if (roomCount > 0)
                        {
                            return Json(new { success = false, message = $"Cannot delete location. {roomCount} active room(s) are assigned to this location." });
                        }
                    }
                    
                    // Get location details for logging
                    string getLocationQuery = @"
                        SELECT location_plant_name, location_block, location_floor 
                        FROM locations 
                        WHERE location_id = @LocationId";
                    string locationName = "";
                    using (SqlCommand getCmd = new SqlCommand(getLocationQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@LocationId", locationId);
                        using (SqlDataReader reader = getCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                locationName = $"{reader["location_plant_name"]} - Block {reader["location_block"]} - Floor {reader["location_floor"]}";
                            }
                        }
                    }
                    
                    // Soft delete location
                    string deleteQuery = @"
                        UPDATE locations 
                        SET location_is_active = 0,
                            location_updated_at = GETDATE()
                        WHERE location_id = @LocationId";
                    
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            LogActivity("Delete", "Location", locationId, $"Deleted location: {locationName}");
                            return Json(new { success = true, message = "Location deleted successfully" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Location not found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Toggle Location Active Status
        [HttpPost]
        public IActionResult ToggleLocationStatus(int locationId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Get current status and details
                    string getQuery = @"
                        SELECT location_plant_name, location_block, location_floor, location_is_active 
                        FROM locations 
                        WHERE location_id = @LocationId";
                    string locationName = "";
                    bool currentStatus = false;
                    
                    using (SqlCommand getCmd = new SqlCommand(getQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@LocationId", locationId);
                        using (SqlDataReader reader = getCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Location not found" });
                            }
                            locationName = $"{reader["location_plant_name"]} - Block {reader["location_block"]} - Floor {reader["location_floor"]}";
                            currentStatus = (bool)reader["location_is_active"];
                        }
                    }
                    
                    // If deactivating, check for active rooms
                    if (currentStatus)
                    {
                        string checkRoomsQuery = "SELECT COUNT(*) FROM rooms WHERE room_location_id = @LocationId AND room_is_active = 1";
                        using (SqlCommand checkCmd = new SqlCommand(checkRoomsQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@LocationId", locationId);
                            int roomCount = (int)checkCmd.ExecuteScalar();
                            
                            if (roomCount > 0)
                            {
                                return Json(new { success = false, message = $"Cannot deactivate location. {roomCount} active room(s) are assigned to this location." });
                            }
                        }
                    }
                    
                    // Toggle status
                    string updateQuery = @"
                        UPDATE locations 
                        SET location_is_active = @NewStatus,
                            location_updated_at = GETDATE()
                        WHERE location_id = @LocationId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        bool newStatus = !currentStatus;
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                        
                        cmd.ExecuteNonQuery();
                        
                        string action = newStatus ? "Activated" : "Deactivated";
                        LogActivity("Toggle Status", "Location", locationId, $"{action} location: {locationName}");
                        
                        return Json(new { 
                            success = true, 
                            message = $"Location {action.ToLower()} successfully",
                            newStatus = newStatus
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Controllers\AdminController.cs (Part 2 - Bookings, Feedbacks, Notifications, Activity Logs, Profile)

        // ==================== BOOKINGS MANAGEMENT ====================
        [HttpGet]
        public IActionResult BookingsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetBookings(string? search = null, string? status = null, int? roomId = null, 
            int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT b.*, u.user_full_name, u.user_employee_id, r.room_name, r.room_code
                        FROM bookings b
                        INNER JOIN users u ON b.booking_user_id = u.user_id
                        INNER JOIN rooms r ON b.booking_room_id = r.room_id
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (!string.IsNullOrEmpty(search))
                    {
                        query.Append(" AND (b.booking_meeting_title LIKE @Search OR b.booking_code LIKE @Search OR u.user_full_name LIKE @Search)");
                        parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                    }
                    
                    if (!string.IsNullOrEmpty(status))
                    {
                        query.Append(" AND b.booking_status = @Status");
                        parameters.Add(new SqlParameter("@Status", status));
                    }
                    
                    if (roomId.HasValue)
                    {
                        query.Append(" AND b.booking_room_id = @RoomId");
                        parameters.Add(new SqlParameter("@RoomId", roomId.Value));
                    }
                    
                    if (userId.HasValue)
                    {
                        query.Append(" AND b.booking_user_id = @UserId");
                        parameters.Add(new SqlParameter("@UserId", userId.Value));
                    }
                    
                    if (fromDate.HasValue)
                    {
                        query.Append(" AND b.booking_date >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
                    }
                    
                    if (toDate.HasValue)
                    {
                        query.Append(" AND b.booking_date <= @ToDate");
                        parameters.Add(new SqlParameter("@ToDate", toDate.Value));
                    }
                    
                    query.Append(" ORDER BY b.booking_date DESC, b.booking_start_time DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var bookings = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            bookings.Add(new
                            {
                                booking_id = row["booking_id"],
                                booking_code = row["booking_code"],
                                booking_meeting_title = row["booking_meeting_title"],
                                booking_date = Convert.ToDateTime(row["booking_date"]).ToString("yyyy-MM-dd"),
                                booking_start_time = TimeSpan.Parse(row["booking_start_time"].ToString()).ToString(@"hh\:mm"),
                                booking_end_time = TimeSpan.Parse(row["booking_end_time"].ToString()).ToString(@"hh\:mm"),
                                booking_status = row["booking_status"],
                                user_full_name = row["user_full_name"],
                                user_employee_id = row["user_employee_id"],
                                room_name = row["room_name"],
                                room_code = row["room_code"],
                                booking_duration_minutes = row["booking_duration_minutes"]
                            });
                        }
                        
                        return Json(new { success = true, data = bookings });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateBooking([FromBody] BookingModel booking)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                // Check for conflicts
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    using (SqlCommand cmd = new SqlCommand("sp_check_room_availability", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@room_id", booking.booking_room_id);
                        cmd.Parameters.AddWithValue("@booking_date", booking.booking_date);
                        cmd.Parameters.AddWithValue("@start_time", booking.booking_start_time);
                        cmd.Parameters.AddWithValue("@end_time", booking.booking_end_time);
                        cmd.Parameters.AddWithValue("@exclude_booking_id", DBNull.Value);
                        
                        int conflictCount = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (conflictCount > 0)
                        {
                            return Json(new { success = false, message = "Room is already booked for this time slot" });
                        }
                    }
                    
                    // Generate booking code
                    string bookingCodeQuery = "SELECT ISNULL(MAX(booking_id), 0) + 1 FROM bookings";
                    int nextId = 1;
                    using (SqlCommand cmd = new SqlCommand(bookingCodeQuery, conn))
                    {
                        nextId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    booking.booking_code = AbrvHelperFunction.GenerateBookingCode(booking.booking_date, nextId);
                    
                    // Create booking
                    string query = @"
                        INSERT INTO bookings (
                            booking_code, booking_user_id, booking_room_id, booking_meeting_title,
                            booking_meeting_description, booking_date, booking_start_time, booking_end_time,
                            booking_status
                        ) VALUES (
                            @Code, @UserId, @RoomId, @Title,
                            @Description, @Date, @StartTime, @EndTime,
                            @Status
                        );
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", booking.booking_code);
                        cmd.Parameters.AddWithValue("@UserId", booking.booking_user_id);
                        cmd.Parameters.AddWithValue("@RoomId", booking.booking_room_id);
                        cmd.Parameters.AddWithValue("@Title", booking.booking_meeting_title);
                        cmd.Parameters.AddWithValue("@Description", (object?)booking.booking_meeting_description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Date", booking.booking_date);
                        cmd.Parameters.AddWithValue("@StartTime", booking.booking_start_time);
                        cmd.Parameters.AddWithValue("@EndTime", booking.booking_end_time);
                        cmd.Parameters.AddWithValue("@Status", "Confirmed"); // Admin bookings auto-confirm
                        
                        int bookingId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Booking", "Booking", bookingId, $"Created booking {booking.booking_code} for room");
                        
                        return Json(new { success = true, message = "Booking created successfully", bookingId = bookingId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateBookingStatus(int bookingId, string status, string? reason = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string query = "";
                    if (status == "Cancelled")
                    {
                        query = @"
                            UPDATE bookings SET 
                                booking_status = @Status,
                                booking_cancellation_reason = @Reason,
                                booking_cancelled_at = GETDATE(),
                                booking_cancelled_by = @UserId,
                                booking_updated_at = GETDATE()
                            WHERE booking_id = @BookingId";
                    }
                    else
                    {
                        query = @"
                            UPDATE bookings SET 
                                booking_status = @Status,
                                booking_updated_at = GETDATE()
                            WHERE booking_id = @BookingId";
                    }
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@UserId", HttpContext.Session.GetInt32("UserId"));
                        
                        if (status == "Cancelled")
                        {
                            cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
                        }
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Update Booking Status", "Booking", bookingId, $"Updated booking status to {status}");
                        
                        return Json(new { success = true, message = $"Booking {status.ToLower()} successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCalendarBookings(DateTime start, DateTime end)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            b.booking_id,
                            b.booking_code,
                            b.booking_meeting_title,
                            b.booking_date,
                            b.booking_start_time,
                            b.booking_end_time,
                            b.booking_status,
                            r.room_name,
                            u.user_full_name,
                            CASE 
                                WHEN b.booking_status = 'Confirmed' THEN '#28a745'
                                WHEN b.booking_status = 'Pending' THEN '#ffc107'
                                WHEN b.booking_status = 'In Progress' THEN '#17a2b8'
                                WHEN b.booking_status = 'Cancelled' THEN '#dc3545'
                                ELSE '#6c757d'
                            END as color
                        FROM bookings b
                        INNER JOIN rooms r ON b.booking_room_id = r.room_id
                        INNER JOIN users u ON b.booking_user_id = u.user_id
                        WHERE b.booking_date BETWEEN @StartDate AND @EndDate
                            AND b.booking_status != 'Cancelled'
                        ORDER BY b.booking_date, b.booking_start_time";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var events = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            DateTime startDateTime = Convert.ToDateTime(row["booking_date"]).Add(TimeSpan.Parse(row["booking_start_time"].ToString()));
                            DateTime endDateTime = Convert.ToDateTime(row["booking_date"]).Add(TimeSpan.Parse(row["booking_end_time"].ToString()));
                            
                            events.Add(new
                            {
                                id = row["booking_id"],
                                title = $"{row["room_name"]}: {row["booking_meeting_title"]}",
                                start = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                                end = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                                color = row["color"],
                                extendedProps = new
                                {
                                    code = row["booking_code"],
                                    room = row["room_name"],
                                    user = row["user_full_name"],
                                    status = row["booking_status"]
                                }
                            });
                        }
                        
                        return Json(new { success = true, data = events });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== FEEDBACKS MANAGEMENT ====================
        [HttpGet]
        public IActionResult FeedbacksView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetFeedbacks(int? roomId = null, int? userId = null, DateTime? fromDate = null, 
            DateTime? toDate = null, byte? minRating = null, byte? maxRating = null, bool? responded = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT f.*, u.user_full_name, r.room_name, r.room_code, b.booking_date,
                               admin.user_full_name as admin_responder_name
                        FROM feedbacks f
                        INNER JOIN users u ON f.feedback_user_id = u.user_id
                        INNER JOIN rooms r ON f.feedback_room_id = r.room_id
                        INNER JOIN bookings b ON f.feedback_booking_id = b.booking_id
                        LEFT JOIN users admin ON f.feedback_admin_responded_by = admin.user_id
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (roomId.HasValue)
                    {
                        query.Append(" AND f.feedback_room_id = @RoomId");
                        parameters.Add(new SqlParameter("@RoomId", roomId.Value));
                    }
                    
                    if (userId.HasValue)
                    {
                        query.Append(" AND f.feedback_user_id = @UserId");
                        parameters.Add(new SqlParameter("@UserId", userId.Value));
                    }
                    
                    if (fromDate.HasValue)
                    {
                        query.Append(" AND b.booking_date >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
                    }
                    
                    if (toDate.HasValue)
                    {
                        query.Append(" AND b.booking_date <= @ToDate");
                        parameters.Add(new SqlParameter("@ToDate", toDate.Value));
                    }
                    
                    if (minRating.HasValue)
                    {
                        query.Append(" AND f.feedback_rating >= @MinRating");
                        parameters.Add(new SqlParameter("@MinRating", minRating.Value));
                    }
                    
                    if (maxRating.HasValue)
                    {
                        query.Append(" AND f.feedback_rating <= @MaxRating");
                        parameters.Add(new SqlParameter("@MaxRating", maxRating.Value));
                    }
                    
                    if (responded.HasValue)
                    {
                        if (responded.Value)
                        {
                            query.Append(" AND f.feedback_admin_response IS NOT NULL");
                        }
                        else
                        {
                            query.Append(" AND f.feedback_admin_response IS NULL");
                        }
                    }
                    
                    query.Append(" ORDER BY f.feedback_created_at DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var feedbacks = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            feedbacks.Add(new
                            {
                                feedback_id = row["feedback_id"],
                                feedback_rating = row["feedback_rating"],
                                feedback_room_condition = row["feedback_room_condition"],
                                feedback_facility_condition = row["feedback_facility_condition"],
                                feedback_issues_reported = row["feedback_issues_reported"],
                                feedback_admin_response = row["feedback_admin_response"],
                                user_full_name = row["user_full_name"],
                                room_name = row["room_name"],
                                room_code = row["room_code"],
                                booking_date = Convert.ToDateTime(row["booking_date"]).ToString("yyyy-MM-dd"),
                                admin_responder_name = row["admin_responder_name"],
                                feedback_admin_responded_at = row["feedback_admin_responded_at"],
                                has_admin_response = !string.IsNullOrEmpty(row["feedback_admin_response"]?.ToString())
                            });
                        }
                        
                        return Json(new { success = true, data = feedbacks });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RespondToFeedback(int feedbackId, string response)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE feedbacks SET 
                            feedback_admin_response = @Response,
                            feedback_admin_responded_by = @AdminId,
                            feedback_admin_responded_at = GETDATE(),
                            feedback_updated_at = GETDATE()
                        WHERE feedback_id = @FeedbackId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FeedbackId", feedbackId);
                        cmd.Parameters.AddWithValue("@Response", response);
                        cmd.Parameters.AddWithValue("@AdminId", HttpContext.Session.GetInt32("UserId"));
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Respond to Feedback", "Feedback", feedbackId, "Responded to feedback");
                        
                        return Json(new { success = true, message = "Response saved successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== NOTIFICATIONS MANAGEMENT ====================
        [HttpGet]
        public IActionResult NotificationsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetNotifications(string? type = null, string? priority = null, bool? isRead = null, 
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT n.*, creator.user_full_name as creator_name
                        FROM notifications n
                        INNER JOIN users creator ON n.notification_created_by = creator.user_id
                        WHERE (n.notification_target_role = 'Admin' OR n.notification_target_role = 'All' 
                               OR n.notification_target_user_id = @UserId)
                        AND 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", HttpContext.Session.GetInt32("UserId")));
                    
                    if (!string.IsNullOrEmpty(type))
                    {
                        query.Append(" AND n.notification_type = @Type");
                        parameters.Add(new SqlParameter("@Type", type));
                    }
                    
                    if (!string.IsNullOrEmpty(priority))
                    {
                        query.Append(" AND n.notification_priority = @Priority");
                        parameters.Add(new SqlParameter("@Priority", priority));
                    }
                    
                    if (isRead.HasValue)
                    {
                        query.Append(" AND n.notification_is_read = @IsRead");
                        parameters.Add(new SqlParameter("@IsRead", isRead.Value));
                    }
                    
                    if (fromDate.HasValue)
                    {
                        query.Append(" AND n.notification_created_at >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
                    }
                    
                    if (toDate.HasValue)
                    {
                        query.Append(" AND n.notification_created_at <= @ToDate");
                        parameters.Add(new SqlParameter("@ToDate", toDate.Value));
                    }
                    
                    query.Append(" ORDER BY n.notification_created_at DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var notifications = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            notifications.Add(new
                            {
                                notification_id = row["notification_id"],
                                notification_type = row["notification_type"],
                                notification_title = row["notification_title"],
                                notification_message = row["notification_message"],
                                notification_priority = row["notification_priority"],
                                notification_is_read = Convert.ToBoolean(row["notification_is_read"]),
                                notification_read_at = row["notification_read_at"],
                                notification_created_at = Convert.ToDateTime(row["notification_created_at"]).ToString("yyyy-MM-dd HH:mm"),
                                creator_name = row["creator_name"],
                                priority_class = row["notification_priority"].ToString().ToLower()
                            });
                        }
                        
                        return Json(new { success = true, data = notifications });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateNotification([FromBody] NotificationModel notification)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO notifications (
                            notification_type, notification_title, notification_message,
                            notification_target_role, notification_priority, notification_created_by
                        ) VALUES (
                            @Type, @Title, @Message,
                            @TargetRole, @Priority, @CreatedBy
                        );
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Type", notification.notification_type);
                        cmd.Parameters.AddWithValue("@Title", notification.notification_title);
                        cmd.Parameters.AddWithValue("@Message", notification.notification_message);
                        cmd.Parameters.AddWithValue("@TargetRole", (object?)notification.notification_target_role ?? "All");
                        cmd.Parameters.AddWithValue("@Priority", notification.notification_priority);
                        cmd.Parameters.AddWithValue("@CreatedBy", HttpContext.Session.GetInt32("UserId"));
                        
                        int notificationId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Notification", "Notification", notificationId, $"Created notification: {notification.notification_title}");
                        
                        return Json(new { success = true, message = "Notification created successfully", notificationId = notificationId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MarkNotificationRead(int notificationId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_mark_notification_read", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@notification_id", notificationId);
                        cmd.Parameters.AddWithValue("@user_id", HttpContext.Session.GetInt32("UserId"));
                        
                        cmd.ExecuteNonQuery();
                        
                        return Json(new { success = true, message = "Notification marked as read" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MarkAllNotificationsRead()
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE notifications 
                        SET notification_is_read = 1, notification_read_at = GETDATE()
                        WHERE notification_target_role IN ('Admin', 'All') 
                            OR notification_target_user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", HttpContext.Session.GetInt32("UserId"));
                        cmd.ExecuteNonQuery();
                        
                        return Json(new { success = true, message = "All notifications marked as read" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== ACTIVITY LOGS ====================
        [HttpGet]
        public IActionResult ActivityLogsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetActivityLogs(string? actionType = null, string? entityType = null, 
            int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT al.*, u.user_full_name, u.user_email
                        FROM activity_logs al
                        INNER JOIN users u ON al.log_user_id = u.user_id
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (!string.IsNullOrEmpty(actionType))
                    {
                        query.Append(" AND al.log_action_type = @ActionType");
                        parameters.Add(new SqlParameter("@ActionType", actionType));
                    }
                    
                    if (!string.IsNullOrEmpty(entityType))
                    {
                        query.Append(" AND al.log_entity_type = @EntityType");
                        parameters.Add(new SqlParameter("@EntityType", entityType));
                    }
                    
                    if (userId.HasValue)
                    {
                        query.Append(" AND al.log_user_id = @UserId");
                        parameters.Add(new SqlParameter("@UserId", userId.Value));
                    }
                    
                    if (fromDate.HasValue)
                    {
                        query.Append(" AND al.log_created_at >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
                    }
                    
                    if (toDate.HasValue)
                    {
                        query.Append(" AND al.log_created_at <= @ToDate");
                        parameters.Add(new SqlParameter("@ToDate", toDate.Value));
                    }
                    
                    query.Append(" ORDER BY al.log_created_at DESC");
                    
                    using (SqlCommand cmd = new SqlCommand(query.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var logs = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            logs.Add(new
                            {
                                log_id = row["log_id"],
                                log_action_type = row["log_action_type"],
                                log_entity_type = row["log_entity_type"],
                                log_entity_id = row["log_entity_id"],
                                log_description = row["log_description"],
                                user_full_name = row["user_full_name"],
                                user_email = row["user_email"],
                                log_created_at = Convert.ToDateTime(row["log_created_at"]).ToString("yyyy-MM-dd HH:mm:ss")
                            });
                        }
                        
                        return Json(new { success = true, data = logs });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== PROFILE MANAGEMENT ====================
        [HttpGet]
        public IActionResult ProfileView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT u.*, d.dept_name
                        FROM users u
                        LEFT JOIN departments d ON u.user_dept_id = d.dept_id
                        WHERE u.user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new
                                {
                                    user_id = reader["user_id"],
                                    user_employee_id = reader["user_employee_id"],
                                    user_email = reader["user_email"],
                                    user_full_name = reader["user_full_name"],
                                    user_phone = reader["user_phone"],
                                    user_role = reader["user_role"],
                                    dept_name = reader["dept_name"],
                                    user_profile_photo = reader["user_profile_photo"],
                                    user_last_login = reader["user_last_login"]
                                };
                                
                                return View(user);
                            }
                        }
                    }
                }
            }
            catch { }
            
            return View();
        }

        [HttpPost]
        public IActionResult UpdateProfile([FromBody] dynamic profileData)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                string fullName = profileData.user_full_name;
                string phone = profileData.user_phone;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE users SET 
                            user_full_name = @FullName,
                            user_phone = @Phone,
                            user_updated_at = GETDATE()
                        WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                        
                        // Update session
                        HttpContext.Session.SetString("UserFullName", fullName);
                        
                        LogActivity("Update Profile", "User", userId, "Updated profile information");
                        
                        return Json(new { success = true, message = "Profile updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Verify current password
                    string verifyQuery = "SELECT user_password FROM users WHERE user_id = @UserId";
                    string storedHash = "";
                    
                    using (SqlCommand cmd = new SqlCommand(verifyQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        storedHash = cmd.ExecuteScalar()?.ToString() ?? "";
                    }
                    
                    string currentHash = SecHelperFunction.HashPasswordMD5(currentPassword);
                    
                    if (!SecHelperFunction.VerifyPassword(currentHash, storedHash))
                    {
                        return Json(new { success = false, message = "Current password is incorrect" });
                    }
                    
                    // Update password
                    string newHash = SecHelperFunction.HashPasswordMD5(newPassword);
                    string updateQuery = "UPDATE users SET user_password = @NewPassword, user_updated_at = GETDATE() WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@NewPassword", newHash);
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Change Password", "User", userId, "Changed password");
                        
                        return Json(new { success = true, message = "Password changed successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UploadProfilePhoto(IFormFile file)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                // Generate unique filename
                string fileName = $"profile_{userId}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }
                
                string filePath = Path.Combine(uploadPath, fileName);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                
                // Update database
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET user_profile_photo = @Photo, user_updated_at = GETDATE() WHERE user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Photo", fileName);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                
                // Update session
                HttpContext.Session.SetString("UserProfilePhoto", fileName);
                
                LogActivity("Upload Profile Photo", "User", userId, "Uploaded profile photo");
                
                return Json(new { success = true, message = "Profile photo uploaded successfully", fileName = fileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetUnreadNotificationCount()
        {
            if (!CheckAuth()) return Json(new { success = false, count = 0 });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_get_unread_notification_count", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@user_id", HttpContext.Session.GetInt32("UserId"));
                        cmd.Parameters.AddWithValue("@user_role", HttpContext.Session.GetString("UserRole"));
                        
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return Json(new { count = count });
                    }
                }
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }
    }
}