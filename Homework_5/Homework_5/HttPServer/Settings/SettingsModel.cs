using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttPServer.Settings
{
    public class SettingsModel
    {
        public string StaticDirectoryPath { get; init; }
        public string Domain { get; init; }
        public string Port { get; init; }
        public string EmailFrom { get; init; }
        public string EmailNameFrom { get; init; }
        public string SmtpPort { get; init; }
        public string SmtpHost { get; init; }
        public string SmtpPassword { get; init; }
    }
}