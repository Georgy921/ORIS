using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Npgsql;
using StatusProcessorServer;

namespace InvoiceProcessorLogic
{
    public class InvoiceProcessorService
    {
        private readonly string _connectionString;

        public InvoiceProcessorService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Invoice> GetPendingInvoices()
        {
            var invoices = new List<Invoice>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT Id, Bank_Name, Amount, Status, Updated_At, Retry_Count, Last_Attempt_At FROM Invoices WHERE Status = 'pending'", conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var invoice = new Invoice
                    {
                        Id = reader.GetInt32(0),
                        BankName = reader.GetString(1),
                        Amount = reader.GetDecimal(2),
                        Status = reader.GetString(3),
                        UpdatedAt = reader.GetDateTime(4),
                        RetryCount = reader.GetInt32(5),
                        LastAttemptAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                    };
                    invoices.Add(invoice);
                }
            }

            return invoices;
        }

        public void UpdateInvoice(int id, string status, int retryCount)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(@"
                UPDATE Invoices 
                SET Status = @status, 
                    Updated_At = NOW(), 
                    Retry_Count = @retryCount, 
                    Last_Attempt_At = NOW() 
                WHERE Id = @id", conn);

                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("retryCount", retryCount);

                cmd.ExecuteNonQuery();
            }
        }
    }
}