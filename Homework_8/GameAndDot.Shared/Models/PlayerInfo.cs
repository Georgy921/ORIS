using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GameAndDot.Shared.Models
{
    public record PlayerInfo(
        string Username,
        string ColorHex);
}
