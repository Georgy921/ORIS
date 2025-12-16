using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttPServer.Core.Attributes;
using HttpServer.Services;
using HttPServer.Suka;

namespace HttPServer.Controllers
{
    [Endpoint]
    public class AuthController
    {
        [HttpGet("/")]
        public void MainPage(HttpListenerContext context)
        {
            Global.Server.SendStaticResponse(context, HttpStatusCode.OK, Global.Settings.Model.StaticDirectoryPath + "auth/login.html");
        }

        [HttpPost("/auth/")]
        public void SendEmail(HttpListenerContext context)
        {
            if (!context.Request.HasEntityBody)
            {
                Global.Server.SendJsonResponse(context, HttpStatusCode.BadRequest);
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var body = reader.ReadToEnd();

            if (string.IsNullOrEmpty(body))
            {
                Global.Server.SendJsonResponse(context, HttpStatusCode.BadRequest);
                return;
            }

            var emailData = JsonSerializer.Deserialize<SendEmailDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var message = $"Ваш email: {emailData.Email}, пароль: {emailData.Password}";

            EmailService.SendEmail(emailData.Email, "Данные от httpсервера", message);

            Global.Server.SendJsonResponse(context, HttpStatusCode.OK);
        }
    }
}
