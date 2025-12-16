using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusProcessorServer
{
    public class Invoice
    {
        public int Id { get; set; }
        public string BankName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public int RetryCount { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
    
    
    
    }
}
