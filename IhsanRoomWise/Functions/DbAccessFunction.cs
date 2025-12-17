// Functions\DbAccessFunction.cs

using System;

namespace IhsanRoomWise.Functions
{
    public class DbAccessFunction
    {
        private readonly string connectionString;

        //constructor
        public DbAccessFunction()
        {
            // Your SQL Server connection string
            connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=rwdb;Integrated Security=True;TrustServerCertificate=True";
        }

        // Method to get connection string - this is what your controller is looking for
        public string GetConnectionString()
        {
            return connectionString;
        }

    }

}