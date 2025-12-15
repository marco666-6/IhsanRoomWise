// Controllers\EmployeeController.cs

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
    public class EmployeeController : Controller
    {
        private readonly string _connectionString;

        public EmployeeController()
        {
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        private bool CheckAuth()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userId == null || userRole != "Employee")
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
            
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var dashboardData = new
            {
                UpcomingBookings = 0,
                TodayBookings = 0,
                PendingBookings = 0,
                TotalBookings = 0
            };
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Get upcoming bookings
                    string upcomingQuery = @"
                        SELECT COUNT(*) 
                        FROM bookings 
                        WHERE booking_user_id = @UserId 
                            AND booking_date >= CAST(GETDATE() AS DATE)
                            AND booking_status IN ('Pending', 'Confirmed')";
                    
                    using (SqlCommand cmd = new SqlCommand(upcomingQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        dashboardData = new
                        {
                            UpcomingBookings = Convert.ToInt32(cmd.ExecuteScalar()),
                            TodayBookings = 0,
                            PendingBookings = 0,
                            TotalBookings = 0
                        };
                    }
                    
                    // Get today's bookings
                    string todayQuery = @"
                        SELECT COUNT(*) 
                        FROM bookings 
                        WHERE booking_user_id = @UserId 
                            AND booking_date = CAST(GETDATE() AS DATE)
                            AND booking_status IN ('Pending', 'Confirmed', 'In Progress')";
                    
                    using (SqlCommand cmd = new SqlCommand(todayQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int todayBookings = Convert.ToInt32(cmd.ExecuteScalar());
                        dashboardData = new
                        {
                            dashboardData.UpcomingBookings,
                            TodayBookings = todayBookings,
                            dashboardData.PendingBookings,
                            dashboardData.TotalBookings
                        };
                    }
                    
                    // Get pending bookings
                    string pendingQuery = "SELECT COUNT(*) FROM bookings WHERE booking_user_id = @UserId AND booking_status = 'Pending'";
                    
                    using (SqlCommand cmd = new SqlCommand(pendingQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int pendingBookings = Convert.ToInt32(cmd.ExecuteScalar());
                        dashboardData = new
                        {
                            dashboardData.UpcomingBookings,
                            dashboardData.TodayBookings,
                            PendingBookings = pendingBookings,
                            dashboardData.TotalBookings
                        };
                    }
                    
                    // Get total bookings
                    string totalQuery = "SELECT COUNT(*) FROM bookings WHERE booking_user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(totalQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int totalBookings = Convert.ToInt32(cmd.ExecuteScalar());
                        dashboardData = new
                        {
                            dashboardData.UpcomingBookings,
                            dashboardData.TodayBookings,
                            dashboardData.PendingBookings,
                            TotalBookings = totalBookings
                        };
                    }
                }
            }
            catch { }
            
            return View(dashboardData);
        }

        // ==================== FIND ROOMS ====================
        [HttpGet]
        public IActionResult FindRoomView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetAvailableRooms(DateTime date, TimeSpan startTime, TimeSpan endTime, 
            int? locationId = null, int? minCapacity = null, int? maxCapacity = null,
            bool? hasProjector = null, bool? hasSmartScreen = null, bool? hasScreenbeam = null, bool? hasCiscoBar = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // First, get all active rooms that match the criteria
                    StringBuilder roomQuery = new StringBuilder(@"
                        SELECT r.*, l.location_code, l.location_plant_name, l.location_block, l.location_floor
                        FROM rooms r
                        INNER JOIN locations l ON r.room_location_id = l.location_id
                        WHERE r.room_is_active = 1 
                            AND r.room_operational_status = 'Available'
                            AND l.location_is_active = 1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    
                    if (locationId.HasValue)
                    {
                        roomQuery.Append(" AND r.room_location_id = @LocationId");
                        parameters.Add(new SqlParameter("@LocationId", locationId.Value));
                    }
                    
                    if (minCapacity.HasValue)
                    {
                        roomQuery.Append(" AND r.room_capacity >= @MinCapacity");
                        parameters.Add(new SqlParameter("@MinCapacity", minCapacity.Value));
                    }
                    
                    if (maxCapacity.HasValue)
                    {
                        roomQuery.Append(" AND r.room_capacity <= @MaxCapacity");
                        parameters.Add(new SqlParameter("@MaxCapacity", maxCapacity.Value));
                    }
                    
                    if (hasProjector.HasValue)
                    {
                        roomQuery.Append(" AND r.room_has_projector = @HasProjector");
                        parameters.Add(new SqlParameter("@HasProjector", hasProjector.Value));
                    }
                    
                    if (hasSmartScreen.HasValue)
                    {
                        roomQuery.Append(" AND r.room_has_smart_screen = @HasSmartScreen");
                        parameters.Add(new SqlParameter("@HasSmartScreen", hasSmartScreen.Value));
                    }
                    
                    if (hasScreenbeam.HasValue)
                    {
                        roomQuery.Append(" AND r.room_has_screenbeam = @HasScreenbeam");
                        parameters.Add(new SqlParameter("@HasScreenbeam", hasScreenbeam.Value));
                    }
                    
                    if (hasCiscoBar.HasValue)
                    {
                        roomQuery.Append(" AND r.room_has_cisco_bar = @HasCiscoBar");
                        parameters.Add(new SqlParameter("@HasCiscoBar", hasCiscoBar.Value));
                    }
                    
                    roomQuery.Append(" ORDER BY r.room_name");
                    
                    DataTable roomsTable = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(roomQuery.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(roomsTable);
                        }
                    }
                    
                    // Check availability for each room
                    var availableRooms = new List<object>();
                    
                    foreach (DataRow room in roomsTable.Rows)
                    {
                        int roomId = Convert.ToInt32(room["room_id"]);
                        
                        using (SqlCommand checkCmd = new SqlCommand("sp_check_room_availability", conn))
                        {
                            checkCmd.CommandType = CommandType.StoredProcedure;
                            checkCmd.Parameters.AddWithValue("@room_id", roomId);
                            checkCmd.Parameters.AddWithValue("@booking_date", date);
                            checkCmd.Parameters.AddWithValue("@start_time", startTime);
                            checkCmd.Parameters.AddWithValue("@end_time", endTime);
                            checkCmd.Parameters.AddWithValue("@exclude_booking_id", DBNull.Value);
                            
                            int conflictCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            
                            if (conflictCount == 0) // Room is available
                            {
                                availableRooms.Add(new
                                {
                                    room_id = roomId,
                                    room_code = room["room_code"],
                                    room_name = room["room_name"],
                                    room_capacity = room["room_capacity"],
                                    room_description = room["room_description"],
                                    room_has_projector = Convert.ToBoolean(room["room_has_projector"]),
                                    room_has_smart_screen = Convert.ToBoolean(room["room_has_smart_screen"]),
                                    room_has_screenbeam = Convert.ToBoolean(room["room_has_screenbeam"]),
                                    room_has_cisco_bar = Convert.ToBoolean(room["room_has_cisco_bar"]),
                                    room_other_facilities = room["room_other_facilities"],
                                    location_code = room["location_code"],
                                    location_plant_name = room["location_plant_name"],
                                    location_block = room["location_block"],
                                    location_floor = room["location_floor"]
                                });
                            }
                        }
                    }
                    
                    return Json(new { success = true, data = availableRooms });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetRoomSchedule(int roomId, DateTime date)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_get_room_schedule", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        cmd.Parameters.AddWithValue("@booking_date", date);
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var schedule = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            schedule.Add(new
                            {
                                booking_id = row["booking_id"],
                                booking_code = row["booking_code"],
                                booking_start_time = TimeSpan.Parse(row["booking_start_time"].ToString()).ToString(@"hh\:mm"),
                                booking_end_time = TimeSpan.Parse(row["booking_end_time"].ToString()).ToString(@"hh\:mm"),
                                booking_meeting_title = row["booking_meeting_title"],
                                booking_status = row["booking_status"],
                                user_full_name = row["user_full_name"],
                                is_current_booking = Convert.ToBoolean(row["is_current_booking"])
                            });
                        }
                        
                        return Json(new { success = true, data = schedule });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAvailableTimeSlots(int roomId, DateTime date, int durationMinutes = 30)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_get_available_time_slots", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        cmd.Parameters.AddWithValue("@booking_date", date);
                        cmd.Parameters.AddWithValue("@slot_duration_minutes", durationMinutes);
                        
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                        
                        var slots = new List<object>();
                        foreach (DataRow row in dt.Rows)
                        {
                            slots.Add(new
                            {
                                slot_start_time = TimeSpan.Parse(row["slot_start_time"].ToString()).ToString(@"hh\:mm"),
                                slot_end_time = TimeSpan.Parse(row["slot_end_time"].ToString()).ToString(@"hh\:mm"),
                                is_available = Convert.ToBoolean(row["is_available"]),
                                occupied_by = row["occupied_by"]
                            });
                        }
                        
                        return Json(new { success = true, data = slots });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== CREATE BOOKING ====================
        [HttpPost]
        public IActionResult CreateBooking([FromBody] BookingModel booking)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                booking.booking_user_id = userId;
                
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
                            booking_meeting_description, booking_date, booking_start_time, booking_end_time
                        ) VALUES (
                            @Code, @UserId, @RoomId, @Title,
                            @Description, @Date, @StartTime, @EndTime
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
                        
                        int bookingId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Booking", "Booking", bookingId, $"Created booking {booking.booking_code}");
                        
                        return Json(new { success = true, message = "Booking created successfully", bookingId = bookingId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== MY BOOKINGS ====================
        [HttpGet]
        public IActionResult MyBookingsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetMyBookings(string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT b.*, r.room_name, r.room_code, l.location_plant_name, l.location_block, l.location_floor
                        FROM bookings b
                        INNER JOIN rooms r ON b.booking_room_id = r.room_id
                        INNER JOIN locations l ON r.room_location_id = l.location_id
                        WHERE b.booking_user_id = @UserId");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", userId));
                    
                    if (!string.IsNullOrEmpty(status))
                    {
                        query.Append(" AND b.booking_status = @Status");
                        parameters.Add(new SqlParameter("@Status", status));
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
                                room_name = row["room_name"],
                                room_code = row["room_code"],
                                location_plant_name = row["location_plant_name"],
                                location_block = row["location_block"],
                                location_floor = row["location_floor"],
                                can_edit = row["booking_status"].ToString() == "Pending" || row["booking_status"].ToString() == "Confirmed",
                                can_cancel = row["booking_status"].ToString() == "Pending" || row["booking_status"].ToString() == "Confirmed",
                                can_finish = row["booking_status"].ToString() == "In Progress"
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

        [HttpGet]
        public IActionResult GetUpcomingBookings()
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_get_user_upcoming_bookings", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@days_ahead", 30);
                        
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
                                booking_date = Convert.ToDateTime(row["booking_date"]).ToString("yyyy-MM-dd"),
                                booking_start_time = TimeSpan.Parse(row["booking_start_time"].ToString()).ToString(@"hh\:mm"),
                                booking_end_time = TimeSpan.Parse(row["booking_end_time"].ToString()).ToString(@"hh\:mm"),
                                booking_meeting_title = row["booking_meeting_title"],
                                booking_status = row["booking_status"],
                                room_name = row["room_name"],
                                room_code = row["room_code"],
                                location_plant_name = row["location_plant_name"],
                                location_block = row["location_block"],
                                location_floor = row["location_floor"],
                                days_until_meeting = row["days_until_meeting"],
                                is_today = Convert.ToBoolean(row["is_today"])
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
        public IActionResult UpdateMyBooking(int bookingId, [FromBody] BookingModel booking)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                // Verify ownership
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string verifyQuery = "SELECT booking_status FROM bookings WHERE booking_id = @BookingId AND booking_user_id = @UserId";
                    string currentStatus = "";
                    
                    using (SqlCommand cmd = new SqlCommand(verifyQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Booking not found or unauthorized" });
                            }
                            currentStatus = reader["booking_status"].ToString();
                        }
                    }
                    
                    if (currentStatus != "Pending" && currentStatus != "Confirmed")
                    {
                        return Json(new { success = false, message = "Only pending or confirmed bookings can be updated" });
                    }
                    
                    // Check for conflicts (excluding this booking)
                    using (SqlCommand cmd = new SqlCommand("sp_check_room_availability", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@room_id", booking.booking_room_id);
                        cmd.Parameters.AddWithValue("@booking_date", booking.booking_date);
                        cmd.Parameters.AddWithValue("@start_time", booking.booking_start_time);
                        cmd.Parameters.AddWithValue("@end_time", booking.booking_end_time);
                        cmd.Parameters.AddWithValue("@exclude_booking_id", bookingId);
                        
                        int conflictCount = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (conflictCount > 0)
                        {
                            return Json(new { success = false, message = "Room is already booked for this time slot" });
                        }
                    }
                    
                    // Update booking
                    string updateQuery = @"
                        UPDATE bookings SET 
                            booking_room_id = @RoomId,
                            booking_meeting_title = @Title,
                            booking_meeting_description = @Description,
                            booking_date = @Date,
                            booking_start_time = @StartTime,
                            booking_end_time = @EndTime,
                            booking_updated_at = GETDATE()
                        WHERE booking_id = @BookingId AND booking_user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@RoomId", booking.booking_room_id);
                        cmd.Parameters.AddWithValue("@Title", booking.booking_meeting_title);
                        cmd.Parameters.AddWithValue("@Description", (object?)booking.booking_meeting_description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Date", booking.booking_date);
                        cmd.Parameters.AddWithValue("@StartTime", booking.booking_start_time);
                        cmd.Parameters.AddWithValue("@EndTime", booking.booking_end_time);
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Update Booking", "Booking", bookingId, "Updated booking details");
                        
                        return Json(new { success = true, message = "Booking updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CancelMyBooking(int bookingId, string reason)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE bookings SET 
                            booking_status = 'Cancelled',
                            booking_cancellation_reason = @Reason,
                            booking_cancelled_at = GETDATE(),
                            booking_cancelled_by = @UserId,
                            booking_updated_at = GETDATE()
                        WHERE booking_id = @BookingId AND booking_user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Reason", reason);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            LogActivity("Cancel Booking", "Booking", bookingId, "Cancelled booking");
                            return Json(new { success = true, message = "Booking cancelled successfully" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Booking not found or unauthorized" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult FinishMeetingEarly(int bookingId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Verify booking is in progress and belongs to user
                    string verifyQuery = "SELECT booking_status FROM bookings WHERE booking_id = @BookingId AND booking_user_id = @UserId";
                    string status = "";
                    
                    using (SqlCommand cmd = new SqlCommand(verifyQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Booking not found or unauthorized" });
                            }
                            status = reader["booking_status"].ToString();
                        }
                    }
                    
                    if (status != "In Progress")
                    {
                        return Json(new { success = false, message = "Only meetings in progress can be finished early" });
                    }
                    
                    // Update booking
                    string updateQuery = @"
                        UPDATE bookings SET 
                            booking_status = 'Completed',
                            booking_actual_end_time = GETDATE(),
                            booking_updated_at = GETDATE()
                        WHERE booking_id = @BookingId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", bookingId);
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Finish Meeting Early", "Booking", bookingId, "Finished meeting early");
                        
                        return Json(new { success = true, message = "Meeting finished successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportMyBookings(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return RedirectToAction("MyBookingsView");
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT 
                            b.booking_code as 'Booking Code',
                            b.booking_meeting_title as 'Meeting Title',
                            FORMAT(b.booking_date, 'dd/MM/yyyy') as 'Date',
                            FORMAT(b.booking_start_time, 'hh\:mm') as 'Start Time',
                            FORMAT(b.booking_end_time, 'hh\:mm') as 'End Time',
                            b.booking_status as 'Status',
                            r.room_name as 'Room',
                            r.room_code as 'Room Code',
                            CONCAT(l.location_plant_name, ' - Block ', l.location_block, ' - Floor ', l.location_floor) as 'Location'
                        FROM bookings b
                        INNER JOIN rooms r ON b.booking_room_id = r.room_id
                        INNER JOIN locations l ON r.room_location_id = l.location_id
                        WHERE b.booking_user_id = @UserId");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", userId));
                    
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
                        
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("My Bookings");
                            
                            // Add title
                            worksheet.Cell(1, 1).Value = "RoomWise - My Bookings Report";
                            worksheet.Cell(1, 1).Style.Font.Bold = true;
                            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                            worksheet.Range(1, 1, 1, dt.Columns.Count).Merge();
                            
                            // Add export date
                            worksheet.Cell(2, 1).Value = $"Exported on: {DateTime.Now:dd/MM/yyyy HH:mm}";
                            worksheet.Range(2, 1, 2, dt.Columns.Count).Merge();
                            
                            // Add user info
                            worksheet.Cell(3, 1).Value = $"Employee: {HttpContext.Session.GetString("UserFullName")} ({HttpContext.Session.GetString("UserEmployeeId")})";
                            worksheet.Range(3, 1, 3, dt.Columns.Count).Merge();
                            
                            // Add headers starting from row 5
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                worksheet.Cell(5, i + 1).Value = dt.Columns[i].ColumnName;
                                worksheet.Cell(5, i + 1).Style.Font.Bold = true;
                                worksheet.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                                worksheet.Cell(5, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                            
                            // Add data
                            for (int row = 0; row < dt.Rows.Count; row++)
                            {
                                for (int col = 0; col < dt.Columns.Count; col++)
                                {
                                    worksheet.Cell(row + 6, col + 1).Value = dt.Rows[row][col].ToString();
                                    worksheet.Cell(row + 6, col + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                    
                                    // Color code status
                                    if (col == 5) // Status column
                                    {
                                        string status = dt.Rows[row][col].ToString();
                                        XLColor statusColor = status switch
                                        {
                                            "Confirmed" => XLColor.Green,
                                            "Pending" => XLColor.Yellow,
                                            "In Progress" => XLColor.Blue,
                                            "Completed" => XLColor.LightGray,
                                            "Cancelled" => XLColor.Red,
                                            _ => XLColor.White
                                        };
                                        worksheet.Cell(row + 6, col + 1).Style.Fill.BackgroundColor = statusColor;
                                    }
                                    else if (row % 2 == 0)
                                    {
                                        worksheet.Cell(row + 6, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
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
                                
                                LogActivity("Export Data", "Booking", null, "Exported personal bookings to Excel");
                                
                                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                                    $"My_Bookings_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to export bookings";
                return RedirectToAction("MyBookingsView");
            }
        }

        // ==================== MY FEEDBACKS ====================
        [HttpGet]
        public IActionResult MyFeedbacksView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetMyFeedbacks(int? roomId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT f.*, r.room_name, r.room_code, b.booking_date
                        FROM feedbacks f
                        INNER JOIN rooms r ON f.feedback_room_id = r.room_id
                        INNER JOIN bookings b ON f.feedback_booking_id = b.booking_id
                        WHERE f.feedback_user_id = @UserId");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", userId));
                    
                    if (roomId.HasValue)
                    {
                        query.Append(" AND f.feedback_room_id = @RoomId");
                        parameters.Add(new SqlParameter("@RoomId", roomId.Value));
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
                                room_name = row["room_name"],
                                room_code = row["room_code"],
                                booking_date = Convert.ToDateTime(row["booking_date"]).ToString("yyyy-MM-dd"),
                                feedback_admin_responded_at = row["feedback_admin_responded_at"],
                                can_edit = string.IsNullOrEmpty(row["feedback_admin_response"]?.ToString())
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
        public IActionResult CreateFeedback([FromBody] FeedbackModel feedback)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                feedback.feedback_user_id = userId;
                
                // Check if feedback already exists for this booking
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string checkQuery = "SELECT COUNT(*) FROM feedbacks WHERE feedback_booking_id = @BookingId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@BookingId", feedback.feedback_booking_id);
                        int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                        
                        if (existingCount > 0)
                        {
                            return Json(new { success = false, message = "Feedback already submitted for this booking" });
                        }
                    }
                    
                    // Create feedback
                    string query = @"
                        INSERT INTO feedbacks (
                            feedback_booking_id, feedback_user_id, feedback_room_id,
                            feedback_rating, feedback_room_condition, feedback_facility_condition,
                            feedback_issues_reported
                        ) VALUES (
                            @BookingId, @UserId, @RoomId,
                            @Rating, @RoomCondition, @FacilityCondition,
                            @IssuesReported
                        );
                        SELECT SCOPE_IDENTITY();";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookingId", feedback.feedback_booking_id);
                        cmd.Parameters.AddWithValue("@UserId", feedback.feedback_user_id);
                        cmd.Parameters.AddWithValue("@RoomId", feedback.feedback_room_id);
                        cmd.Parameters.AddWithValue("@Rating", feedback.feedback_rating);
                        cmd.Parameters.AddWithValue("@RoomCondition", (object?)feedback.feedback_room_condition ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FacilityCondition", (object?)feedback.feedback_facility_condition ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IssuesReported", (object?)feedback.feedback_issues_reported ?? DBNull.Value);
                        
                        int feedbackId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        LogActivity("Create Feedback", "Feedback", feedbackId, "Submitted feedback for booking");
                        
                        return Json(new { success = true, message = "Feedback submitted successfully", feedbackId = feedbackId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateFeedback(int feedbackId, [FromBody] FeedbackModel feedback)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                // Check ownership and if admin has responded
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string checkQuery = "SELECT feedback_admin_response FROM feedbacks WHERE feedback_id = @FeedbackId AND feedback_user_id = @UserId";
                    string adminResponse = "";
                    
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@FeedbackId", feedbackId);
                        checkCmd.Parameters.AddWithValue("@UserId", userId);
                        
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Feedback not found or unauthorized" });
                            }
                            adminResponse = reader["feedback_admin_response"]?.ToString() ?? "";
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(adminResponse))
                    {
                        return Json(new { success = false, message = "Cannot edit feedback after admin response" });
                    }
                    
                    // Update feedback
                    string updateQuery = @"
                        UPDATE feedbacks SET 
                            feedback_rating = @Rating,
                            feedback_room_condition = @RoomCondition,
                            feedback_facility_condition = @FacilityCondition,
                            feedback_issues_reported = @IssuesReported,
                            feedback_updated_at = GETDATE()
                        WHERE feedback_id = @FeedbackId AND feedback_user_id = @UserId";
                    
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FeedbackId", feedbackId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Rating", feedback.feedback_rating);
                        cmd.Parameters.AddWithValue("@RoomCondition", (object?)feedback.feedback_room_condition ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FacilityCondition", (object?)feedback.feedback_facility_condition ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IssuesReported", (object?)feedback.feedback_issues_reported ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                        
                        LogActivity("Update Feedback", "Feedback", feedbackId, "Updated feedback");
                        
                        return Json(new { success = true, message = "Feedback updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== NOTIFICATIONS ====================
        [HttpGet]
        public IActionResult NotificationsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetMyNotifications(bool? isRead = null, DateTime? fromDate = null, DateTime? toDate = null)
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
                        WHERE (n.notification_target_role = 'Employee' OR n.notification_target_role = 'All' 
                               OR n.notification_target_user_id = @UserId)
                        AND 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", HttpContext.Session.GetInt32("UserId")));
                    
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
                        WHERE notification_target_role IN ('Employee', 'All') 
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

        // ==================== MY ACTIVITY LOGS ====================
        [HttpGet]
        public IActionResult MyActivityLogsView()
        {
            if (!CheckAuth()) return RedirectToAction("LoginView", "Auth");
            return View();
        }

        [HttpGet]
        public IActionResult GetMyActivityLogs(string? actionType = null, string? entityType = null, 
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    StringBuilder query = new StringBuilder(@"
                        SELECT al.*
                        FROM activity_logs al
                        WHERE al.log_user_id = @UserId");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@UserId", userId));
                    
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