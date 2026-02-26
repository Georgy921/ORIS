using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Core.Services;
using MiniHttpServer.Model;
using Npgsql;
using TemplateEngine;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class AuthEndpoint
    {
        private readonly ORMContext _databaseService;
        private readonly IHtmlTemplateRenderer _templateRenderer;
        private readonly AuthService _authService;
        public AuthEndpoint()
        {
            var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=9996";
            _databaseService = new ORMContext(connectionString);
            _templateRenderer = new HtmlTemplateRenderer();
            _authService = new AuthService(_databaseService);
            
            _databaseService.Create<User>("Users");
            _databaseService.Create<Session>("Sessions");
        }

        [HttpPost("auth")]
        public void HandleAuth(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                var username = data.GetValueOrDefault("username")?.Trim() ?? "";
                var email = data.GetValueOrDefault("email")?.Trim() ?? "";
                var password = data.GetValueOrDefault("password") ?? "";
                var rememberMe = data.GetValueOrDefault("rememberMe") == "true";

                // валидация
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SendJsonResponse(response, new { success = false, message = "Email и пароль обязательны." });
                    return;
                }
                var token = GenerateToken();

                //Настройка Cookie в зависимости от "Запомнить меня"
                var cookieExpiration = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.MinValue;

                var sessionCookie = new Cookie("session_token", token)
                {
                    HttpOnly = true,
                    Secure = false,
                    Path = "/",
                };

                // Если Expires = DateTime.MinValue, cookie будет сессионной (удалится при закрытии браузера)
                if (rememberMe)
                {
                    sessionCookie.Expires = cookieExpiration;
                }

                var existingUser = _databaseService.FirstOrDefault<User>(u => u.email == email);
                User? resultUser = null;

                if (existingUser == null)
                {
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        SendJsonResponse(response, new { success = false, message = "Имя обязательно при регистрации." });
                        return;
                    }

                    var newUser = new User
                    {
                        username = username,
                        email = email,
                        password = password,
                        role = "user"
                    };

                    var createdUser = _databaseService.Create(newUser, "Users");
                    if (createdUser == null)
                    {
                        SendJsonResponse(response, new { success = false, message = "Ошибка создания пользователя." });
                        return;
                    }

                    _databaseService.Create(new Session
                    {
                        user_id = createdUser.id,
                        session_token = token,
                        expires_at = cookieExpiration == DateTime.MinValue ? DateTime.UtcNow.AddDays(7) : cookieExpiration
                    }, "Sessions");

                    resultUser = createdUser;
                }
                else
                {

                    // Если поле username пришло, проверяем его (опционально, для строгой регистрации)
                    if (!string.IsNullOrEmpty(username) && existingUser.username != username)
                    {
                        SendJsonResponse(response, new { success = false, message = "Пользователь с таким email уже существует, но имя не совпадает." });
                        return;
                    }

                    if (existingUser.password != password)
                    {
                        SendJsonResponse(response, new { success = false, message = "Неверный пароль." });
                        return;
                    }

                    // Удаляем старые сессии пользователя
                    var oldSessions = _databaseService.Where<Session>(s => s.user_id == existingUser.id);
                    foreach (var oldSession in oldSessions)
                    {
                        _databaseService.Delete(oldSession.id, "Sessions");
                    }

                    // Создаем новую сессию в БД
                    _databaseService.Create(new Session
                    {
                        user_id = existingUser.id,
                        session_token = token,
                        expires_at = cookieExpiration == DateTime.MinValue ? DateTime.UtcNow.AddDays(7) : cookieExpiration
                    }, "Sessions");

                    resultUser = existingUser;
                }

                response.SetCookie(sessionCookie);

                SendJsonResponse(response, new
                {
                    success = true,
                    message = existingUser == null ? "Регистрация успешна" : "Вход выполнен",
                    user = new { resultUser.username, resultUser.role, resultUser.email },
                    token = token, // Отправляем токен на клиент, если нужно для JS
                    rememberMe = rememberMe
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleAuth: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                SendJsonResponse(response, new { success = false, message = "Ошибка сервера." }, 500);
            }
        }



        private string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [HttpGet("check-auth")]
        public void CheckAuth(HttpListenerContext context)
        {
            var token = context.Request.Cookies["session_token"]?.Value;
            var (valid, user) = _authService.ValidateSession(token);

            if (!valid)
            {                
                context.Response.SetCookie(new Cookie("session_token", "")
                {
                    HttpOnly = true,
                    Secure = false,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(-1)
                });
                SendJsonResponse(context.Response, new { authenticated = false });
            }
            else
            {
                SendJsonResponse(context.Response, new { authenticated = true, user = new { user.username, user.role } });
            }
        }

        /*[HttpPost("add-tour")]
        public void AddTour(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {                
                var token = request.Cookies["session_token"]?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    SendJsonResponse(response, new { success = false, message = "Требуется авторизация" }, 401);
                    return;
                }

                var (valid, user) = _authService.ValidateSession(token);
                if (!valid || user?.role != "admin")
                {
                    SendJsonResponse(response, new { success = false, message = "Доступ запрещён" }, 403);
                    return;
                }

                // Чтение JSON
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var jsonDoc = JsonDocument.Parse(body);

                var tour = new Tour
                {
                    image_url = jsonDoc.RootElement.GetProperty("image_url").GetString(),
                    name = jsonDoc.RootElement.GetProperty("tour_name").GetString(),
                    departure_city = jsonDoc.RootElement.GetProperty("departure_city").GetString(),
                    arrival_city = jsonDoc.RootElement.GetProperty("arrival_city").GetString(),
                    departure_date = jsonDoc.RootElement.GetProperty("departure_date").GetString(),
                    nights_count = jsonDoc.RootElement.GetProperty("nights_count").GetInt32(),
                    adults_count = jsonDoc.RootElement.GetProperty("people_count").GetInt32(),
                    tour_price = jsonDoc.RootElement.GetProperty("tour_price").GetDecimal(),
                    hotel_name = jsonDoc.RootElement.GetProperty("hotel_name").GetString(),
                    rating = jsonDoc.RootElement.GetProperty("rating").GetInt32(),
                    meal_plan = jsonDoc.RootElement.GetProperty("meal_plan").GetString(),
                    nearby_attractions = jsonDoc.RootElement.GetProperty("nearby_attractions").GetString(),
                    hotel_facilities = jsonDoc.RootElement.GetProperty("hotel_facilities").GetString(),
                    adult_pools_count = jsonDoc.RootElement.GetProperty("adult_pools_count").GetInt32(),
                    children_pools_count = jsonDoc.RootElement.GetProperty("children_pools_count").GetInt32(),
                    beach_info = jsonDoc.RootElement.GetProperty("beach_info").GetString(),
                    contact_info = jsonDoc.RootElement.GetProperty("contact_info").GetString()
                };

                // Сохраняем в БД
                var createdTour = _databaseService.Create(tour, "Tours");
                if (createdTour != null)
                {
                    SendJsonResponse(response, new { success = true, message = "Тур добавлен", id = createdTour.id });
                }
                else
                {
                    SendJsonResponse(response, new { success = false, message = "Ошибка сохранения" }, 500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка AddTour: {ex.Message}");
                SendJsonResponse(response, new { success = false, message = "Ошибка сервера" }, 500);
            }
        }*/

        

      

        private void SendJsonResponse(HttpListenerResponse response, object data, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close(); 
        }
    }
}
