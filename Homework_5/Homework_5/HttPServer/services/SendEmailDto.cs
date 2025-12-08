using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Services
{
    public class SendEmailDto
    {
        public string Email { get; init; }
        public string Password { get; init; }
    }
}
