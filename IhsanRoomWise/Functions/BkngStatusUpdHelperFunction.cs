// =====================================================
// Background Service for SQL Server Express
// (Use this if SQL Server Agent is not available)
// =====================================================

// File: Functions/BkngStatusUpdHelperFunction.cs
using Microsoft.Data.SqlClient;
using IhsanRoomWise.Functions;

namespace IhsanRoomWise.Functions
{
    public class BkngStatusUpdHelperFunction : BackgroundService
    {
        private readonly ILogger<BkngStatusUpdHelperFunction> _logger;
        private readonly string _connectionString;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(2); // Run every 2 minutes

        public BkngStatusUpdHelperFunction(ILogger<BkngStatusUpdHelperFunction> logger)
        {
            _logger = logger;
            _connectionString = new DbAccessFunction().GetConnectionString();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Status Update Service started at {time}", DateTimeOffset.Now);

            // Wait 30 seconds before first run (let app startup complete)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateBookingStatuses();
                    _logger.LogInformation("Next update in {minutes} minutes", _interval.TotalMinutes);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping, this is expected
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating booking statuses");
                    // Wait 1 minute before retry on error
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Booking Status Update Service stopped at {time}", DateTimeOffset.Now);
        }

        private async Task UpdateBookingStatuses()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var now = DateTime.Now;
                    var currentDate = now.Date;
                    var currentTime = now.TimeOfDay;
                    int systemUserId = 1; // System user for auto-cancellations

                    int cancelledCount = 0;
                    int inProgressCount = 0;
                    int completedCount = 0;

                    // 1. Auto-cancel expired pending bookings
                    string cancelQuery = @"
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
                            )";

                    using (SqlCommand cmd = new SqlCommand(cancelQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@SystemUserId", systemUserId);
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        
                        cancelledCount = await cmd.ExecuteNonQueryAsync();
                    }

                    // 2. Update to InProgress
                    string inProgressQuery = @"
                        UPDATE bookings
                        SET booking_status = 'InProgress',
                            booking_updated_at = GETDATE()
                        WHERE booking_status = 'Confirmed'
                            AND booking_date = @CurrentDate
                            AND booking_start_time <= @CurrentTime
                            AND booking_end_time > @CurrentTime";

                    using (SqlCommand cmd = new SqlCommand(inProgressQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        
                        inProgressCount = await cmd.ExecuteNonQueryAsync();
                    }

                    // 3. Update to Completed
                    string completedQuery = @"
                        UPDATE bookings
                        SET booking_status = 'Completed',
                            booking_updated_at = GETDATE()
                        WHERE booking_status IN ('InProgress', 'Confirmed')
                            AND (
                                (booking_date < @CurrentDate)
                                OR 
                                (booking_date = @CurrentDate AND booking_end_time <= @CurrentTime)
                            )";

                    using (SqlCommand cmd = new SqlCommand(completedQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                        cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                        
                        completedCount = await cmd.ExecuteNonQueryAsync();
                    }

                    int totalUpdates = cancelledCount + inProgressCount + completedCount;

                    if (totalUpdates > 0)
                    {
                        _logger.LogInformation(
                            "Booking Status Update: Cancelled={cancelled}, InProgress={inProgress}, Completed={completed}, Total={total}",
                            cancelledCount, inProgressCount, completedCount, totalUpdates);
                    }
                    else
                    {
                        _logger.LogDebug("Booking Status Update: No changes needed at {time}", now);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateBookingStatuses method");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Status Update Service is stopping...");
            await base.StopAsync(stoppingToken);
        }
    }
}

// =====================================================
// Add to Program.cs
// =====================================================
/*
Add this line in Program.cs after var builder = WebApplication.CreateBuilder(args);

using IhsanRoomWise.Services;

// ... other code ...

// Register the background service
builder.Services.AddHostedService<BkngStatusUpdHelperFunction>();

// ... rest of your code ...
*/

// =====================================================
// To verify it's running, check your application logs
// You should see messages like:
// - "Booking Status Update Service started"
// - "Booking Status Update: Cancelled=X, InProgress=Y, Completed=Z"
// =====================================================