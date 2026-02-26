namespace MiniHttpServer.Settings
{
    public class JsonEntity
    {
        public string Domain { get; set; }
        public int Port { get; set; }
        
        // Новое поле для подключения к БД
        public string ConnectionString { get; set; } 

        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string EmailFrom { get; set; }
        public bool SmtpEnableSsl { get; set; }
        
        // Пустой конструктор нужен для десериализации JSON
        public JsonEntity() { }

        public JsonEntity(string domain, int port, string connectionString, string smtpHost, string smtpUsername, int smtpPort, string emailFrom, bool smtpEnableSsl, string smtpPassword)
        {
            Domain = domain;
            Port = port;
            ConnectionString = connectionString;
            SmtpHost = smtpHost;
            SmtpPassword = smtpPassword;
            SmtpPort = smtpPort;
            EmailFrom = emailFrom;
            SmtpUsername = smtpUsername;
            SmtpEnableSsl = smtpEnableSsl;
        }
    }
}