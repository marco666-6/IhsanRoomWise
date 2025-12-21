// Controllers\AdminController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using IhsanRoomWise.Functions;
using IhsanRoomWise.Models;
using System.Data;

namespace RoomWise.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString;

        public AdminController()
        {
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        // ============================================
        // AUTHORIZATION CHECK
        // ============================================
        private bool CheckAdminAuth()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // ============================================
        // KF-04: Dashboard View
        // ============================================
        [HttpGet]
        public IActionResult DashboardView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Basic Statistics
                    ViewBag.TotalUsers = GetCount(conn, "SELECT COUNT(*) FROM users WHERE user_is_active = 1");
                    ViewBag.ActiveUsers = GetCount(conn, "SELECT COUNT(DISTINCT booking_user_id) FROM bookings WHERE booking_date >= DATEADD(day, -30, GETDATE())");
                    ViewBag.TotalRooms = GetCount(conn, "SELECT COUNT(*) FROM rooms WHERE room_is_active = 1");
                    ViewBag.TotalBookingsToday = GetCount(conn, "SELECT COUNT(*) FROM bookings WHERE booking_date = CAST(GETDATE() AS DATE)");
                    ViewBag.PendingBookings = GetCount(conn, "SELECT COUNT(*) FROM bookings WHERE booking_status = 'Pending'");
                    ViewBag.AvailableRooms = GetAvailableRoomsCount(conn);
                    ViewBag.MaintenanceRooms = GetCount(conn, "SELECT COUNT(*) FROM rooms WHERE room_status = 'Maintenance'");
                    ViewBag.ActiveBookingsNow = GetActiveBookingsCount(conn);

                    // Real-Time Room Status with current occupancy
                    ViewBag.RoomStatusList = GetRealTimeRoomStatus(conn);
                    
                    // Get unique floors for filter
                    ViewBag.Floors = GetFloors(conn);
                    
                    // Get all rooms for filter
                    ViewBag.AllRooms = GetAllRooms(conn);

                    // Today's complete schedule
                    ViewBag.TodaySchedule = GetTodaySchedule(conn);

                    // Charts data
                    ViewBag.BookingsByStatus = GetBookingsByStatus(conn);
                    ViewBag.BookingTrends = GetBookingTrends(conn);
                    ViewBag.RoomUtilization = GetRoomUtilization(conn);
                    ViewBag.PeakHours = GetPeakBookingHours(conn);

                    // Week overview
                    ViewBag.WeekOverview = GetWeekOverview(conn);

                    // Pending actions
                    ViewBag.PendingActions = GetPendingActions(conn);

                    // Top users
                    ViewBag.TopUsers = GetTopUsers(conn);
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading dashboard: " + ex.Message;
                return View();
            }
        }

        // Helper method to get available rooms count (excluding occupied and maintenance)
        private int GetAvailableRoomsCount(SqlConnection conn)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM rooms r
                WHERE r.room_is_active = 1 
                AND r.room_status = 'Available'
                AND NOT EXISTS (
                    SELECT 1 FROM bookings b
                    WHERE b.booking_room_id = r.room_id
                    AND b.booking_date = CAST(GETDATE() AS DATE)
                    AND b.booking_status IN ('Confirmed', 'InProgress')
                    AND CAST(GETDATE() AS TIME) BETWEEN b.booking_start_time AND b.booking_end_time
                )";
            return GetCount(conn, query);
        }

        // Get count of currently active bookings
        private int GetActiveBookingsCount(SqlConnection conn)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM bookings 
                WHERE booking_date = CAST(GETDATE() AS DATE)
                AND booking_status IN ('Confirmed', 'InProgress')
                AND CAST(GETDATE() AS TIME) BETWEEN booking_start_time AND booking_end_time";
            return GetCount(conn, query);
        }

        // Get real-time room status including current occupancy
        private List<dynamic> GetRealTimeRoomStatus(SqlConnection conn)
        {
            string query = @"
                WITH CurrentBookings AS (
                    SELECT 
                        booking_room_id,
                        booking_id,
                        booking_title,
                        booking_user_id,
                        booking_end_time,
                        ROW_NUMBER() OVER (PARTITION BY booking_room_id ORDER BY booking_start_time) as rn
                    FROM bookings
                    WHERE booking_date = CAST(GETDATE() AS DATE)
                    AND booking_status IN ('Confirmed', 'InProgress')
                    AND CAST(GETDATE() AS TIME) BETWEEN booking_start_time AND booking_end_time
                ),
                NextBookings AS (
                    SELECT 
                        booking_room_id,
                        booking_user_id,
                        booking_start_time,
                        ROW_NUMBER() OVER (PARTITION BY booking_room_id ORDER BY booking_start_time) as rn
                    FROM bookings
                    WHERE booking_date = CAST(GETDATE() AS DATE)
                    AND booking_status IN ('Pending', 'Confirmed')
                    AND booking_start_time > CAST(GETDATE() AS TIME)
                )
                SELECT 
                    r.room_id,
                    r.room_name,
                    r.room_code,
                    r.room_capacity,
                    r.room_status,
                    l.location_plant_name,
                    l.location_block,
                    l.location_floor,
                    cb.booking_id as current_booking_id,
                    cb.booking_title as current_booking_title,
                    CONVERT(VARCHAR(5), cb.booking_end_time, 108) as current_booking_end,
                    u1.user_full_name as current_user_name,
                    CONVERT(VARCHAR(5), nb.booking_start_time, 108) as next_booking_start,
                    u2.user_full_name as next_user_name
                FROM rooms r
                INNER JOIN locations l ON r.room_location_id = l.location_id
                LEFT JOIN CurrentBookings cb ON r.room_id = cb.booking_room_id AND cb.rn = 1
                LEFT JOIN users u1 ON cb.booking_user_id = u1.user_id
                LEFT JOIN NextBookings nb ON r.room_id = nb.booking_room_id AND nb.rn = 1
                LEFT JOIN users u2 ON nb.booking_user_id = u2.user_id
                WHERE r.room_is_active = 1
                ORDER BY l.location_floor, r.room_name";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            room_id = reader["room_id"],
                            room_name = reader["room_name"],
                            room_code = reader["room_code"],
                            room_capacity = reader["room_capacity"],
                            room_status = reader["room_status"],
                            location_plant_name = reader["location_plant_name"],
                            location_block = reader["location_block"],
                            location_floor = reader["location_floor"],
                            current_booking_id = reader["current_booking_id"] == DBNull.Value ? null : reader["current_booking_id"],
                            current_booking_title = reader["current_booking_title"] == DBNull.Value ? null : reader["current_booking_title"].ToString(),
                            current_booking_end = reader["current_booking_end"] == DBNull.Value ? null : reader["current_booking_end"].ToString(),
                            current_user_name = reader["current_user_name"] == DBNull.Value ? null : reader["current_user_name"].ToString(),
                            next_booking_start = reader["next_booking_start"] == DBNull.Value ? null : reader["next_booking_start"].ToString(),
                            next_user_name = reader["next_user_name"] == DBNull.Value ? null : reader["next_user_name"].ToString()
                        });
                    }
                }
            }
            return results;
        }

        // Get unique floors
        private List<int> GetFloors(SqlConnection conn)
        {
            string query = "SELECT DISTINCT location_floor FROM locations ORDER BY location_floor";
            var floors = new List<int>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        floors.Add((int)(byte)reader["location_floor"]);
                    }
                }
            }
            return floors;
        }

        // Get all active rooms
        private List<dynamic> GetAllRooms(SqlConnection conn)
        {
            string query = "SELECT room_id, room_name FROM rooms WHERE room_is_active = 1 ORDER BY room_name";
            var rooms = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new
                        {
                            room_id = reader["room_id"],
                            room_name = reader["room_name"]
                        });
                    }
                }
            }
            return rooms;
        }

        // Get today's complete schedule
        private List<dynamic> GetTodaySchedule(SqlConnection conn)
        {
            string query = @"
                SELECT 
                    b.booking_id,
                    b.booking_title,
                    b.booking_description,
                    b.booking_room_id,
                    b.booking_status,
                    CONVERT(VARCHAR(5), b.booking_start_time, 108) as booking_start_time,
                    CONVERT(VARCHAR(5), b.booking_end_time, 108) as booking_end_time,
                    r.room_name,
                    l.location_plant_name + ' - B' + CAST(l.location_block AS VARCHAR) + 'F' + CAST(l.location_floor AS VARCHAR) as location_info,
                    u.user_full_name,
                    CASE 
                        WHEN b.booking_status IN ('Confirmed', 'InProgress')
                        AND CAST(GETDATE() AS TIME) BETWEEN b.booking_start_time AND b.booking_end_time
                        THEN 1
                        ELSE 0
                    END as is_current
                FROM bookings b
                INNER JOIN rooms r ON b.booking_room_id = r.room_id
                INNER JOIN locations l ON r.room_location_id = l.location_id
                INNER JOIN users u ON b.booking_user_id = u.user_id
                WHERE b.booking_date = CAST(GETDATE() AS DATE)
                AND b.booking_status != 'Cancelled'
                ORDER BY b.booking_start_time";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            booking_id = reader["booking_id"],
                            booking_title = reader["booking_title"],
                            booking_description = reader["booking_description"] == DBNull.Value ? "" : reader["booking_description"].ToString(),
                            booking_room_id = reader["booking_room_id"],
                            booking_status = reader["booking_status"],
                            booking_start_time = reader["booking_start_time"],
                            booking_end_time = reader["booking_end_time"],
                            room_name = reader["room_name"],
                            location_info = reader["location_info"],
                            user_full_name = reader["user_full_name"],
                            is_current = Convert.ToBoolean(reader["is_current"])
                        });
                    }
                }
            }
            return results;
        }

        // Get booking trends for last 7 days
        private List<dynamic> GetBookingTrends(SqlConnection conn)
        {
            string query = @"
                WITH DateRange AS (
                    SELECT CAST(DATEADD(day, -6, GETDATE()) AS DATE) as date_val
                    UNION ALL
                    SELECT CAST(DATEADD(day, -5, GETDATE()) AS DATE)
                    UNION ALL
                    SELECT CAST(DATEADD(day, -4, GETDATE()) AS DATE)
                    UNION ALL
                    SELECT CAST(DATEADD(day, -3, GETDATE()) AS DATE)
                    UNION ALL
                    SELECT CAST(DATEADD(day, -2, GETDATE()) AS DATE)
                    UNION ALL
                    SELECT CAST(DATEADD(day, -1, GETDATE()) AS DATE)
                    UNION ALL
                    SELECT CAST(GETDATE() AS DATE)
                )
                SELECT 
                    dr.date_val,
                    FORMAT(dr.date_val, 'MMM dd') as date_str,
                    COALESCE(COUNT(b.booking_id), 0) as count
                FROM DateRange dr
                LEFT JOIN bookings b ON b.booking_date = dr.date_val
                GROUP BY dr.date_val
                ORDER BY dr.date_val";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            date_str = reader["date_str"],
                            count = reader["count"]
                        });
                    }
                }
            }
            return results;
        }

        // Get room utilization rate
        private List<dynamic> GetRoomUtilization(SqlConnection conn)
        {
            string query = @"
                WITH RoomBookings AS (
                    SELECT 
                        r.room_id,
                        r.room_name,
                        COUNT(b.booking_id) as booking_count,
                        SUM(DATEDIFF(MINUTE, b.booking_start_time, b.booking_end_time)) as total_minutes
                    FROM rooms r
                    LEFT JOIN bookings b ON r.room_id = b.booking_room_id
                        AND b.booking_date >= DATEADD(day, -30, GETDATE())
                        AND b.booking_status IN ('Confirmed', 'Completed')
                    WHERE r.room_is_active = 1
                    GROUP BY r.room_id, r.room_name
                )
                SELECT 
                    room_name,
                    booking_count,
                    CAST(ROUND((CAST(total_minutes AS FLOAT) / (30 * 8 * 60)) * 100, 1) AS DECIMAL(5,1)) as utilization_rate
                FROM RoomBookings
                ORDER BY utilization_rate DESC";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            room_name = reader["room_name"],
                            utilization_rate = reader["utilization_rate"]
                        });
                    }
                }
            }
            return results;
        }

        // Get peak booking hours
        private List<dynamic> GetPeakBookingHours(SqlConnection conn)
        {
            string query = @"
                SELECT
                    DATEPART(HOUR, booking_start_time) AS hour_val,
                    CONCAT(
                        RIGHT('0' + CAST(DATEPART(HOUR, booking_start_time) AS VARCHAR(2)), 2),
                        ':00 ',
                        CASE WHEN DATEPART(HOUR, booking_start_time) >= 12 THEN 'PM' ELSE 'AM' END
                    ) AS hour_str,
                    COUNT(*) AS booking_count
                FROM bookings
                WHERE booking_date >= DATEADD(DAY, -30, GETDATE())
                GROUP BY
                    DATEPART(HOUR, booking_start_time)
                ORDER BY hour_val;";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            hour_str = reader["hour_str"],
                            booking_count = reader["booking_count"]
                        });
                    }
                }
            }
            return results;
        }

        // Get week overview
        private List<dynamic> GetWeekOverview(SqlConnection conn)
        {
            string query = @"
                WITH DateRange AS (
                    SELECT 0 as day_offset
                    UNION ALL SELECT 1
                    UNION ALL SELECT 2
                    UNION ALL SELECT 3
                    UNION ALL SELECT 4
                    UNION ALL SELECT 5
                    UNION ALL SELECT 6
                )
                SELECT 
                    CAST(DATEADD(day, dr.day_offset, GETDATE()) AS DATE) as date_val,
                    FORMAT(DATEADD(day, dr.day_offset, GETDATE()), 'ddd') as day_name,
                    FORMAT(DATEADD(day, dr.day_offset, GETDATE()), 'MMM dd') as date_str,
                    COALESCE(COUNT(b.booking_id), 0) as booking_count,
                    CASE WHEN dr.day_offset = 0 THEN 1 ELSE 0 END as is_today
                FROM DateRange dr
                LEFT JOIN bookings b ON b.booking_date = CAST(DATEADD(day, dr.day_offset, GETDATE()) AS DATE)
                GROUP BY dr.day_offset, DATEADD(day, dr.day_offset, GETDATE())
                ORDER BY dr.day_offset";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            day_name = reader["day_name"],
                            date_str = reader["date_str"],
                            booking_count = reader["booking_count"],
                            is_today = Convert.ToBoolean(reader["is_today"])
                        });
                    }
                }
            }
            return results;
        }

        // Get pending actions
        private List<dynamic> GetPendingActions(SqlConnection conn)
        {
            string query = @"
                SELECT TOP 5
                    b.booking_id,
                    b.booking_title,
                    b.booking_date,
                    FORMAT(b.booking_date, 'MMM dd, yyyy') as booking_date_str,
                    CONVERT(VARCHAR(5), b.booking_start_time, 108) + ' - ' + CONVERT(VARCHAR(5), b.booking_end_time, 108) as time_range,
                    u.user_full_name,
                    r.room_name
                FROM bookings b
                INNER JOIN users u ON b.booking_user_id = u.user_id
                INNER JOIN rooms r ON b.booking_room_id = r.room_id
                WHERE b.booking_status = 'Pending'
                ORDER BY b.booking_date, b.booking_start_time";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            booking_id = reader["booking_id"],
                            booking_title = reader["booking_title"],
                            booking_date_str = reader["booking_date_str"],
                            time_range = reader["time_range"],
                            user_full_name = reader["user_full_name"],
                            room_name = reader["room_name"]
                        });
                    }
                }
            }
            return results;
        }

        // Get top users this month
        private List<dynamic> GetTopUsers(SqlConnection conn)
        {
            string query = @"
                SELECT TOP 5
                    u.user_id,
                    u.user_full_name,
                    u.user_dept_name,
                    COUNT(b.booking_id) as booking_count
                FROM users u
                INNER JOIN bookings b ON u.user_id = b.booking_user_id
                WHERE b.booking_date >= DATEADD(day, -30, GETDATE())
                GROUP BY u.user_id, u.user_full_name, u.user_dept_name
                ORDER BY booking_count DESC";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            user_full_name = reader["user_full_name"],
                            user_dept_name = reader["user_dept_name"] == DBNull.Value ? "N/A" : reader["user_dept_name"].ToString(),
                            booking_count = reader["booking_count"]
                        });
                    }
                }
            }
            return results;
        }

        // Quick approve booking (AJAX endpoint)
        [HttpPost]
        public JsonResult QApproveBooking(int id)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var now = DateTime.Now;
                    var currentDate = now.Date;
                    var currentTime = now.TimeOfDay;
                    int systemUserId = 1; // System user for auto-cancellations
                    string query = @"UPDATE bookings SET booking_status = 'Confirmed', booking_updated_at = GETDATE() WHERE booking_id = @id;
                                
                                UPDATE bookings
                                SET booking_status = 'Cancelled',
                                    booking_cancel_reason = 'Not reviewed by admin before meeting time.',
                                    booking_cancelled_by = @SystemUserId,
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Pending'
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_start_time < @CurrentTime)
                                    );
                                
                                UPDATE bookings
                                SET booking_status = 'InProgress',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Confirmed'
                                    AND booking_date = @CurrentDate
                                    AND booking_start_time <= @CurrentTime
                                    AND booking_end_time > @CurrentTime;
                                
                                UPDATE bookings
                                SET booking_status = 'Completed',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status IN ('InProgress', 'Confirmed')
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_end_time <= @CurrentTime)
                                    );";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@SystemUserId", systemUserId);
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        cmd.ExecuteNonQuery();

                LogActivity("Approve Booking", $"Approved booking ID: {id}");
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Quick reject booking (AJAX endpoint)
        [HttpPost]
        public JsonResult QRejectBooking(int id, string reason)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var now = DateTime.Now;
                    var currentDate = now.Date;
                    var currentTime = now.TimeOfDay;
                    int systemUserId = 1; // System user for auto-cancellations
                    int adminId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
                    string query = @"UPDATE bookings 
                                SET booking_status = 'Cancelled', 
                                    booking_cancel_reason = @reason,
                                    booking_cancelled_by = @adminId,
                                    booking_updated_at = GETDATE() 
                                WHERE booking_id = @id;
                                
                                UPDATE bookings
                                SET booking_status = 'Cancelled',
                                    booking_cancel_reason = 'Not reviewed by admin before meeting time.',
                                    booking_cancelled_by = @SystemUserId,
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Pending'
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_start_time < @CurrentTime)
                                    );
                                
                                UPDATE bookings
                                SET booking_status = 'InProgress',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Confirmed'
                                    AND booking_date = @CurrentDate
                                    AND booking_start_time <= @CurrentTime
                                    AND booking_end_time > @CurrentTime;
                                
                                UPDATE bookings
                                SET booking_status = 'Completed',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status IN ('InProgress', 'Confirmed')
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_end_time <= @CurrentTime)
                                    );";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@reason", reason);
                        cmd.Parameters.AddWithValue("@adminId", adminId);
                        cmd.Parameters.AddWithValue("@SystemUserId", systemUserId);
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        cmd.ExecuteNonQuery();
                
                LogActivity("Cancel Booking", $"Cancelled booking ID: {id}");
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // KF-05, KF-06: Users Management
        // ============================================
        [HttpGet]
        public IActionResult UsersView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<User> users = new List<User>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM users ORDER BY user_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    user_id = Convert.ToInt32(reader["user_id"]),
                                    user_employee_id = reader["user_employee_id"].ToString()!,
                                    user_email = reader["user_email"].ToString()!,
                                    user_full_name = reader["user_full_name"].ToString()!,
                                    user_role = reader["user_role"].ToString()!,
                                    user_dept_name = reader["user_dept_name"]?.ToString(),
                                    user_is_active = Convert.ToBoolean(reader["user_is_active"]),
                                    user_created_at = Convert.ToDateTime(reader["user_created_at"])
                                });
                            }
                        }
                    }
                }

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading users: " + ex.Message;
                return View(new List<User>());
            }
        }

        [HttpPost]
        public IActionResult CreateUser(string employeeId, string email, string fullName, string role, string deptName, string password)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Check if employee ID or email already exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE user_employee_id = @EmployeeId OR user_email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                        checkCmd.Parameters.AddWithValue("@Email", email);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Employee ID or Email already exists" });
                    }

                    // Hash password
                    string hashedPassword = SecHelperFunction.HashPasswordMD5(password);

                    // Insert user
                    string query = @"INSERT INTO users (user_employee_id, user_email, user_password, user_full_name, 
                                   user_role, user_dept_name, user_is_active, user_created_at, user_updated_at)
                                   VALUES (@EmployeeId, @Email, @Password, @FullName, @Role, @DeptName, 1, GETDATE(), GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@DeptName", string.IsNullOrEmpty(deptName) ? (object)DBNull.Value : deptName);

                        cmd.ExecuteNonQuery();

                        LogActivity("Create User", $"Created user: {fullName} ({employeeId})");

                        return Json(new { success = true, message = "User created successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateUser(int userId, string fullName, string email, string role, string deptName)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE users 
                                   SET user_full_name = @FullName,
                                       user_email = @Email,
                                       user_role = @Role,
                                       user_dept_name = @DeptName,
                                       user_updated_at = GETDATE()
                                   WHERE user_id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@DeptName", string.IsNullOrEmpty(deptName) ? (object)DBNull.Value : deptName);
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        cmd.ExecuteNonQuery();

                        LogActivity("Update User", $"Updated user ID: {userId}");

                        return Json(new { success = true, message = "User updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleUserStatus(int userId, bool isActive)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET user_is_active = @IsActive, user_updated_at = GETDATE() WHERE user_id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Toggle User Status", $"Changed user ID {userId} status to {(isActive ? "Active" : "Inactive")}");

                        return Json(new { success = true, message = $"User {(isActive ? "activated" : "deactivated")} successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ResetUserPassword(int userId, string newPassword)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                string hashedPassword = SecHelperFunction.HashPasswordMD5(newPassword);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE users SET user_password = @Password, user_updated_at = GETDATE() WHERE user_id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Reset Password", $"Reset password for user ID: {userId}");

                        return Json(new { success = true, message = "Password reset successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-08: Locations Management
        // ============================================
        [HttpGet]
        public IActionResult LocationsView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<Location> locations = new List<Location>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM locations ORDER BY location_plant_name, location_block, location_floor";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                locations.Add(new Location
                                {
                                    location_id = Convert.ToInt32(reader["location_id"]),
                                    location_code = reader["location_code"].ToString()!,
                                    location_plant_name = reader["location_plant_name"].ToString()!,
                                    location_block = Convert.ToByte(reader["location_block"]),
                                    location_floor = Convert.ToByte(reader["location_floor"]),
                                    location_is_active = Convert.ToBoolean(reader["location_is_active"]),
                                    location_created_at = Convert.ToDateTime(reader["location_created_at"])
                                });
                            }
                        }
                    }
                }

                return View(locations);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading locations: " + ex.Message;
                return View(new List<Location>());
            }
        }

        [HttpPost]
        public IActionResult CreateLocation(string code, string plantName, byte block, byte floor)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Check if code exists
                    string checkQuery = "SELECT COUNT(*) FROM locations WHERE location_code = @Code";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", code);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Location code already exists" });
                    }

                    string query = @"INSERT INTO locations (location_code, location_plant_name, location_block, 
                                   location_floor, location_is_active, location_created_at)
                                   VALUES (@Code, @PlantName, @Block, @Floor, 1, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@Block", block);
                        cmd.Parameters.AddWithValue("@Floor", floor);

                        cmd.ExecuteNonQuery();

                        LogActivity("Create Location", $"Created location: {code}");

                        return Json(new { success = true, message = "Location created successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateLocation(int locationId, string plantName, byte block, byte floor)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE locations 
                                   SET location_plant_name = @PlantName,
                                       location_block = @Block,
                                       location_floor = @Floor
                                   WHERE location_id = @LocationId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@Block", block);
                        cmd.Parameters.AddWithValue("@Floor", floor);
                        cmd.Parameters.AddWithValue("@LocationId", locationId);

                        cmd.ExecuteNonQuery();

                        LogActivity("Update Location", $"Updated location ID: {locationId}");

                        return Json(new { success = true, message = "Location updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleLocationStatus(int locationId, bool isActive)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE locations SET location_is_active = @IsActive WHERE location_id = @LocationId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Toggle Location Status", $"Changed location ID {locationId} status to {(isActive ? "Active" : "Inactive")}");

                        return Json(new { success = true, message = $"Location {(isActive ? "activated" : "deactivated")} successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-09, KF-10: Rooms Management
        // ============================================
        [HttpGet]
        public IActionResult RoomsView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<dynamic> rooms = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT r.*, l.location_code, l.location_plant_name, l.location_block, l.location_floor
                                   FROM rooms r
                                   LEFT JOIN locations l ON r.room_location_id = l.location_id
                                   ORDER BY r.room_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rooms.Add(new
                                {
                                    room_id = Convert.ToInt32(reader["room_id"]),
                                    room_location_id = Convert.ToInt32(reader["room_location_id"]),
                                    room_code = reader["room_code"].ToString(),
                                    room_name = reader["room_name"].ToString(),
                                    room_capacity = Convert.ToInt32(reader["room_capacity"]),
                                    room_facilities = reader["room_facilities"]?.ToString(),
                                    room_status = reader["room_status"].ToString(),
                                    room_is_active = Convert.ToBoolean(reader["room_is_active"]),
                                    location_code = reader["location_code"]?.ToString(),
                                    location_plant_name = reader["location_plant_name"]?.ToString(),
                                    location_block = reader["location_block"] != DBNull.Value ? Convert.ToInt32(reader["location_block"]) : 0,
                                    location_floor = reader["location_floor"] != DBNull.Value ? Convert.ToInt32(reader["location_floor"]) : 0
                                });
                            }
                        }
                    }
                }

                // Get locations for dropdown
                ViewBag.Locations = GetActiveLocations();

                return View(rooms);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading rooms: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        [HttpPost]
        public IActionResult CreateRoom(string code, string name, int locationId, int capacity, string facilities, string status)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Check if code exists
                    string checkQuery = "SELECT COUNT(*) FROM rooms WHERE room_code = @Code";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", code);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Room code already exists" });
                    }

                    string query = @"INSERT INTO rooms (room_code, room_name, room_location_id, room_capacity, 
                                   room_facilities, room_status, room_is_active, room_created_at, room_updated_at)
                                   VALUES (@Code, @Name, @LocationId, @Capacity, @Facilities, @Status, 1, GETDATE(), GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        cmd.Parameters.AddWithValue("@Capacity", capacity);
                        cmd.Parameters.AddWithValue("@Facilities", string.IsNullOrEmpty(facilities) ? (object)DBNull.Value : facilities);
                        cmd.Parameters.AddWithValue("@Status", status);

                        cmd.ExecuteNonQuery();

                        LogActivity("Create Room", $"Created room: {code} - {name}");

                        return Json(new { success = true, message = "Room created successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateRoom(int roomId, string name, int locationId, int capacity, string facilities, string status)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE rooms 
                                   SET room_name = @Name,
                                       room_location_id = @LocationId,
                                       room_capacity = @Capacity,
                                       room_facilities = @Facilities,
                                       room_status = @Status,
                                       room_updated_at = GETDATE()
                                   WHERE room_id = @RoomId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@LocationId", locationId);
                        cmd.Parameters.AddWithValue("@Capacity", capacity);
                        cmd.Parameters.AddWithValue("@Facilities", string.IsNullOrEmpty(facilities) ? (object)DBNull.Value : facilities);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@RoomId", roomId);

                        cmd.ExecuteNonQuery();

                        LogActivity("Update Room", $"Updated room ID: {roomId}");

                        return Json(new { success = true, message = "Room updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleRoomStatus(int roomId, bool isActive)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE rooms SET room_is_active = @IsActive, room_updated_at = GETDATE() WHERE room_id = @RoomId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Toggle Room Status", $"Changed room ID {roomId} status to {(isActive ? "Active" : "Inactive")}");

                        return Json(new { success = true, message = $"Room {(isActive ? "activated" : "deactivated")} successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateRoomOperationalStatus(int roomId, string status)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE rooms SET room_status = @Status, room_updated_at = GETDATE() WHERE room_id = @RoomId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Update Room Status", $"Changed room ID {roomId} operational status to {status}");

                        return Json(new { success = true, message = "Room status updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-11: Bookings Management
        // ============================================
        [HttpGet]
        public IActionResult BookingsView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<dynamic> bookings = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT b.*, r.room_code, r.room_name, u.user_full_name, u.user_employee_id
                                   FROM bookings b
                                   LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                                   LEFT JOIN users u ON b.booking_user_id = u.user_id
                                   ORDER BY b.booking_date DESC, b.booking_start_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bookings.Add(new
                                {
                                    booking_id = Convert.ToInt32(reader["booking_id"]),
                                    booking_code = reader["booking_code"].ToString(),
                                    booking_title = reader["booking_title"].ToString(),
                                    booking_description = reader["booking_description"]?.ToString(),
                                    booking_date = Convert.ToDateTime(reader["booking_date"]),
                                    booking_start_time = TimeSpan.Parse(reader["booking_start_time"].ToString()!),
                                    booking_end_time = TimeSpan.Parse(reader["booking_end_time"].ToString()!),
                                    booking_status = reader["booking_status"].ToString(),
                                    room_code = reader["room_code"]?.ToString(),
                                    room_name = reader["room_name"]?.ToString(),
                                    user_full_name = reader["user_full_name"]?.ToString(),
                                    user_employee_id = reader["user_employee_id"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return View(bookings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading bookings: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        [HttpPost]
        public IActionResult ApproveBooking(int bookingId)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var now = DateTime.Now;
                    var currentDate = now.Date;
                    var currentTime = now.TimeOfDay;
                    int systemUserId = 1; // System user for auto-cancellations
                    string query = @"
                                UPDATE bookings SET booking_status = 'Confirmed', booking_updated_at = GETDATE() WHERE booking_id = @BookingId;
                                
                                UPDATE bookings
                                SET booking_status = 'Cancelled',
                                    booking_cancel_reason = 'Not reviewed by admin before meeting time.',
                                    booking_cancelled_by = @SystemUserId,
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Pending'
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_start_time < @CurrentTime)
                                    );
                                
                                UPDATE bookings
                                SET booking_status = 'InProgress',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Confirmed'
                                    AND booking_date = @CurrentDate
                                    AND booking_start_time <= @CurrentTime
                                    AND booking_end_time > @CurrentTime;
                                
                                UPDATE bookings
                                SET booking_status = 'Completed',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status IN ('InProgress', 'Confirmed')
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_end_time <= @CurrentTime)
                                    );";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@SystemUserId", systemUserId);
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        cmd.ExecuteNonQuery();

                        LogActivity("Approve Booking", $"Approved booking ID: {bookingId}");

                        return Json(new { success = true, message = "Booking approved successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CancelBooking(int bookingId, string reason)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var adminId = HttpContext.Session.GetString("UserId");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var now = DateTime.Now;
                    var currentDate = now.Date;
                    var currentTime = now.TimeOfDay;
                    int systemUserId = 1; // System user for auto-cancellations
                    string query = @"UPDATE bookings 
                                SET booking_status = 'Cancelled', 
                                    booking_cancel_reason = @Reason,
                                    booking_cancelled_by = @CancelledBy,
                                    booking_updated_at = GETDATE() 
                                WHERE booking_id = @BookingId;
                                
                                UPDATE bookings
                                SET booking_status = 'Cancelled',
                                    booking_cancel_reason = 'Not reviewed by admin before meeting time.',
                                    booking_cancelled_by = @SystemUserId,
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Pending'
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_start_time < @CurrentTime)
                                    );
                                
                                UPDATE bookings
                                SET booking_status = 'InProgress',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status = 'Confirmed'
                                    AND booking_date = @CurrentDate
                                    AND booking_start_time <= @CurrentTime
                                    AND booking_end_time > @CurrentTime;
                                
                                UPDATE bookings
                                SET booking_status = 'Completed',
                                    booking_updated_at = GETDATE()
                                WHERE booking_status IN ('InProgress', 'Confirmed')
                                    AND (
                                        (booking_date < @CurrentDate)
                                        OR 
                                        (booking_date = @CurrentDate AND booking_end_time <= @CurrentTime)
                                    );";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Reason", reason);
                        cmd.Parameters.AddWithValue("@CancelledBy", adminId);
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@SystemUserId", systemUserId);
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        cmd.ExecuteNonQuery();

                        LogActivity("Cancel Booking", $"Cancelled booking ID: {bookingId}");

                        return Json(new { success = true, message = "Booking cancelled successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // Feedbacks Management
        // ============================================
        [HttpGet]
        public IActionResult FeedbacksView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<dynamic> feedbacks = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT f.*, b.booking_code, b.booking_title, u.user_full_name, r.room_name
                                   FROM feedbacks f
                                   LEFT JOIN bookings b ON f.feedback_booking_id = b.booking_id
                                   LEFT JOIN users u ON f.feedback_user_id = u.user_id
                                   LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                                   ORDER BY f.feedback_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedbacks.Add(new
                                {
                                    feedback_id = Convert.ToInt32(reader["feedback_id"]),
                                    feedback_rating = Convert.ToInt32(reader["feedback_rating"]),
                                    feedback_comments = reader["feedback_comments"]?.ToString(),
                                    feedback_admin_response = reader["feedback_admin_response"]?.ToString(),
                                    feedback_created_at = Convert.ToDateTime(reader["feedback_created_at"]),
                                    booking_code = reader["booking_code"]?.ToString(),
                                    booking_title = reader["booking_title"]?.ToString(),
                                    user_full_name = reader["user_full_name"]?.ToString(),
                                    room_name = reader["room_name"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return View(feedbacks);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading feedbacks: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        [HttpPost]
        public IActionResult RespondToFeedback(int feedbackId, string response)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var adminId = HttpContext.Session.GetString("UserId");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE feedbacks 
                                   SET feedback_admin_response = @Response,
                                       feedback_admin_responded_by = @AdminId,
                                       feedback_responded_at = GETDATE()
                                   WHERE feedback_id = @FeedbackId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Response", response);
                        cmd.Parameters.AddWithValue("@AdminId", adminId);
                        cmd.Parameters.AddWithValue("@FeedbackId", feedbackId);
                        cmd.ExecuteNonQuery();

                        LogActivity("Respond to Feedback", $"Responded to feedback ID: {feedbackId}");

                        return Json(new { success = true, message = "Response saved successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-13: Activity Logs View
        // ============================================
        [HttpGet]
        public IActionResult ActivityLogsView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                List<dynamic> logs = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Create table if not exists
                    string createTable = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'activity_logs')
                                         BEGIN
                                             CREATE TABLE activity_logs (
                                                 log_id INT IDENTITY(1,1) PRIMARY KEY,
                                                 log_user_id INT NOT NULL,
                                                 log_action NVARCHAR(100) NOT NULL,
                                                 log_description NVARCHAR(500),
                                                 log_created_at DATETIME2 DEFAULT GETDATE(),
                                                 FOREIGN KEY (log_user_id) REFERENCES users(user_id)
                                             )
                                         END";
                    
                    using (SqlCommand createCmd = new SqlCommand(createTable, conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    string query = @"SELECT l.*, u.user_full_name, u.user_employee_id
                                   FROM activity_logs l
                                   LEFT JOIN users u ON l.log_user_id = u.user_id
                                   ORDER BY l.log_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.Add(new
                                {
                                    log_id = Convert.ToInt32(reader["log_id"]),
                                    log_action = reader["log_action"].ToString(),
                                    log_description = reader["log_description"]?.ToString(),
                                    log_created_at = Convert.ToDateTime(reader["log_created_at"]),
                                    user_full_name = reader["user_full_name"]?.ToString(),
                                    user_employee_id = reader["user_employee_id"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return View(logs);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading activity logs: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        // ============================================
        // KF-03: Profile Management (Shared with Auth)
        // ============================================
        [HttpGet]
        public IActionResult ProfileView()
        {
            if (!CheckAdminAuth())
                return RedirectToAction("LoginView", "Auth");

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                User user = new User();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM users WHERE user_id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user.user_id = Convert.ToInt32(reader["user_id"]);
                                user.user_employee_id = reader["user_employee_id"].ToString()!;
                                user.user_email = reader["user_email"].ToString()!;
                                user.user_full_name = reader["user_full_name"].ToString()!;
                                user.user_role = reader["user_role"].ToString()!;
                                user.user_dept_name = reader["user_dept_name"]?.ToString();
                                user.user_is_active = Convert.ToBoolean(reader["user_is_active"]);
                                user.user_created_at = Convert.ToDateTime(reader["user_created_at"]);
                            }
                        }
                    }
                }

                return View(user);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading profile: " + ex.Message;
                return RedirectToAction("DashboardView");
            }
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string deptName, string email)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE users 
                                   SET user_full_name = @FullName, 
                                       user_dept_name = @DeptName,
                                       user_email = @Email,
                                       user_updated_at = GETDATE()
                                   WHERE user_id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@DeptName", string.IsNullOrEmpty(deptName) ? (object)DBNull.Value : deptName);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        cmd.ExecuteNonQuery();

                        // Update session
                        HttpContext.Session.SetString("UserFullName", fullName);
                        HttpContext.Session.SetString("UserEmail", email);
                        if (!string.IsNullOrEmpty(deptName))
                            HttpContext.Session.SetString("UserDeptName", deptName);

                        LogActivity("Update Profile", "Updated own profile information");

                        return Json(new { success = true, message = "Profile updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            if (!CheckAdminAuth())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Verify current password
                    string queryVerify = "SELECT user_password FROM users WHERE user_id = @UserId";
                    using (SqlCommand cmdVerify = new SqlCommand(queryVerify, conn))
                    {
                        cmdVerify.Parameters.AddWithValue("@UserId", userId);
                        string storedPassword = cmdVerify.ExecuteScalar()?.ToString() ?? "";

                        if (!SecHelperFunction.VerifyPassword(SecHelperFunction.HashPasswordMD5(currentPassword), storedPassword))
                        {
                            return Json(new { success = false, message = "Current password is incorrect" });
                        }
                    }

                    // Update password
                    string hashedNewPassword = SecHelperFunction.HashPasswordMD5(newPassword);
                    string queryUpdate = @"UPDATE users 
                                         SET user_password = @NewPassword,
                                             user_updated_at = GETDATE()
                                         WHERE user_id = @UserId";

                    using (SqlCommand cmdUpdate = new SqlCommand(queryUpdate, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@NewPassword", hashedNewPassword);
                        cmdUpdate.Parameters.AddWithValue("@UserId", userId);
                        cmdUpdate.ExecuteNonQuery();

                        LogActivity("Change Password", "Changed own password");

                        return Json(new { success = true, message = "Password changed successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-14: Export Data to Spreadsheet
        // ============================================
        [HttpGet]
        public IActionResult ExportUsers()
        {
            if (!CheckAdminAuth())
                return Forbid();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Users");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Employee ID";
                    worksheet.Cell(1, 2).Value = "Full Name";
                    worksheet.Cell(1, 3).Value = "Email";
                    worksheet.Cell(1, 4).Value = "Role";
                    worksheet.Cell(1, 5).Value = "Department";
                    worksheet.Cell(1, 6).Value = "Status";
                    worksheet.Cell(1, 7).Value = "Created At";

                    // Style headers
                    var headerRange = worksheet.Range(1, 1, 1, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                    // Data
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        string query = "SELECT * FROM users ORDER BY user_created_at DESC";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int row = 2;
                                while (reader.Read())
                                {
                                    worksheet.Cell(row, 1).Value = reader["user_employee_id"].ToString();
                                    worksheet.Cell(row, 2).Value = reader["user_full_name"].ToString();
                                    worksheet.Cell(row, 3).Value = reader["user_email"].ToString();
                                    worksheet.Cell(row, 4).Value = reader["user_role"].ToString();
                                    worksheet.Cell(row, 5).Value = reader["user_dept_name"]?.ToString() ?? "";
                                    worksheet.Cell(row, 6).Value = Convert.ToBoolean(reader["user_is_active"]) ? "Active" : "Inactive";
                                    worksheet.Cell(row, 7).Value = Convert.ToDateTime(reader["user_created_at"]).ToString("yyyy-MM-dd HH:mm:ss");
                                    row++;
                                }
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        LogActivity("Export Data", "Exported users data to Excel");

                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("UsersView");
            }
        }

        [HttpGet]
        public IActionResult ExportBookings()
        {
            if (!CheckAdminAuth())
                return Forbid();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Bookings");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Booking Code";
                    worksheet.Cell(1, 2).Value = "Title";
                    worksheet.Cell(1, 3).Value = "Room";
                    worksheet.Cell(1, 4).Value = "User";
                    worksheet.Cell(1, 5).Value = "Date";
                    worksheet.Cell(1, 6).Value = "Start Time";
                    worksheet.Cell(1, 7).Value = "End Time";
                    worksheet.Cell(1, 8).Value = "Status";

                    // Style headers
                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

                    // Data
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        string query = @"SELECT b.*, r.room_code, r.room_name, u.user_full_name
                                       FROM bookings b
                                       LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                                       LEFT JOIN users u ON b.booking_user_id = u.user_id
                                       ORDER BY b.booking_date DESC";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int row = 2;
                                while (reader.Read())
                                {
                                    worksheet.Cell(row, 1).Value = reader["booking_code"].ToString();
                                    worksheet.Cell(row, 2).Value = reader["booking_title"].ToString();
                                    worksheet.Cell(row, 3).Value = $"{reader["room_code"]} - {reader["room_name"]}";
                                    worksheet.Cell(row, 4).Value = reader["user_full_name"]?.ToString();
                                    worksheet.Cell(row, 5).Value = Convert.ToDateTime(reader["booking_date"]).ToString("yyyy-MM-dd");
                                    worksheet.Cell(row, 6).Value = reader["booking_start_time"].ToString();
                                    worksheet.Cell(row, 7).Value = reader["booking_end_time"].ToString();
                                    worksheet.Cell(row, 8).Value = reader["booking_status"].ToString();
                                    row++;
                                }
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        LogActivity("Export Data", "Exported bookings data to Excel");

                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Bookings_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("BookingsView");
            }
        }

        [HttpGet]
        public IActionResult ExportRooms()
        {
            if (!CheckAdminAuth())
                return Forbid();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Rooms");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Room Code";
                    worksheet.Cell(1, 2).Value = "Room Name";
                    worksheet.Cell(1, 3).Value = "Location";
                    worksheet.Cell(1, 4).Value = "Capacity";
                    worksheet.Cell(1, 5).Value = "Facilities";
                    worksheet.Cell(1, 6).Value = "Status";

                    // Style headers
                    var headerRange = worksheet.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;

                    // Data
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        string query = @"SELECT r.*, l.location_code, l.location_plant_name
                                       FROM rooms r
                                       LEFT JOIN locations l ON r.room_location_id = l.location_id
                                       ORDER BY r.room_code";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int row = 2;
                                while (reader.Read())
                                {
                                    worksheet.Cell(row, 1).Value = reader["room_code"].ToString();
                                    worksheet.Cell(row, 2).Value = reader["room_name"].ToString();
                                    worksheet.Cell(row, 3).Value = $"{reader["location_code"]} - {reader["location_plant_name"]}";
                                    worksheet.Cell(row, 4).Value = Convert.ToInt32(reader["room_capacity"]);
                                    worksheet.Cell(row, 5).Value = reader["room_facilities"]?.ToString() ?? "";
                                    worksheet.Cell(row, 6).Value = reader["room_status"].ToString();
                                    row++;
                                }
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        LogActivity("Export Data", "Exported rooms data to Excel");

                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Rooms_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("RoomsView");
            }
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private void LogActivity(string action, string description)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId)) return;

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Create table if not exists
                    string createTable = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'activity_logs')
                                         BEGIN
                                             CREATE TABLE activity_logs (
                                                 log_id INT IDENTITY(1,1) PRIMARY KEY,
                                                 log_user_id INT NOT NULL,
                                                 log_action NVARCHAR(100) NOT NULL,
                                                 log_description NVARCHAR(500),
                                                 log_created_at DATETIME2 DEFAULT GETDATE(),
                                                 FOREIGN KEY (log_user_id) REFERENCES users(user_id)
                                             )
                                         END";
                    
                    using (SqlCommand createCmd = new SqlCommand(createTable, conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    string query = @"INSERT INTO activity_logs (log_user_id, log_action, log_description, log_created_at)
                                   VALUES (@UserId, @Action, @Description, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Silent fail
            }
        }

        private int GetCount(SqlConnection conn, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private List<dynamic> GetRecentBookings(SqlConnection conn)
        {
            List<dynamic> bookings = new List<dynamic>();
            string query = @"SELECT TOP 10 b.*, r.room_name, u.user_full_name
                           FROM bookings b
                           LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                           LEFT JOIN users u ON b.booking_user_id = u.user_id
                           ORDER BY b.booking_created_at DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookings.Add(new
                        {
                            booking_title = reader["booking_title"].ToString(),
                            room_name = reader["room_name"]?.ToString(),
                            user_full_name = reader["user_full_name"]?.ToString(),
                            booking_date = Convert.ToDateTime(reader["booking_date"]),
                            booking_status = reader["booking_status"].ToString()
                        });
                    }
                }
            }
            return bookings;
        }

        private Dictionary<string, int> GetBookingsByStatus(SqlConnection conn)
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();
            string query = "SELECT booking_status, COUNT(*) as count FROM bookings GROUP BY booking_status";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats[reader["booking_status"].ToString()!] = Convert.ToInt32(reader["count"]);
                    }
                }
            }
            return stats;
        }

        private List<dynamic> GetTopRooms(SqlConnection conn)
        {
            List<dynamic> rooms = new List<dynamic>();
            string query = @"SELECT TOP 5 r.room_name, COUNT(b.booking_id) as booking_count
                           FROM rooms r
                           LEFT JOIN bookings b ON r.room_id = b.booking_room_id
                           GROUP BY r.room_name
                           ORDER BY booking_count DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new
                        {
                            room_name = reader["room_name"].ToString(),
                            booking_count = Convert.ToInt32(reader["booking_count"])
                        });
                    }
                }
            }
            return rooms;
        }

        private List<Location> GetActiveLocations()
        {
            List<Location> locations = new List<Location>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM locations WHERE location_is_active = 1 ORDER BY location_plant_name, location_block, location_floor";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new Location
                            {
                                location_id = Convert.ToInt32(reader["location_id"]),
                                location_code = reader["location_code"].ToString()!,
                                location_plant_name = reader["location_plant_name"].ToString()!,
                                location_block = Convert.ToByte(reader["location_block"]),
                                location_floor = Convert.ToByte(reader["location_floor"])
                            });
                        }
                    }
                }
            }
            return locations;
        }
    }
}