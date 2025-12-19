// Controllers\EmployeeController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using IhsanRoomWise.Functions;
using IhsanRoomWise.Models;
using System.Data;

namespace RoomWise.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly string _connectionString;

        public EmployeeController()
        {
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        // ============================================
        // AUTHORIZATION CHECK
        // ============================================
        private bool CheckEmployeeAuth()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Employee";
        }

        // ============================================
        // Employee Dashboard
        // ============================================
        // REPLACE the existing DashboardView method with this enhanced version:
        [HttpGet]
        public IActionResult DashboardView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                var userId = HttpContext.Session.GetString("UserId");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Existing statistics
                    ViewBag.MyBookingsToday = GetCount(conn, $"SELECT COUNT(*) FROM bookings WHERE booking_user_id = {userId} AND booking_date = CAST(GETDATE() AS DATE)");
                    ViewBag.MyUpcomingBookings = GetCount(conn, $"SELECT COUNT(*) FROM bookings WHERE booking_user_id = {userId} AND booking_date >= CAST(GETDATE() AS DATE) AND booking_status IN ('Pending', 'Confirmed')");
                    ViewBag.MyPastBookings = GetCount(conn, $"SELECT COUNT(*) FROM bookings WHERE booking_user_id = {userId} AND booking_date < CAST(GETDATE() AS DATE)");
                    ViewBag.MyFeedbacksGiven = GetCount(conn, $"SELECT COUNT(*) FROM feedbacks WHERE feedback_user_id = {userId}");
                    ViewBag.AvailableRoomsNow = GetCount(conn, "SELECT COUNT(*) FROM rooms WHERE room_status = 'Available' AND room_is_active = 1");
                    
                    // NEW: Additional statistics
                    ViewBag.MyPendingBookings = GetCount(conn, $"SELECT COUNT(*) FROM bookings WHERE booking_user_id = {userId} AND booking_status = 'Pending'");
                    ViewBag.MyConfirmedBookings = GetCount(conn, $"SELECT COUNT(*) FROM bookings WHERE booking_user_id = {userId} AND booking_status = 'Confirmed'");
                    ViewBag.TotalRooms = GetCount(conn, "SELECT COUNT(*) FROM rooms WHERE room_is_active = 1");

                    // Existing data
                    ViewBag.UpcomingBookings = GetUserUpcomingBookings(conn, userId!);
                    ViewBag.RecentActivity = GetUserRecentActivity(conn, userId!);

                    // NEW: Enhanced dashboard data
                    ViewBag.WeeklySchedule = GetWeeklySchedule(conn, userId!);
                    ViewBag.MyBookingTrends = GetMyBookingTrends(conn, userId!);
                    ViewBag.AvailableRoomsNowList = GetAvailableRoomsNow(conn);
                    ViewBag.PopularRooms = GetPopularRooms(conn);
                    ViewBag.MyBookingsByStatus = GetMyBookingsByStatus(conn, userId!);
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading dashboard: " + ex.Message;
                return View();
            }
        }

        // Add these methods to EmployeeController.cs after the existing DashboardView method

        // Get weekly calendar data
        private List<dynamic> GetWeeklySchedule(SqlConnection conn, string userId)
        {
            string query = @"
                WITH DateRange AS (
                    SELECT 0 as day_offset UNION ALL SELECT 1 UNION ALL SELECT 2 
                    UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6
                )
                SELECT 
                    CAST(DATEADD(day, dr.day_offset, GETDATE()) AS DATE) as booking_date,
                    FORMAT(DATEADD(day, dr.day_offset, GETDATE()), 'ddd') as day_name,
                    FORMAT(DATEADD(day, dr.day_offset, GETDATE()), 'MMM dd') as date_str,
                    b.booking_id,
                    b.booking_title,
                    b.booking_status,
                    CONVERT(VARCHAR(5), b.booking_start_time, 108) as start_time,
                    CONVERT(VARCHAR(5), b.booking_end_time, 108) as end_time,
                    r.room_name,
                    r.room_code,
                    CASE WHEN b.booking_user_id = @UserId THEN 1 ELSE 0 END as is_mine,
                    CASE WHEN dr.day_offset = 0 THEN 1 ELSE 0 END as is_today
                FROM DateRange dr
                LEFT JOIN bookings b ON b.booking_date = CAST(DATEADD(day, dr.day_offset, GETDATE()) AS DATE)
                    AND b.booking_status IN ('Pending', 'Confirmed', 'InProgress')
                LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                ORDER BY dr.day_offset, b.booking_start_time";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            booking_date = reader["booking_date"],
                            day_name = reader["day_name"],
                            date_str = reader["date_str"],
                            booking_id = reader["booking_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["booking_id"]),
                            booking_title = reader["booking_title"] == DBNull.Value ? null : reader["booking_title"].ToString(),
                            booking_status = reader["booking_status"] == DBNull.Value ? null : reader["booking_status"].ToString(),
                            start_time = reader["start_time"] == DBNull.Value ? null : reader["start_time"].ToString(),
                            end_time = reader["end_time"] == DBNull.Value ? null : reader["end_time"].ToString(),
                            room_name = reader["room_name"] == DBNull.Value ? null : reader["room_name"].ToString(),
                            room_code = reader["room_code"] == DBNull.Value ? null : reader["room_code"].ToString(),
                            is_mine = reader["is_mine"] == DBNull.Value ? false : Convert.ToBoolean(reader["is_mine"]),
                            is_today = Convert.ToBoolean(reader["is_today"])
                        });
                    }
                }
            }
            return results;
        }

        // Get my booking trends (last 30 days)
        private List<dynamic> GetMyBookingTrends(SqlConnection conn, string userId)
        {
            string query = @"
                SELECT 
                    FORMAT(booking_date, 'MMM dd') as date_str,
                    COUNT(*) as count
                FROM bookings
                WHERE booking_user_id = @UserId
                AND booking_date >= DATEADD(day, -30, GETDATE())
                GROUP BY booking_date
                ORDER BY booking_date";

            var results = new List<dynamic>();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
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

        // Get available rooms right now
        private List<dynamic> GetAvailableRoomsNow(SqlConnection conn)
        {
            string query = @"
                SELECT TOP 6
                    r.room_id,
                    r.room_name,
                    r.room_code,
                    r.room_capacity,
                    r.room_facilities,
                    l.location_plant_name,
                    l.location_block,
                    l.location_floor,
                    (SELECT MIN(booking_start_time) 
                    FROM bookings 
                    WHERE booking_room_id = r.room_id 
                    AND booking_date = CAST(GETDATE() AS DATE)
                    AND booking_start_time > CAST(GETDATE() AS TIME)
                    AND booking_status IN ('Pending', 'Confirmed')) as next_booking_time
                FROM rooms r
                INNER JOIN locations l ON r.room_location_id = l.location_id
                WHERE r.room_is_active = 1 
                AND r.room_status = 'Available'
                AND NOT EXISTS (
                    SELECT 1 FROM bookings b
                    WHERE b.booking_room_id = r.room_id
                    AND b.booking_date = CAST(GETDATE() AS DATE)
                    AND b.booking_status IN ('Confirmed', 'InProgress')
                    AND CAST(GETDATE() AS TIME) BETWEEN b.booking_start_time AND b.booking_end_time
                )
                ORDER BY r.room_capacity DESC";

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
                            room_facilities = reader["room_facilities"],
                            location_plant_name = reader["location_plant_name"],
                            location_block = reader["location_block"],
                            location_floor = reader["location_floor"],
                            next_booking_time = reader["next_booking_time"] == DBNull.Value ? null : 
                                TimeSpan.Parse(reader["next_booking_time"].ToString()!).ToString(@"hh\:mm")
                        });
                    }
                }
            }
            return results;
        }

        // Get popular rooms this month
        private List<dynamic> GetPopularRooms(SqlConnection conn)
        {
            string query = @"
                SELECT TOP 5
                    r.room_name,
                    r.room_code,
                    COUNT(b.booking_id) as booking_count,
                    AVG(CAST(r.room_capacity AS FLOAT)) as avg_capacity
                FROM bookings b
                INNER JOIN rooms r ON b.booking_room_id = r.room_id
                WHERE b.booking_date >= DATEADD(day, -30, GETDATE())
                AND b.booking_status IN ('Confirmed', 'Completed')
                GROUP BY r.room_id, r.room_name, r.room_code
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
                            room_name = reader["room_name"],
                            room_code = reader["room_code"],
                            booking_count = reader["booking_count"]
                        });
                    }
                }
            }
            return results;
        }

        // Get my booking status breakdown
        private Dictionary<string, int> GetMyBookingsByStatus(SqlConnection conn, string userId)
        {
            var stats = new Dictionary<string, int>();
            string query = @"
                SELECT booking_status, COUNT(*) as count 
                FROM bookings 
                WHERE booking_user_id = @UserId
                AND booking_date >= DATEADD(day, -30, GETDATE())
                GROUP BY booking_status";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
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

        // ============================================
        // KF-15, KF-16, KF-22: Find Room View (Search and Filter)
        // ============================================
        [HttpGet]
        public IActionResult FindRoomView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                // Get all locations for filter
                ViewBag.Locations = GetActiveLocations();

                // Get all available rooms
                List<dynamic> rooms = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT r.*, l.location_code, l.location_plant_name, l.location_block, l.location_floor
                                   FROM rooms r
                                   LEFT JOIN locations l ON r.room_location_id = l.location_id
                                   WHERE r.room_is_active = 1
                                   ORDER BY r.room_name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rooms.Add(new
                                {
                                    room_id = Convert.ToInt32(reader["room_id"]),
                                    room_code = reader["room_code"].ToString(),
                                    room_name = reader["room_name"].ToString(),
                                    room_capacity = Convert.ToInt32(reader["room_capacity"]),
                                    room_facilities = reader["room_facilities"]?.ToString(),
                                    room_status = reader["room_status"].ToString(),
                                    location_code = reader["location_code"]?.ToString(),
                                    location_plant_name = reader["location_plant_name"]?.ToString(),
                                    location_block = reader["location_block"] != DBNull.Value ? Convert.ToInt32(reader["location_block"]) : 0,
                                    location_floor = reader["location_floor"] != DBNull.Value ? Convert.ToInt32(reader["location_floor"]) : 0,
                                    room_location_id = Convert.ToInt32(reader["room_location_id"])
                                });
                            }
                        }
                    }
                }

                return View(rooms);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading rooms: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        // KF-16: Search and Filter Rooms
        [HttpGet]
        public IActionResult SearchRooms(int? locationId, int? minCapacity, string? facilities, string? status, DateTime? date, TimeSpan? startTime, TimeSpan? endTime)
        {
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                List<dynamic> rooms = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"SELECT r.*, l.location_code, l.location_plant_name, l.location_block, l.location_floor
                                   FROM rooms r
                                   LEFT JOIN locations l ON r.room_location_id = l.location_id
                                   WHERE r.room_is_active = 1";

                    // Build dynamic query based on filters
                    if (locationId.HasValue)
                        query += " AND r.room_location_id = @LocationId";
                    
                    if (minCapacity.HasValue)
                        query += " AND r.room_capacity >= @MinCapacity";
                    
                    if (!string.IsNullOrEmpty(facilities))
                        query += " AND r.room_facilities LIKE @Facilities";
                    
                    if (!string.IsNullOrEmpty(status))
                        query += " AND r.room_status = @Status";

                    query += " ORDER BY r.room_name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (locationId.HasValue)
                            cmd.Parameters.AddWithValue("@LocationId", locationId.Value);
                        
                        if (minCapacity.HasValue)
                            cmd.Parameters.AddWithValue("@MinCapacity", minCapacity.Value);
                        
                        if (!string.IsNullOrEmpty(facilities))
                            cmd.Parameters.AddWithValue("@Facilities", "%" + facilities + "%");
                        
                        if (!string.IsNullOrEmpty(status))
                            cmd.Parameters.AddWithValue("@Status", status);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomId = Convert.ToInt32(reader["room_id"]);
                                
                                // Check availability if date and time are provided
                                bool isAvailable = true;
                                if (date.HasValue && startTime.HasValue && endTime.HasValue)
                                {
                                    isAvailable = CheckRoomAvailability(roomId, date.Value, startTime.Value, endTime.Value);
                                }

                                rooms.Add(new
                                {
                                    room_id = roomId,
                                    room_code = reader["room_code"].ToString(),
                                    room_name = reader["room_name"].ToString(),
                                    room_capacity = Convert.ToInt32(reader["room_capacity"]),
                                    room_facilities = reader["room_facilities"]?.ToString(),
                                    room_status = reader["room_status"].ToString(),
                                    location_code = reader["location_code"]?.ToString(),
                                    location_plant_name = reader["location_plant_name"]?.ToString(),
                                    location_block = reader["location_block"] != DBNull.Value ? Convert.ToInt32(reader["location_block"]) : 0,
                                    location_floor = reader["location_floor"] != DBNull.Value ? Convert.ToInt32(reader["location_floor"]) : 0,
                                    is_available = isAvailable
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, rooms = rooms });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Check room availability for specific date and time
        [HttpGet]
        public IActionResult CheckAvailability(int roomId, DateTime date, string startTime, string endTime)
        {
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);

                bool isAvailable = CheckRoomAvailability(roomId, date, start, end);

                return Json(new { success = true, available = isAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-17: Create Booking
        // ============================================
        [HttpPost]
        public IActionResult CreateBooking(int roomId, string title, string description, DateTime date, string startTime, string endTime)
        {
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);

                // Validate time
                if (start >= end)
                    return Json(new { success = false, message = "End time must be after start time" });

                // Check if date is not in the past
                if (date.Date < DateTime.Now.Date)
                    return Json(new { success = false, message = "Cannot book for past dates" });

                // Check room availability
                if (!CheckRoomAvailability(roomId, date, start, end))
                    return Json(new { success = false, message = "Room is not available for the selected time slot" });

                // Check room status
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string checkRoomQuery = "SELECT room_status FROM rooms WHERE room_id = @RoomId AND room_is_active = 1";
                    using (SqlCommand checkCmd = new SqlCommand(checkRoomQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@RoomId", roomId);
                        var roomStatus = checkCmd.ExecuteScalar()?.ToString();
                        
                        if (roomStatus != "Available")
                            return Json(new { success = false, message = $"Room is currently {roomStatus}" });
                    }

                    // Generate booking code
                    string bookingCode = GenerateBookingCode();

                    // Create booking
                    string query = @"INSERT INTO bookings (booking_code, booking_user_id, booking_room_id, booking_title, 
                                   booking_description, booking_date, booking_start_time, booking_end_time, 
                                   booking_status, booking_created_at, booking_updated_at)
                                   VALUES (@Code, @UserId, @RoomId, @Title, @Description, @Date, @StartTime, 
                                   @EndTime, 'Pending', GETDATE(), GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", bookingCode);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@Date", date.Date);
                        cmd.Parameters.AddWithValue("@StartTime", start);
                        cmd.Parameters.AddWithValue("@EndTime", end);

                        cmd.ExecuteNonQuery();

                        LogActivity("Create Booking", $"Created booking: {bookingCode}");

                        return Json(new { success = true, message = "Booking created successfully. Waiting for admin approval.", bookingCode = bookingCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-19: My Bookings View
        // ============================================
        [HttpGet]
        public IActionResult MyBookingsView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                List<dynamic> bookings = new List<dynamic>();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT b.*, r.room_code, r.room_name, l.location_plant_name
                                   FROM bookings b
                                   LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                                   LEFT JOIN locations l ON r.room_location_id = l.location_id
                                   WHERE b.booking_user_id = @UserId
                                   ORDER BY b.booking_date DESC, b.booking_start_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
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
                                    booking_cancel_reason = reader["booking_cancel_reason"]?.ToString(),
                                    room_code = reader["room_code"]?.ToString(),
                                    room_name = reader["room_name"]?.ToString(),
                                    location_plant_name = reader["location_plant_name"]?.ToString()
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

        // ============================================
        // KF-18: Update/Cancel Booking
        // ============================================
        [HttpPost]
        public IActionResult UpdateBooking(int bookingId, string title, string description, DateTime date, string startTime, string endTime)
        {
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);

                // Validate ownership and status
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string checkQuery = "SELECT booking_user_id, booking_status, booking_room_id FROM bookings WHERE booking_id = @BookingId";
                    int ownerId = 0;
                    string status = "";
                    int roomId = 0;

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@BookingId", bookingId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ownerId = Convert.ToInt32(reader["booking_user_id"]);
                                status = reader["booking_status"].ToString()!;
                                roomId = Convert.ToInt32(reader["booking_room_id"]);
                            }
                        }
                    }

                    if (ownerId.ToString() != userId)
                        return Json(new { success = false, message = "You can only modify your own bookings" });

                    if (status == "Completed" || status == "Cancelled")
                        return Json(new { success = false, message = "Cannot modify completed or cancelled bookings" });

                    // Check availability for new time slot (excluding current booking)
                    if (!CheckRoomAvailability(roomId, date, start, end, bookingId))
                        return Json(new { success = false, message = "Room is not available for the selected time slot" });

                    // Update booking
                    string query = @"UPDATE bookings 
                                   SET booking_title = @Title,
                                       booking_description = @Description,
                                       booking_date = @Date,
                                       booking_start_time = @StartTime,
                                       booking_end_time = @EndTime,
                                       booking_status = 'Pending',
                                       booking_updated_at = GETDATE()
                                   WHERE booking_id = @BookingId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@Date", date.Date);
                        cmd.Parameters.AddWithValue("@StartTime", start);
                        cmd.Parameters.AddWithValue("@EndTime", end);
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);

                        cmd.ExecuteNonQuery();

                        LogActivity("Update Booking", $"Updated booking ID: {bookingId}");

                        return Json(new { success = true, message = "Booking updated successfully. Waiting for admin re-approval." });
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
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var userId = HttpContext.Session.GetString("UserId");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Validate ownership
                    string checkQuery = "SELECT booking_user_id, booking_status FROM bookings WHERE booking_id = @BookingId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@BookingId", bookingId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader["booking_user_id"].ToString() != userId)
                                    return Json(new { success = false, message = "You can only cancel your own bookings" });

                                string status = reader["booking_status"].ToString()!;
                                if (status == "Completed" || status == "Cancelled")
                                    return Json(new { success = false, message = "Cannot cancel completed or already cancelled bookings" });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Booking not found" });
                            }
                        }
                    }

                    // Cancel booking
                    string query = @"UPDATE bookings 
                                   SET booking_status = 'Cancelled',
                                       booking_cancel_reason = @Reason,
                                       booking_cancelled_by = @CancelledBy,
                                       booking_updated_at = GETDATE()
                                   WHERE booking_id = @BookingId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Reason", reason);
                        cmd.Parameters.AddWithValue("@CancelledBy", userId);
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);

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
        // KF-20: My Feedbacks View
        // ============================================
        [HttpGet]
        public IActionResult MyFeedbacksView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                List<dynamic> feedbacks = new List<dynamic>();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT f.*, b.booking_code, b.booking_title, b.booking_date, r.room_name
                                   FROM feedbacks f
                                   LEFT JOIN bookings b ON f.feedback_booking_id = b.booking_id
                                   LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                                   WHERE f.feedback_user_id = @UserId
                                   ORDER BY f.feedback_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
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
                                    booking_date = reader["booking_date"] != DBNull.Value ? Convert.ToDateTime(reader["booking_date"]) : (DateTime?)null,
                                    room_name = reader["room_name"]?.ToString()
                                });
                            }
                        }
                    }
                }

                // Get completed bookings without feedback
                ViewBag.BookingsWithoutFeedback = GetCompletedBookingsWithoutFeedback(userId!);

                return View(feedbacks);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading feedbacks: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

        // KF-20: Submit Feedback
        [HttpPost]
        public IActionResult SubmitFeedback(int bookingId, int rating, string comments)
        {
            if (!CheckEmployeeAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var userId = HttpContext.Session.GetString("UserId");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Validate booking ownership and status
                    string checkQuery = @"SELECT booking_user_id, booking_status 
                                        FROM bookings 
                                        WHERE booking_id = @BookingId";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@BookingId", bookingId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader["booking_user_id"].ToString() != userId)
                                    return Json(new { success = false, message = "You can only provide feedback for your own bookings" });

                                string status = reader["booking_status"].ToString()!;
                                if (status != "Completed")
                                    return Json(new { success = false, message = "Can only provide feedback for completed bookings" });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Booking not found" });
                            }
                        }
                    }

                    // Check if feedback already exists
                    string checkFeedbackQuery = "SELECT COUNT(*) FROM feedbacks WHERE feedback_booking_id = @BookingId";
                    using (SqlCommand checkFeedbackCmd = new SqlCommand(checkFeedbackQuery, conn))
                    {
                        checkFeedbackCmd.Parameters.AddWithValue("@BookingId", bookingId);
                        int count = (int)checkFeedbackCmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Feedback already submitted for this booking" });
                    }

                    // Insert feedback
                    string query = @"INSERT INTO feedbacks (feedback_booking_id, feedback_user_id, feedback_rating, 
                                   feedback_comments, feedback_created_at)
                                   VALUES (@BookingId, @UserId, @Rating, @Comments, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Rating", rating);
                        cmd.Parameters.AddWithValue("@Comments", string.IsNullOrEmpty(comments) ? (object)DBNull.Value : comments);

                        cmd.ExecuteNonQuery();

                        LogActivity("Submit Feedback", $"Submitted feedback for booking ID: {bookingId}");

                        return Json(new { success = true, message = "Feedback submitted successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================
        // KF-21: Notifications View
        // ============================================
        [HttpGet]
        public IActionResult NotificationsView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            // Placeholder for notifications feature
            return View();
        }

        // ============================================
        // My Activity Logs View
        // ============================================
        [HttpGet]
        public IActionResult MyActivityLogsView()
        {
            if (!CheckEmployeeAuth())
                return RedirectToAction("LoginView", "Auth");

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
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

                    string query = @"SELECT * FROM activity_logs 
                                   WHERE log_user_id = @UserId 
                                   ORDER BY log_created_at DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.Add(new
                                {
                                    log_id = Convert.ToInt32(reader["log_id"]),
                                    log_action = reader["log_action"].ToString(),
                                    log_description = reader["log_description"]?.ToString(),
                                    log_created_at = Convert.ToDateTime(reader["log_created_at"])
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
            if (!CheckEmployeeAuth())
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
            if (!CheckEmployeeAuth())
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
            if (!CheckEmployeeAuth())
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

                        if (!SecHelperFunction.VerifyPassword(currentPassword, storedPassword))
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

        private bool CheckRoomAvailability(int roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string query = @"SELECT COUNT(*) FROM bookings 
                                   WHERE booking_room_id = @RoomId 
                                   AND booking_date = @Date
                                   AND booking_status IN ('Pending', 'Confirmed', 'InProgress')
                                   AND (
                                       (booking_start_time < @EndTime AND booking_end_time > @StartTime)
                                   )";
                    
                    if (excludeBookingId.HasValue)
                        query += " AND booking_id != @ExcludeBookingId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.Parameters.AddWithValue("@Date", date.Date);
                        cmd.Parameters.AddWithValue("@StartTime", startTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        
                        if (excludeBookingId.HasValue)
                            cmd.Parameters.AddWithValue("@ExcludeBookingId", excludeBookingId.Value);

                        int conflictCount = (int)cmd.ExecuteScalar();
                        return conflictCount == 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private string GenerateBookingCode()
        {
            return "BK" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
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

        private List<dynamic> GetUserUpcomingBookings(SqlConnection conn, string userId)
        {
            List<dynamic> bookings = new List<dynamic>();
            string query = @"SELECT TOP 5 b.*, r.room_name
                           FROM bookings b
                           LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                           WHERE b.booking_user_id = @UserId 
                           AND b.booking_date >= CAST(GETDATE() AS DATE)
                           AND b.booking_status IN ('Pending', 'Confirmed')
                           ORDER BY b.booking_date, b.booking_start_time";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookings.Add(new
                        {
                            booking_id = Convert.ToInt32(reader["booking_id"]),
                            booking_title = reader["booking_title"].ToString(),
                            room_name = reader["room_name"]?.ToString(),
                            booking_date = Convert.ToDateTime(reader["booking_date"]),
                            booking_start_time = reader["booking_start_time"] != DBNull.Value
                                ? TimeSpan.Parse(reader["booking_start_time"].ToString()!)
                                    .ToString(@"hh\:mm")
                                : "-",
                            booking_end_time = reader["booking_end_time"] != DBNull.Value
                                ? TimeSpan.Parse(reader["booking_end_time"].ToString()!)
                                    .ToString(@"hh\:mm")
                                : "-",
                            booking_status = reader["booking_status"].ToString()
                        });
                    }
                }
            }
            return bookings;
        }

        private List<dynamic> GetUserRecentActivity(SqlConnection conn, string userId)
        {
            List<dynamic> activities = new List<dynamic>();
            
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

            string query = @"SELECT TOP 10 * FROM activity_logs 
                           WHERE log_user_id = @UserId 
                           ORDER BY log_created_at DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activities.Add(new
                        {
                            log_action = reader["log_action"].ToString(),
                            log_description = reader["log_description"]?.ToString(),
                            log_created_at = Convert.ToDateTime(reader["log_created_at"])
                        });
                    }
                }
            }
            return activities;
        }

        private List<dynamic> GetCompletedBookingsWithoutFeedback(string userId)
        {
            List<dynamic> bookings = new List<dynamic>();
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"SELECT b.*, r.room_name
                               FROM bookings b
                               LEFT JOIN rooms r ON b.booking_room_id = r.room_id
                               LEFT JOIN feedbacks f ON b.booking_id = f.feedback_booking_id
                               WHERE b.booking_user_id = @UserId 
                               AND b.booking_status = 'Completed'
                               AND f.feedback_id IS NULL
                               ORDER BY b.booking_date DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookings.Add(new
                            {
                                booking_id = Convert.ToInt32(reader["booking_id"]),
                                booking_code = reader["booking_code"].ToString(),
                                booking_title = reader["booking_title"].ToString(),
                                room_name = reader["room_name"]?.ToString(),
                                booking_date = Convert.ToDateTime(reader["booking_date"])
                            });
                        }
                    }
                }
            }
            
            return bookings;
        }
    }
}