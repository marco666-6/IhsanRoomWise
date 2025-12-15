using Microsoft.AspNetCore.Mvc;
using IhsanRoomWise.Functions;

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
        public IActionResult UsersView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult RoomsView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult BookingsView()
        {
            if (!CheckAuth())
            {
                return RedirectToAction("LoginView", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult FeedbacksView()
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
        public IActionResult ActivityLogsView()
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