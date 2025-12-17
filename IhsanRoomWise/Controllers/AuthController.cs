// Controllers\AuthController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using IhsanRoomWise.Functions;
using IhsanRoomWise.Models;

namespace RoomWise.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController()
        {
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        // ============================================
        // KF-01: Login View (All users can access)
        // ============================================
        [HttpGet]
        public IActionResult LoginView()
        {
            // If already logged in, redirect to appropriate dashboard
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Admin")
                    return RedirectToAction("DashboardView", "Admin");
                else
                    return RedirectToAction("DashboardView", "Employee");
            }

            return View();
        }

        // ============================================
        // KF-02: Sign In (All users can sign in based on role)
        // ============================================
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT user_id, user_employee_id, user_email, user_password, user_full_name, 
                                    user_role, user_dept_name, user_is_active 
                                    FROM users 
                                    WHERE user_email = @Email AND user_is_active = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader["user_password"]?.ToString() ?? "";

                                // Verify password
                                if (SecHelperFunction.VerifyPassword(SecHelperFunction.HashPasswordMD5(password), storedPassword))
                                {
                                    // Set session variables
                                    HttpContext.Session.SetString("UserId", reader["user_id"].ToString()!);
                                    HttpContext.Session.SetString("UserEmployeeId", reader["user_employee_id"].ToString()!);
                                    HttpContext.Session.SetString("UserEmail", reader["user_email"].ToString()!);
                                    HttpContext.Session.SetString("UserFullName", reader["user_full_name"].ToString()!);
                                    HttpContext.Session.SetString("UserRole", reader["user_role"].ToString()!);
                                    HttpContext.Session.SetString("UserDeptName", reader["user_dept_name"]?.ToString() ?? "");

                                    string role = reader["user_role"].ToString()!;
                                    int userId = Convert.ToInt32(reader["user_id"]);

                                    reader.Close();

                                    // Log activity
                                    LogActivity(userId, "Login", "User logged in successfully");

                                    // Redirect based on role
                                    if (role == "Admin")
                                        return Json(new { success = true, redirectUrl = Url.Action("DashboardView", "Admin") });
                                    else
                                        return Json(new { success = true, redirectUrl = Url.Action("DashboardView", "Employee") });
                                }
                                else
                                {
                                    return Json(new { success = false, message = "Invalid email or password" });
                                }
                            }
                            else
                            {
                                return Json(new { success = false, message = "Invalid email or password" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // ============================================
        // KF-23: Logout (All users can logout)
        // ============================================
        [HttpPost]
        public IActionResult Logout()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId))
                {
                    LogActivity(Convert.ToInt32(userId), "Logout", "User logged out");
                }

                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = Url.Action("LoginView", "Auth") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private void LogActivity(int userId, string action, string description)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Check if activity_logs table exists, if not, silently fail
                    string checkTable = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'activity_logs')
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
                    
                    using (SqlCommand cmdCheck = new SqlCommand(checkTable, conn))
                    {
                        cmdCheck.ExecuteNonQuery();
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
                // Silent fail for logging
            }
        }
    }
}