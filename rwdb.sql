CREATE DATABASE [rwdb]
GO
USE [rwdb]
GO
/****** Object:  Table [dbo].[activity_logs]    Script Date: 12/22/2025 8:37:21 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[activity_logs](
	[log_id] [int] IDENTITY(1,1) NOT NULL,
	[log_user_id] [int] NOT NULL,
	[log_action] [nvarchar](100) NOT NULL,
	[log_description] [nvarchar](500) NOT NULL,
	[log_created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[log_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[bookings]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bookings](
	[booking_id] [int] IDENTITY(1,1) NOT NULL,
	[booking_code] [nvarchar](30) NOT NULL,
	[booking_user_id] [int] NOT NULL,
	[booking_room_id] [int] NOT NULL,
	[booking_title] [nvarchar](200) NOT NULL,
	[booking_description] [nvarchar](500) NULL,
	[booking_date] [date] NOT NULL,
	[booking_start_time] [time](7) NOT NULL,
	[booking_end_time] [time](7) NOT NULL,
	[booking_status] [nvarchar](20) NOT NULL,
	[booking_cancel_reason] [nvarchar](500) NULL,
	[booking_cancelled_by] [int] NULL,
	[booking_created_at] [datetime2](7) NULL,
	[booking_updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[booking_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[feedbacks]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[feedbacks](
	[feedback_id] [int] IDENTITY(1,1) NOT NULL,
	[feedback_booking_id] [int] NOT NULL,
	[feedback_user_id] [int] NOT NULL,
	[feedback_rating] [tinyint] NOT NULL,
	[feedback_comments] [nvarchar](500) NULL,
	[feedback_admin_response] [nvarchar](500) NULL,
	[feedback_admin_responded_by] [int] NULL,
	[feedback_responded_at] [datetime2](7) NULL,
	[feedback_created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[feedback_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[locations]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[locations](
	[location_id] [int] IDENTITY(1,1) NOT NULL,
	[location_code] [nvarchar](15) NOT NULL,
	[location_plant_name] [nvarchar](50) NOT NULL,
	[location_block] [tinyint] NOT NULL,
	[location_floor] [tinyint] NOT NULL,
	[location_is_active] [bit] NOT NULL,
	[location_created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[location_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[notifications]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[notifications](
	[notification_id] [int] IDENTITY(1,1) NOT NULL,
	[notification_title] [nvarchar](200) NOT NULL,
	[notification_message] [nvarchar](1000) NOT NULL,
	[notification_type] [nvarchar](20) NOT NULL,
	[notification_target_role] [nvarchar](20) NOT NULL,
	[notification_is_active] [bit] NOT NULL,
	[notification_created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[notification_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rooms]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rooms](
	[room_id] [int] IDENTITY(1,1) NOT NULL,
	[room_code] [nvarchar](20) NOT NULL,
	[room_name] [nvarchar](100) NOT NULL,
	[room_location_id] [int] NOT NULL,
	[room_capacity] [int] NOT NULL,
	[room_facilities] [nvarchar](500) NULL,
	[room_status] [nvarchar](20) NOT NULL,
	[room_is_active] [bit] NOT NULL,
	[room_created_at] [datetime2](7) NULL,
	[room_updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[room_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[users]    Script Date: 12/22/2025 8:37:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[users](
	[user_id] [int] IDENTITY(1,1) NOT NULL,
	[user_employee_id] [nvarchar](20) NOT NULL,
	[user_email] [nvarchar](100) NOT NULL,
	[user_password] [nvarchar](255) NOT NULL,
	[user_full_name] [nvarchar](150) NOT NULL,
	[user_role] [nvarchar](20) NOT NULL,
	[user_dept_name] [nvarchar](100) NULL,
	[user_is_active] [bit] NOT NULL,
	[user_created_at] [datetime2](7) NULL,
	[user_updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[activity_logs] ON 
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (1, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-17T15:27:49.0700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (2, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:16:36.0966667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (3, 1, N'Export Data', N'Exported users data to Excel', CAST(N'2025-12-18T07:17:01.7200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (4, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:19:58.2800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (5, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:26:55.4133333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (6, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:28:20.2666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (7, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:30:30.6533333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (8, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T07:35:42.4433333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (9, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:38:24.2366667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (10, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T07:38:29.5333333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (11, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:45:09.2833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (12, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:48:31.8833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (13, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T07:55:16.7066667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (14, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T08:06:45.1366667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (15, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T08:22:53.3266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (16, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T08:23:44.5233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (17, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T08:23:47.6666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (18, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T08:25:52.3833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (19, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T08:25:54.9033333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (20, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T08:26:18.2766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (21, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T09:02:52.5266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (22, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T10:25:33.6400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (23, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T10:25:36.2600000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (24, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T10:27:13.5500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (25, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T12:38:43.3900000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (26, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T12:41:27.7633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (27, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T12:41:30.3866667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (28, 2, N'Create Booking', N'Created booking: BK202512181242313644', CAST(N'2025-12-18T12:42:31.7833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (29, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T12:42:50.6700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (30, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T12:42:53.7133333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (31, 1, N'Approve Booking', N'Approved booking ID: 0', CAST(N'2025-12-18T12:43:15.7733333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (32, 1, N'Approve Booking', N'Approved booking ID: 0', CAST(N'2025-12-18T12:44:41.6800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (33, 1, N'Approve Booking', N'Approved booking ID: 0', CAST(N'2025-12-18T12:44:48.4900000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (34, 1, N'Approve Booking', N'Approved booking ID: 2', CAST(N'2025-12-18T12:45:00.7033333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (35, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T14:20:26.3166667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (36, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T14:20:29.9000000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (37, 2, N'Create Booking', N'Created booking: BK202512181421102087', CAST(N'2025-12-18T14:21:10.5733333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (38, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T14:21:15.7233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (39, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T14:21:18.1500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (40, 1, N'Approve Booking', N'Approved booking ID: 3', CAST(N'2025-12-18T14:22:09.0500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (41, 1, N'Cancel Booking', N'Cancelled booking ID: 3', CAST(N'2025-12-18T14:23:02.1466667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (42, 1, N'Update User', N'Updated user ID: 2', CAST(N'2025-12-18T14:59:23.3033333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (43, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T15:22:25.3666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (44, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T15:26:49.4800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (45, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T15:28:45.7700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (46, 1, N'Update Room', N'Updated room ID: 1', CAST(N'2025-12-18T16:03:55.3833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (47, 1, N'Update Room', N'Updated room ID: 1', CAST(N'2025-12-18T16:04:04.0800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (48, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T16:29:00.9133333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (49, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T16:29:03.0766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (50, 1, N'Logout', N'User logged out', CAST(N'2025-12-18T16:29:22.4333333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (51, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-18T16:29:28.2400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (52, 2, N'Create Booking', N'Created booking: BK202512181630321810', CAST(N'2025-12-18T16:30:32.4633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (53, 2, N'Logout', N'User logged out', CAST(N'2025-12-18T16:30:45.8366667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (54, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-18T16:30:48.9000000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (55, 1, N'Approve Booking', N'Approved booking ID: 4', CAST(N'2025-12-18T16:31:15.6600000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (56, 1, N'Cancel Booking', N'Cancelled booking ID: 4', CAST(N'2025-12-18T16:31:37.2800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (57, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T07:21:17.1533333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (58, 1, N'Cancel Booking', N'Cancelled booking ID: 2', CAST(N'2025-12-19T07:28:26.8400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (59, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T08:04:19.4233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (60, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:04:22.5500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (61, 2, N'Create Booking', N'Created booking: BK202512190805184946', CAST(N'2025-12-19T08:05:18.3866667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (62, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T08:05:23.2900000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (63, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:05:26.1133333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (64, 1, N'Reset Password', N'Reset password for user ID: 3', CAST(N'2025-12-19T08:07:20.6433333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (65, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T08:07:32.4200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (66, 3, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:07:45.4400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (67, 3, N'Create Booking', N'Created booking: BK202512190808114472', CAST(N'2025-12-19T08:08:11.4966667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (68, 3, N'Logout', N'User logged out', CAST(N'2025-12-19T08:08:15.6666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (69, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:08:18.9333333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (70, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T08:08:43.7800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (71, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:08:51.8500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (72, 2, N'Create Booking', N'Created booking: BK202512190809291792', CAST(N'2025-12-19T08:09:29.7200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (73, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T08:09:40.6966667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (74, 3, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:09:43.0533333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (75, 3, N'Create Booking', N'Created booking: BK202512190810404750', CAST(N'2025-12-19T08:10:40.9700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (76, 3, N'Logout', N'User logged out', CAST(N'2025-12-19T08:10:44.7100000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (77, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T08:10:48.5633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (78, 1, N'Update Room Status', N'Changed room ID 1 operational status to Available', CAST(N'2025-12-19T08:11:30.3633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (79, 1, N'Update Room Status', N'Changed room ID 3 operational status to Maintenance', CAST(N'2025-12-19T08:11:35.4033333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (80, 1, N'Update Room Status', N'Changed room ID 3 operational status to Available', CAST(N'2025-12-19T08:33:53.9633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (81, 1, N'Update Room Status', N'Changed room ID 3 operational status to Maintenance', CAST(N'2025-12-19T08:34:05.8200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (82, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T09:28:30.6800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (83, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T09:28:35.4766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (84, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T09:39:42.4800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (85, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T09:39:46.2100000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (86, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:10:47.4433333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (87, 1, N'Change Password', N'Changed own password', CAST(N'2025-12-19T10:11:07.0833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (88, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:11:15.7600000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (89, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:11:19.6266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (90, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:20:41.6233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (91, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:21:39.8100000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (92, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:22:05.0666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (93, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:22:11.7233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (94, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:34:22.4533333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (95, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:34:30.2433333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (96, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:34:44.4600000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (97, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T10:34:49.7333333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (98, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:34:52.8700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (99, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:39:15.0700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (100, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:39:16.0400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (101, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T10:39:27.4200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (102, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T10:44:44.6466667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (103, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T11:00:23.3800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (104, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T11:00:30.0066667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (105, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:20:52.7766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (106, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:29:15.4266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (107, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:33:09.4933333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (108, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:36:27.1800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (109, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:40:11.2800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (110, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:44:22.2366667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (111, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T13:58:43.9800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (112, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T14:16:08.5400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (113, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:16:11.8566667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (114, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T14:20:20.4833333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (115, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:20:41.2200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (116, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T14:21:09.3666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (117, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:21:12.8566667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (118, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T14:21:25.8766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (119, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:21:29.8200000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (120, 2, N'Logout', N'User logged out', CAST(N'2025-12-19T14:21:38.0666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (121, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:21:41.9300000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (122, 1, N'Logout', N'User logged out', CAST(N'2025-12-19T14:21:49.4966667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (123, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:22:01.0700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (124, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T14:50:02.5866667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (125, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T16:09:38.9400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (126, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-19T16:30:10.1566667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (127, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T07:52:19.2500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (128, 1, N'Update Profile', N'Updated own profile information', CAST(N'2025-12-22T07:52:34.8500000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (129, 1, N'Logout', N'User logged out', CAST(N'2025-12-22T07:52:39.7066667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (130, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-22T07:52:51.3700000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (131, 2, N'Update Profile', N'Updated own profile information', CAST(N'2025-12-22T07:53:17.0800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (132, 2, N'Change Password', N'Changed own password', CAST(N'2025-12-22T07:53:25.9900000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (133, 2, N'Logout', N'User logged out', CAST(N'2025-12-22T07:53:35.7733333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (134, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T07:53:51.8066667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (135, 1, N'Export Data', N'Exported users data to Excel', CAST(N'2025-12-22T07:53:59.9800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (136, 1, N'Export Data', N'Exported bookings data to Excel', CAST(N'2025-12-22T07:58:10.8133333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (137, 1, N'Reset Password', N'Reset password for user ID: 2', CAST(N'2025-12-22T07:59:33.1633333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (138, 1, N'Logout', N'User logged out', CAST(N'2025-12-22T07:59:46.3266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (139, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T08:05:13.2966667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (140, 1, N'Reset Password', N'Reset password for user ID: 2', CAST(N'2025-12-22T08:05:28.4800000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (141, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T08:08:17.3666667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (142, 1, N'Reset Password', N'Reset password for user ID: 2', CAST(N'2025-12-22T08:08:30.0766667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (143, 1, N'Logout', N'User logged out', CAST(N'2025-12-22T08:09:00.1733333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (144, 2, N'Login', N'User logged in successfully', CAST(N'2025-12-22T08:09:08.6400000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (145, 2, N'Create Booking', N'Created booking: BK202512220809446885', CAST(N'2025-12-22T08:09:44.2233333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (146, 2, N'Create Booking', N'Created booking: BK202512220819251203', CAST(N'2025-12-22T08:19:25.4533333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (147, 2, N'Logout', N'User logged out', CAST(N'2025-12-22T08:20:02.8333333' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (148, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T08:20:10.5100000' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (149, 1, N'Login', N'User logged in successfully', CAST(N'2025-12-22T08:23:51.4266667' AS DateTime2))
GO
INSERT [dbo].[activity_logs] ([log_id], [log_user_id], [log_action], [log_description], [log_created_at]) VALUES (150, 1, N'Export Data', N'Exported rooms data to Excel', CAST(N'2025-12-22T08:27:03.8900000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[activity_logs] OFF
GO
SET IDENTITY_INSERT [dbo].[bookings] ON 
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (1, N'BK-20251220-001', 2, 1, N'Project Review Meeting', NULL, CAST(N'2025-12-20' AS Date), CAST(N'10:00:00' AS Time), CAST(N'11:30:00' AS Time), N'Completed', NULL, NULL, CAST(N'2025-12-17T13:51:18.6233333' AS DateTime2), CAST(N'2025-12-22T07:52:27.9800000' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (2, N'BK202512181242313644', 2, 2, N'Dance', N'Donce', CAST(N'2025-12-18' AS Date), CAST(N'22:41:00' AS Time), CAST(N'23:42:00' AS Time), N'Cancelled', N'Not healthy', 1, CAST(N'2025-12-18T12:42:31.6966667' AS DateTime2), CAST(N'2025-12-19T07:28:26.7233333' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (3, N'BK202512181421102087', 2, 2, N'Dancey', N'y', CAST(N'2025-12-18' AS Date), CAST(N'20:20:00' AS Time), CAST(N'21:20:00' AS Time), N'Cancelled', N'You orderin twice? No!', 1, CAST(N'2025-12-18T14:21:10.4800000' AS DateTime2), CAST(N'2025-12-18T14:23:02.0600000' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (4, N'BK202512181630321810', 2, 1, N'wow', N'wew', CAST(N'2025-12-18' AS Date), CAST(N'22:30:00' AS Time), CAST(N'23:30:00' AS Time), N'Cancelled', N'no', 1, CAST(N'2025-12-18T16:30:32.3000000' AS DateTime2), CAST(N'2025-12-18T16:31:37.1800000' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (5, N'BK202512190805184946', 2, 2, N'Dancey', N'haha must!', CAST(N'2025-12-19' AS Date), CAST(N'11:04:00' AS Time), CAST(N'11:46:00' AS Time), N'Cancelled', N'Not reviewed by admin before meeting time.', 1, CAST(N'2025-12-19T08:05:18.2900000' AS DateTime2), CAST(N'2025-12-22T07:52:27.9366667' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (6, N'BK202512190808114472', 3, 2, N'Momsy', N'Must', CAST(N'2025-12-19' AS Date), CAST(N'22:07:00' AS Time), CAST(N'23:08:00' AS Time), N'Cancelled', N'Not reviewed by admin before meeting time.', 1, CAST(N'2025-12-19T08:08:11.4200000' AS DateTime2), CAST(N'2025-12-22T07:52:27.9366667' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (7, N'BK202512190809291792', 2, 1, N'Popsy', N'must', CAST(N'2025-12-19' AS Date), CAST(N'13:09:00' AS Time), CAST(N'14:09:00' AS Time), N'Cancelled', N'Not reviewed by admin before meeting time.', 1, CAST(N'2025-12-19T08:09:29.6233333' AS DateTime2), CAST(N'2025-12-22T07:52:27.9366667' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (8, N'BK202512190810404750', 3, 1, N'Akame', N'Ga Kirru', CAST(N'2025-12-19' AS Date), CAST(N'15:03:00' AS Time), CAST(N'17:10:00' AS Time), N'Cancelled', N'Not reviewed by admin before meeting time.', 1, CAST(N'2025-12-19T08:10:40.8666667' AS DateTime2), CAST(N'2025-12-22T07:52:27.9366667' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (9, N'BK202512220809446885', 2, 2, N'Yopsi', N'Die at 3 A.M.', CAST(N'2025-12-22' AS Date), CAST(N'10:09:00' AS Time), CAST(N'11:09:00' AS Time), N'Pending', NULL, NULL, CAST(N'2025-12-22T08:09:44.1166667' AS DateTime2), CAST(N'2025-12-22T08:09:44.1166667' AS DateTime2))
GO
INSERT [dbo].[bookings] ([booking_id], [booking_code], [booking_user_id], [booking_room_id], [booking_title], [booking_description], [booking_date], [booking_start_time], [booking_end_time], [booking_status], [booking_cancel_reason], [booking_cancelled_by], [booking_created_at], [booking_updated_at]) VALUES (10, N'BK202512220819251203', 2, 1, N'yy', N'yy', CAST(N'2025-12-22' AS Date), CAST(N'12:19:00' AS Time), CAST(N'13:19:00' AS Time), N'Pending', NULL, NULL, CAST(N'2025-12-22T08:19:25.3466667' AS DateTime2), CAST(N'2025-12-22T08:19:25.3466667' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[bookings] OFF
GO
SET IDENTITY_INSERT [dbo].[locations] ON 
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (1, N'PLANT-A-B1-F1', N'Plant A', 1, 1, 1, CAST(N'2025-12-17T13:51:18.6166667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (2, N'PLANT-A-B1-F2', N'Plant A', 1, 2, 1, CAST(N'2025-12-17T13:51:18.6166667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (3, N'PLANT-A-B1-F3', N'Plant A', 1, 3, 1, CAST(N'2025-12-17T13:51:18.6166667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (4, N'PLANT-1-B1-F1', N'Plant 1', 1, 1, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (5, N'PLANT-1-B1-F2', N'Plant 1', 1, 2, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (6, N'PLANT-1-B2-F1', N'Plant 1', 2, 1, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (7, N'PLANT-1-B2-F2', N'Plant 1', 2, 2, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (8, N'PLANT-1-B3-F1', N'Plant 1', 3, 1, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (9, N'PLANT-1-B3-F2', N'Plant 1', 3, 2, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (10, N'PLANT-1-B4-F1', N'Plant 1', 4, 1, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (11, N'PLANT-1-B4-F2', N'Plant 1', 4, 2, 1, CAST(N'2025-12-22T08:36:18.2333333' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (12, N'PLANT-2-B5-F1', N'Plant 2', 5, 1, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (13, N'PLANT-2-B5-F2', N'Plant 2', 5, 2, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (14, N'PLANT-2-B6-F1', N'Plant 2', 6, 1, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (15, N'PLANT-2-B6-F2', N'Plant 2', 6, 2, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (16, N'PLANT-2-B7-F1', N'Plant 2', 7, 1, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (17, N'PLANT-2-B7-F2', N'Plant 2', 7, 2, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (18, N'PLANT-2-B8-F1', N'Plant 2', 8, 1, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (19, N'PLANT-2-B8-F2', N'Plant 2', 8, 2, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (20, N'PLANT-2-B9-F1', N'Plant 2', 9, 1, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (21, N'PLANT-2-B9-F2', N'Plant 2', 9, 2, 1, CAST(N'2025-12-22T08:36:18.2366667' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (22, N'PLANT-3-B10-F1', N'Plant 3', 10, 1, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (23, N'PLANT-3-B10-F2', N'Plant 3', 10, 2, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (24, N'PLANT-3-B11-F1', N'Plant 3', 11, 1, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (25, N'PLANT-3-B11-F2', N'Plant 3', 11, 2, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (26, N'PLANT-3-B12-F1', N'Plant 3', 12, 1, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
INSERT [dbo].[locations] ([location_id], [location_code], [location_plant_name], [location_block], [location_floor], [location_is_active], [location_created_at]) VALUES (27, N'PLANT-3-B12-F2', N'Plant 3', 12, 2, 1, CAST(N'2025-12-22T08:36:18.2400000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[locations] OFF
GO
SET IDENTITY_INSERT [dbo].[notifications] ON 
GO
INSERT [dbo].[notifications] ([notification_id], [notification_title], [notification_message], [notification_type], [notification_target_role], [notification_is_active], [notification_created_at]) VALUES (1, N'Welcome to RoomWise', N'Welcome to the RoomWise Meeting Room Management System. Book your rooms efficiently and manage your meetings seamlessly.', N'Info', N'All', 1, CAST(N'2025-12-17T13:51:18.8300000' AS DateTime2))
GO
INSERT [dbo].[notifications] ([notification_id], [notification_title], [notification_message], [notification_type], [notification_target_role], [notification_is_active], [notification_created_at]) VALUES (2, N'System Maintenance Notice', N'The system will undergo scheduled maintenance on Sunday from 2:00 AM to 4:00 AM. Please plan accordingly.', N'Warning', N'All', 1, CAST(N'2025-12-17T13:51:18.8300000' AS DateTime2))
GO
INSERT [dbo].[notifications] ([notification_id], [notification_title], [notification_message], [notification_type], [notification_target_role], [notification_is_active], [notification_created_at]) VALUES (3, N'New Feature: Real-time Availability', N'You can now check room availability in real-time before making a booking!', N'Announcement', N'Employee', 1, CAST(N'2025-12-17T13:51:18.8300000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[notifications] OFF
GO
SET IDENTITY_INSERT [dbo].[rooms] ON 
GO
INSERT [dbo].[rooms] ([room_id], [room_code], [room_name], [room_location_id], [room_capacity], [room_facilities], [room_status], [room_is_active], [room_created_at], [room_updated_at]) VALUES (1, N'RM-001', N'Meeting Room Alpha', 1, 8, N'Projector,SmartScreen, Cool Stuff, Yes Yes, Mama mia', N'Available', 1, CAST(N'2025-12-17T13:51:18.6200000' AS DateTime2), CAST(N'2025-12-19T08:11:30.3433333' AS DateTime2))
GO
INSERT [dbo].[rooms] ([room_id], [room_code], [room_name], [room_location_id], [room_capacity], [room_facilities], [room_status], [room_is_active], [room_created_at], [room_updated_at]) VALUES (2, N'RM-002', N'Conference Room Beta', 1, 15, N'Projector,ScreenBeam,CiscoBar', N'Available', 1, CAST(N'2025-12-17T13:51:18.6200000' AS DateTime2), CAST(N'2025-12-17T13:51:18.6200000' AS DateTime2))
GO
INSERT [dbo].[rooms] ([room_id], [room_code], [room_name], [room_location_id], [room_capacity], [room_facilities], [room_status], [room_is_active], [room_created_at], [room_updated_at]) VALUES (3, N'RM-003', N'Board Room Gamma', 2, 20, N'Projector,SmartScreen,CiscoBar', N'Maintenance', 1, CAST(N'2025-12-17T13:51:18.6200000' AS DateTime2), CAST(N'2025-12-19T08:34:05.8100000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[rooms] OFF
GO
SET IDENTITY_INSERT [dbo].[users] ON 
GO
INSERT [dbo].[users] ([user_id], [user_employee_id], [user_email], [user_password], [user_full_name], [user_role], [user_dept_name], [user_is_active], [user_created_at], [user_updated_at]) VALUES (1, N'EMP-0001', N'marcophilips73@gmail.com', N'e10adc3949ba59abbe56e057f20f883e', N'System Administrator', N'Admin', N'IT Department', 1, CAST(N'2025-12-17T13:51:18.6200000' AS DateTime2), CAST(N'2025-12-22T07:52:34.7700000' AS DateTime2))
GO
INSERT [dbo].[users] ([user_id], [user_employee_id], [user_email], [user_password], [user_full_name], [user_role], [user_dept_name], [user_is_active], [user_created_at], [user_updated_at]) VALUES (2, N'EMP-0002', N'marcophilips85@gmail.com', N'c33367701511b4f6020ec61ded352059', N'John Doe', N'Employee', N'Engineering', 1, CAST(N'2025-12-17T13:51:18.6233333' AS DateTime2), CAST(N'2025-12-22T08:08:29.9900000' AS DateTime2))
GO
INSERT [dbo].[users] ([user_id], [user_employee_id], [user_email], [user_password], [user_full_name], [user_role], [user_dept_name], [user_is_active], [user_created_at], [user_updated_at]) VALUES (3, N'EMP-0003', N'jane.smith@company.com', N'e10adc3949ba59abbe56e057f20f883e', N'Jane Smith', N'Employee', N'HR', 1, CAST(N'2025-12-17T13:51:18.6233333' AS DateTime2), CAST(N'2025-12-19T08:07:20.5500000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[users] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__bookings__FF29040F2B9333A1]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[bookings] ADD UNIQUE NONCLUSTERED 
(
	[booking_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ__feedback__5A8675B09AEE85A4]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[feedbacks] ADD UNIQUE NONCLUSTERED 
(
	[feedback_booking_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__location__68EFE2C42FB25F89]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[locations] ADD UNIQUE NONCLUSTERED 
(
	[location_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__rooms__B970AF18E9739A5C]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[rooms] ADD UNIQUE NONCLUSTERED 
(
	[room_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__users__B0FBA212BA51E46D]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[users] ADD UNIQUE NONCLUSTERED 
(
	[user_email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__users__F4F2EDAEC8C909AF]    Script Date: 12/22/2025 8:37:22 AM ******/
