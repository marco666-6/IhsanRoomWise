// Functions\EmailServiceFunction.cs

using System.Net;
using System.Net.Mail;

namespace IhsanRoomWise.Functions
{
    public class EmailServiceFunction
    {
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "ihsannabawi669@gmail.com"; // CHANGE THIS
        private readonly string _smtpPassword = "ezcl fatw jsfq axcq"; // CHANGE THIS
        private readonly string _fromEmail = "ihsannabawi669@gmail.com"; // CHANGE THIS
        private readonly string _fromName = "RoomWise System";

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(new MailAddress(toEmail, toName));
                    message.Subject = subject;
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;
                    message.Priority = MailPriority.Normal;

                    using (var client = new SmtpClient(_smtpHost, _smtpPort))
                    {
                        client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                        client.EnableSsl = true;
                        client.Timeout = 30000; // 30 seconds

                        await client.SendMailAsync(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        // Email Templates with Green Theme
        public string GetBookingCreatedTemplate(string userName, string bookingCode, string roomName, string date, string startTime, string endTime)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f0f8f0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(34, 139, 34, 0.15); }}
        .header {{ background: linear-gradient(135deg, #2d5016 0%, #3a7d23 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.95; font-size: 14px; }}
        .content {{ padding: 40px 30px; }}
        .greeting {{ font-size: 18px; color: #2d5016; margin-bottom: 20px; }}
        .message {{ color: #555; line-height: 1.8; margin-bottom: 30px; }}
        .booking-card {{ background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%); border-left: 4px solid #2d5016; padding: 25px; border-radius: 8px; margin: 25px 0; }}
        .booking-detail {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid rgba(45, 80, 22, 0.1); }}
        .booking-detail:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: 600; color: #2d5016; }}
        .detail-value {{ color: #555; }}
        .status-badge {{ display: inline-block; background: #ffd54f; color: #5d4037; padding: 6px 16px; border-radius: 20px; font-size: 13px; font-weight: 600; margin: 20px 0; }}
        .footer {{ background: #f5f5f5; padding: 30px; text-align: center; color: #777; font-size: 13px; }}
        .footer-links {{ margin: 15px 0; }}
        .footer-links a {{ color: #2d5016; text-decoration: none; margin: 0 10px; }}
        .divider {{ height: 2px; background: linear-gradient(90deg, transparent, #2d5016, transparent); margin: 30px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎯 Booking Created Successfully</h1>
            <p>Your meeting room reservation is awaiting approval</p>
        </div>
        <div class='content'>
            <div class='greeting'>Hello {userName},</div>
            <div class='message'>
                Your booking request has been submitted successfully! Our admin team will review it shortly. You'll receive a confirmation email once it's approved.
            </div>
            <div class='booking-card'>
                <div class='booking-detail'>
                    <span class='detail-label'>Booking Code:</span>
                    <span class='detail-value'><strong>{bookingCode}</strong></span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Room:</span>
                    <span class='detail-value'>{roomName}</span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Date:</span>
                    <span class='detail-value'>{date}</span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{startTime} - {endTime}</span>
                </div>
            </div>
            <div style='text-align: center;'>
                <span class='status-badge'>⏳ PENDING APPROVAL</span>
            </div>
            <div class='divider'></div>
            <div class='message' style='font-size: 14px; color: #777;'>
                <strong>Next Steps:</strong><br>
                • Wait for admin approval (usually within 24 hours)<br>
                • Check your email for confirmation<br>
                • Prepare for your meeting!
            </div>
        </div>
        <div class='footer'>
            <div>© 2025 RoomWise System. All rights reserved.</div>
            <div class='footer-links'>
                <a href='#'>View Dashboard</a> | <a href='#'>Contact Support</a>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public string GetBookingApprovedTemplate(string userName, string bookingCode, string roomName, string date, string startTime, string endTime)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f0f8f0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(34, 139, 34, 0.15); }}
        .header {{ background: linear-gradient(135deg, #2d5016 0%, #4caf50 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.95; font-size: 14px; }}
        .content {{ padding: 40px 30px; }}
        .greeting {{ font-size: 18px; color: #2d5016; margin-bottom: 20px; }}
        .message {{ color: #555; line-height: 1.8; margin-bottom: 30px; }}
        .booking-card {{ background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%); border-left: 4px solid #4caf50; padding: 25px; border-radius: 8px; margin: 25px 0; }}
        .booking-detail {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid rgba(76, 175, 80, 0.1); }}
        .booking-detail:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: 600; color: #2d5016; }}
        .detail-value {{ color: #555; }}
        .status-badge {{ display: inline-block; background: #4caf50; color: white; padding: 8px 20px; border-radius: 20px; font-size: 13px; font-weight: 600; margin: 20px 0; }}
        .footer {{ background: #f5f5f5; padding: 30px; text-align: center; color: #777; font-size: 13px; }}
        .divider {{ height: 2px; background: linear-gradient(90deg, transparent, #4caf50, transparent); margin: 30px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Booking Confirmed!</h1>
            <p>Your meeting room has been approved</p>
        </div>
        <div class='content'>
            <div class='greeting'>Great news, {userName}! 🎉</div>
            <div class='message'>
                Your booking request has been <strong>approved</strong>! The meeting room is now reserved for you. Make sure to arrive on time and enjoy your session.
            </div>
            <div class='booking-card'>
                <div class='booking-detail'>
                    <span class='detail-label'>Booking Code:</span>
                    <span class='detail-value'><strong>{bookingCode}</strong></span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Room:</span>
                    <span class='detail-value'>{roomName}</span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Date:</span>
                    <span class='detail-value'>{date}</span>
                </div>
                <div class='booking-detail'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{startTime} - {endTime}</span>
                </div>
            </div>
            <div style='text-align: center;'>
                <span class='status-badge'>✓ CONFIRMED</span>
            </div>
            <div class='divider'></div>
            <div class='message' style='font-size: 14px; color: #777;'>
                <strong>Important Reminders:</strong><br>
                • Arrive 5 minutes early<br>
                • Bring necessary materials<br>
                • Leave the room clean after use
            </div>
        </div>
        <div class='footer'>
            © 2025 RoomWise System. All rights reserved.
        </div>
    </div>
</body>
</html>";
        }

        public string GetBookingCancelledTemplate(string userName, string bookingCode, string roomName, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f0f8f0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(34, 139, 34, 0.15); }}
        .header {{ background: linear-gradient(135deg, #c62828 0%, #d32f2f 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.95; font-size: 14px; }}
        .content {{ padding: 40px 30px; }}
        .greeting {{ font-size: 18px; color: #2d5016; margin-bottom: 20px; }}
        .message {{ color: #555; line-height: 1.8; margin-bottom: 30px; }}
        .reason-card {{ background: #ffebee; border-left: 4px solid #d32f2f; padding: 20px; border-radius: 8px; margin: 25px 0; }}
        .reason-label {{ font-weight: 600; color: #c62828; margin-bottom: 10px; }}
        .reason-text {{ color: #555; }}
        .status-badge {{ display: inline-block; background: #d32f2f; color: white; padding: 8px 20px; border-radius: 20px; font-size: 13px; font-weight: 600; margin: 20px 0; }}
        .footer {{ background: #f5f5f5; padding: 30px; text-align: center; color: #777; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Booking Cancelled</h1>
            <p>Your reservation has been cancelled</p>
        </div>
        <div class='content'>
            <div class='greeting'>Hello {userName},</div>
            <div class='message'>
                We regret to inform you that your booking <strong>{bookingCode}</strong> for <strong>{roomName}</strong> has been cancelled.
            </div>
            <div class='reason-card'>
                <div class='reason-label'>Cancellation Reason:</div>
                <div class='reason-text'>{reason}</div>
            </div>
            <div style='text-align: center;'>
                <span class='status-badge'>✗ CANCELLED</span>
            </div>
            <div class='message' style='font-size: 14px; color: #777; margin-top: 30px;'>
                You can create a new booking anytime from your dashboard. If you have questions, please contact our support team.
            </div>
        </div>
        <div class='footer'>
            © 2025 RoomWise System. All rights reserved.
        </div>
    </div>
</body>
</html>";
        }

        public string GetPasswordResetTemplate(string userName, string employeeId, string newPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f0f8f0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(34, 139, 34, 0.15); }}
        .header {{ background: linear-gradient(135deg, #2d5016 0%, #3a7d23 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .password-box {{ background: #e8f5e9; border: 2px dashed #2d5016; padding: 20px; border-radius: 8px; text-align: center; margin: 25px 0; }}
        .password {{ font-size: 24px; font-weight: 700; color: #2d5016; letter-spacing: 2px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ff9800; padding: 15px; margin: 20px 0; color: #856404; }}
        .footer {{ background: #f5f5f5; padding: 30px; text-align: center; color: #777; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset</h1>
            <p>Your password has been reset by an administrator</p>
        </div>
        <div class='content'>
            <div style='font-size: 18px; color: #2d5016; margin-bottom: 20px;'>Hello {userName},</div>
            <div style='color: #555; line-height: 1.8; margin-bottom: 30px;'>
                Your password has been reset by an administrator. Please use the temporary password below to log in.
            </div>
            <div class='password-box'>
                <div style='color: #555; font-size: 14px; margin-bottom: 10px;'>Your New Password</div>
                <div class='password'>{newPassword}</div>
            </div>
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong> Please change this password immediately after logging in for security purposes.
            </div>
            <div style='color: #777; font-size: 14px; margin-top: 30px;'>
                <strong>Login Credentials:</strong><br>
                Employee ID: <strong>{employeeId}</strong><br>
                Password: <strong>{newPassword}</strong>
            </div>
        </div>
        <div class='footer'>
            © 2025 RoomWise System. All rights reserved.
        </div>
    </div>
</body>
</html>";
        }
    }
}