using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Model;
using MiniHttpServer.Settings;   
using TemplateEngine;       

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    public class ToursEndpoint
    {
        private readonly ORMContext _orm;
        private HtmlTemplateRenderer renderer = new HtmlTemplateRenderer();

        public ToursEndpoint()
        {
            //Берем строку подключения из settings.json
            var settings = Singleton.GetInstance().Settings;
            
            // Убедись, что в settings.json строка подключения для Postgres!
            _orm = new ORMContext(settings.ConnectionString);
        }

        // GET /tours/list
        [HttpGet]
        public void GetToursList(HttpListenerContext context)
        {
            try
            {
                var tours = _orm.ReadByAll<Tour>("tours").ToList();

                // Передаём как Dictionary с ключом "HotTours"
                var model = new Dictionary<string, object>
                {
                    ["HotTours"] = tours
                };



                string templatePath = "Public/index.html"; 

                string html = renderer.RenderFromFile(templatePath, model);
                if (html.Contains("$if") || html.Contains("$endif"))
                {
                    Console.WriteLine("❌ ШАБЛОНИЗАТОР НЕ СРАБОТАЛ!");
                }
                else
                {
                    Console.WriteLine("✅ Теги обработаны успешно");
                }
                SendHtml(context, html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetToursList: {ex.Message}");
                SendError(context, 500, ex.Message);
            }
        }

        [HttpPost("filters")]
        public void ApplyFilters(HttpListenerContext context)
        {
            var response = context.Response;

            try
            {
                // 1. Чтение тела запроса
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(body))
                {
                    SendJsonResponse(response, new { success = false, message = "Пустой запрос" }, 400);
                    return;
                }

                // 2. Парсинг JSON
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // 3. Извлечение параметров
                var filters = ExtractFilters(root);

                // 4. Получение и фильтрация данных
                var tours = _orm.ReadByAll<Tour>("tours").AsEnumerable();
                var filtered = FilterTours(tours, filters);

                // 5. Формирование ответа
                var result = filtered
                .ToList();

                Console.WriteLine($"✅ Найдено {result.Count} туров");

                SendJsonResponse(response, new
                {
                    success = true,
                    count = result.Count,
                    tours = result
                });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON Error: {ex.Message}");
                SendJsonResponse(response, new { success = false, message = "Неверный формат JSON" }, 400);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ApplyFilters Error: {ex.Message}\n{ex.StackTrace}");
                SendJsonResponse(response, new { success = false, message = "Ошибка сервера" }, 500);
            }
        }

        [HttpGet("details")]
        public void TourDetails(HttpListenerContext context)
        {
            try
            {
                // 🔥 Логирование для отладки
                Console.WriteLine($"🔍 TourDetails вызван! URL: {context.Request.Url}");
                Console.WriteLine($"🔍 QueryString: {context.Request.QueryString}");

                // 🔥 Получаем id из query string с проверкой на null
                var query = context.Request.QueryString;
                string idParam = query["id"];

                Console.WriteLine($"🔍 ID параметр: '{idParam}'");

                // 🔥 Проверяем что параметр существует и это положительное число
                if (string.IsNullOrWhiteSpace(idParam) ||
                    !int.TryParse(idParam, out int id) ||
                    id <= 0)  // 🔥 Исправлено: id <= 0 (ID не может быть 0 или отрицательным)
                {
                    SendError(context, 400, $"Неверный ID тура. Получено: '{idParam}'");
                    return;
                }

                Console.WriteLine($"✅ Загрузка тура с ID={id}...");

                // 🔥 Убедитесь, что имя таблицы совпадает с вашей БД (обычно "tours" с маленькой)
                var tour = _orm.ReadById<Tour>(id, "tours");

                if (tour == null)
                {
                    Console.WriteLine($"❌ Тур с ID={id} не найден в БД");
                    SendError(context, 404, $"Тур с ID {id} не найден");
                    return;
                }

                Console.WriteLine($"✅ Тур найден: {tour.hotel_name}");

                var templatePath = "Public/tour-details.html";

                // 🔥 Проверка существования шаблона
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"❌ Шаблон не найден: {templatePath}");
                    SendError(context, 500, $"Шаблон не найден: {templatePath}");
                    return;
                }

                string html = renderer.RenderFromFile(templatePath, new { Tour = tour });

                // 🔥 Логирование результата рендеринга
                if (html.Contains("$if") || html.Contains("$foreach"))
                {
                    Console.WriteLine("❌ Шаблонизатор не обработал все теги!");
                }
                else
                {
                    Console.WriteLine("✅ Шаблон обработан успешно");
                }

                SendHtml(context, html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TourDetails Error: {ex.Message}\n{ex.StackTrace}");
                SendError(context, 500, $"Ошибка сервера: {ex.Message}");
            }
        }



        private FilterParams ExtractFilters(JsonElement root)
        {
            var filters = new FilterParams();

            // Поиск
            if (root.TryGetProperty("search", out var search))
            {
                if (search.TryGetProperty("from", out var from) && from.GetString() is string f && !string.IsNullOrWhiteSpace(f))
                    filters.DepartureCity = f;
                if (search.TryGetProperty("to", out var to) && to.GetString() is string t && !string.IsNullOrWhiteSpace(t))
                    filters.ArrivalCity = t;
                if (search.TryGetProperty("date", out var date) && date.GetString() is string d && !string.IsNullOrWhiteSpace(d))
                    filters.DepartureDate = d;
                if (search.TryGetProperty("duration", out var dur) && dur.GetString() is string durStr)
                    filters.NightsCount = ParseNumber(durStr);
                if (search.TryGetProperty("people", out var people) && people.GetString() is string pStr)
                    filters.AdultsCount = ParseNumber(pStr);
            }

            // Цена
            if (root.TryGetProperty("price", out var price))
            {
                if (price.TryGetProperty("min", out var min) && min.TryGetInt32(out var minVal))
                    filters.MinPrice = minVal;
                if (price.TryGetProperty("max", out var max) && max.TryGetInt32(out var maxVal))
                    filters.MaxPrice = maxVal;
            }

            // Поиск по отелю
            if (root.TryGetProperty("hotel_search", out var hotel) && hotel.GetString() is string h && !string.IsNullOrWhiteSpace(h))
                filters.HotelSearch = h.ToLower();

            // Чекбоксы
            if (root.TryGetProperty("filters", out var filtersObj))
            {
                foreach (var prop in filtersObj.EnumerateObject())
                {
                    var values = new List<string>();
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                            if (item.GetString() is string val) values.Add(val);
                    }
                    if (values.Count > 0)
                        filters.CheckboxFilters[prop.Name] = values;
                }
            }

            return filters;
        }

        private IEnumerable<Tour> FilterTours(IEnumerable<Tour> tours, FilterParams f)
        {
            return tours.Where(t =>
                // Город отправления
                (string.IsNullOrWhiteSpace(f.DepartureCity) ||
                 t.departure_city?.Contains(f.DepartureCity, StringComparison.OrdinalIgnoreCase) == true) &&
                // Город прибытия
                (string.IsNullOrWhiteSpace(f.ArrivalCity) ||
                 t.arrival_city?.Contains(f.ArrivalCity, StringComparison.OrdinalIgnoreCase) == true) &&
                // Дата
                (string.IsNullOrWhiteSpace(f.DepartureDate) ||
                 t.departure_date?.Contains(f.DepartureDate, StringComparison.OrdinalIgnoreCase) == true) &&
                // Ночи
                (!f.NightsCount.HasValue || t.nights_count == f.NightsCount.Value) &&
                // Взрослые
                (!f.AdultsCount.HasValue || t.adults_count >= f.AdultsCount.Value) &&
                // Цена
                t.tour_price >= f.MinPrice && t.tour_price <= f.MaxPrice &&
                // Отель
                (string.IsNullOrWhiteSpace(f.HotelSearch) ||
                 t.hotel_name?.ToLower().Contains(f.HotelSearch) == true) &&
                // Чекбоксы
                MatchesCheckboxFilters(t, f.CheckboxFilters)
            );
        }

        private bool MatchesCheckboxFilters(Tour t, Dictionary<string, List<string>> filters)
        {
            // === Популярные фильтры ===
            if (filters.TryGetValue("Популярные фильтры", out var popular) && popular.Count > 0)
            {
                // Логика OR: тур подходит, если совпадает ХОТЯ БЫ с одним выбранным фильтром
                bool matches = popular.Any(filter => filter switch
                {
                    "all-inclusive" => t.meal_plan?.Contains("все включено", StringComparison.OrdinalIgnoreCase) == true,
                    "wi-fi" => !string.IsNullOrWhiteSpace(t.wifi) && t.wifi.ToLower() != "нет",
                    "first-line" => t.popular_filters?.Contains("1-я линия", StringComparison.OrdinalIgnoreCase) == true,
                    "beach" => t.popular_filters?.Contains("пляж", StringComparison.OrdinalIgnoreCase) == true,
                    "regular-flight" => true, // Заглушка, если нет поля в БД
                    _ => false
                });

                if (!matches) return false; // Если не совпал ни с одним — исключаем тур
            }

            // === Питание ===
            if (filters.TryGetValue("Питание", out var meals) && meals.Count > 0)
            {
                var meal = t.meal_plan?.ToLower() ?? "";

                bool matches = meals.Any(m => m switch
                {
                    "ultra_all" => meal.Contains("ультра все включено"),
                    "all" => meal.Contains("все включено") && !meal.Contains("ультра"),
                    "breakfest" => meal.Contains("завтрак"),
                    "without_food" => string.IsNullOrWhiteSpace(meal) || meal.Contains("без питания"),
                    "pansion" => meal.Contains("полупансион"),
                    "full_pansion" => meal.Contains("полный пансион"),
                    _ => false
                });

                if (!matches) return false;
            }

            // === Категория отеля (звёзды) ===
            if (filters.TryGetValue("Категория отеля", out var stars) && stars.Count > 0)
            {
                var allowedStars = stars
                    .Select(s => int.TryParse(s, out var star) ? (int?)star : null)
                    .Where(s => s.HasValue)
                    .Select(s => s.Value)
                    .ToList();

                if (allowedStars.Count > 0 && !allowedStars.Contains(t.rating))
                    return false;
            }

            // === Регионы и курорты ===
            if (filters.TryGetValue("Регионы и курорты", out var regions) && regions.Count > 0)
            {
                var city = t.arrival_city?.ToLower() ?? "";
                var region = t.region?.ToLower() ?? "";

                bool matches = regions.Any(r =>
                    city.Contains(r.ToLower()) ||
                    region.Contains(r.ToLower()));

                if (!matches) return false;
            }

            // Если фильтр не применён (категория пуста) — тур проходит
            return true;
        }

        private int? ParseNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : null;
        }

    // === Вспомогательный класс для параметров фильтрации ===
   

        private void SendJsonResponse(HttpListenerResponse response, object data, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        // Вспомогательный метод для отправки HTML
        private void SendHtml(HttpListenerContext context, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=UTF-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = 200;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        // Вспомогательный метод для отправки ошибки
        private void SendError(HttpListenerContext context, int code, string message)
        {
            context.Response.StatusCode = code;
            string errorHtml = $"<h1>Error {code}</h1><p>{message}</p>";
            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentType = "text/html; charset=UTF-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
    }
    internal class FilterParams
    {
        public string? DepartureCity { get; set; }
        public string? ArrivalCity { get; set; }
        public string? DepartureDate { get; set; }
        public int? NightsCount { get; set; }
        public int? AdultsCount { get; set; }
        public int MinPrice { get; set; } = 0;
        public int MaxPrice { get; set; } = 2000000;
        public string? HotelSearch { get; set; }
        public Dictionary<string, List<string>> CheckboxFilters { get; } = new();
    }
}