ALTER TABLE [dbo].[users] ADD UNIQUE NONCLUSTERED 
(
	[user_employee_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[activity_logs] ADD  DEFAULT (getdate()) FOR [log_created_at]
GO
ALTER TABLE [dbo].[bookings] ADD  DEFAULT ('Pending') FOR [booking_status]
GO
ALTER TABLE [dbo].[bookings] ADD  DEFAULT (getdate()) FOR [booking_created_at]
GO
ALTER TABLE [dbo].[bookings] ADD  DEFAULT (getdate()) FOR [booking_updated_at]
GO
ALTER TABLE [dbo].[feedbacks] ADD  DEFAULT (getdate()) FOR [feedback_created_at]
GO
ALTER TABLE [dbo].[locations] ADD  DEFAULT ((1)) FOR [location_is_active]
GO
ALTER TABLE [dbo].[locations] ADD  DEFAULT (getdate()) FOR [location_created_at]
GO
ALTER TABLE [dbo].[notifications] ADD  DEFAULT ((1)) FOR [notification_is_active]
GO
ALTER TABLE [dbo].[notifications] ADD  DEFAULT (getdate()) FOR [notification_created_at]
GO
ALTER TABLE [dbo].[rooms] ADD  DEFAULT ('Available') FOR [room_status]
GO
ALTER TABLE [dbo].[rooms] ADD  DEFAULT ((1)) FOR [room_is_active]
GO
ALTER TABLE [dbo].[rooms] ADD  DEFAULT (getdate()) FOR [room_created_at]
GO
ALTER TABLE [dbo].[rooms] ADD  DEFAULT (getdate()) FOR [room_updated_at]
GO
ALTER TABLE [dbo].[users] ADD  DEFAULT ((1)) FOR [user_is_active]
GO
ALTER TABLE [dbo].[users] ADD  DEFAULT (getdate()) FOR [user_created_at]
GO
ALTER TABLE [dbo].[users] ADD  DEFAULT (getdate()) FOR [user_updated_at]
GO
ALTER TABLE [dbo].[activity_logs]  WITH CHECK ADD FOREIGN KEY([log_user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[bookings]  WITH CHECK ADD FOREIGN KEY([booking_user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[bookings]  WITH CHECK ADD FOREIGN KEY([booking_room_id])
REFERENCES [dbo].[rooms] ([room_id])
GO
ALTER TABLE [dbo].[bookings]  WITH CHECK ADD FOREIGN KEY([booking_cancelled_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[feedbacks]  WITH CHECK ADD FOREIGN KEY([feedback_booking_id])
REFERENCES [dbo].[bookings] ([booking_id])
GO
ALTER TABLE [dbo].[feedbacks]  WITH CHECK ADD FOREIGN KEY([feedback_user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[feedbacks]  WITH CHECK ADD FOREIGN KEY([feedback_admin_responded_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rooms]  WITH CHECK ADD FOREIGN KEY([room_location_id])
REFERENCES [dbo].[locations] ([location_id])
GO
ALTER TABLE [dbo].[bookings]  WITH CHECK ADD CHECK  (([booking_status]='Cancelled' OR [booking_status]='Completed' OR [booking_status]='InProgress' OR [booking_status]='Confirmed' OR [booking_status]='Pending'))
GO
ALTER TABLE [dbo].[feedbacks]  WITH CHECK ADD CHECK  (([feedback_rating]>=(1) AND [feedback_rating]<=(5)))
GO
ALTER TABLE [dbo].[notifications]  WITH CHECK ADD CHECK  (([notification_type]='Announcement' OR [notification_type]='Alert' OR [notification_type]='Warning' OR [notification_type]='Info'))
GO
ALTER TABLE [dbo].[notifications]  WITH CHECK ADD CHECK  (([notification_target_role]='All' OR [notification_target_role]='Employee' OR [notification_target_role]='Admin'))
GO
ALTER TABLE [dbo].[rooms]  WITH CHECK ADD CHECK  (([room_status]='OutOfService' OR [room_status]='Maintenance' OR [room_status]='Available'))
GO
ALTER TABLE [dbo].[users]  WITH CHECK ADD CHECK  (([user_role]='Employee' OR [user_role]='Admin'))
GO
