using Microsoft.AspNetCore.Mvc;
using IhsanRoomWise.Functions;

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

        [HttpGet]
        public IActionResult DashboardView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult FindRoomView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult MyBookingsView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult MyFeedbacksView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult NotificationsView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult MyActivityLogsView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ProfileView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }
    }
}