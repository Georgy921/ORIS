using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttPServer.Settings;
using MiniHttpServer.Utils;

namespace HttPServer.Utils
{
    public static class Buffer
    {
        public static byte[] GetBytesFromFile(string path)
        {
            if (Path.HasExtension(path))
                return File.ReadAllBytes(TryGetFile(path));

            return File.ReadAllBytes(TryGetFile(path));
        }

        public static byte[] GetBytesFromJson(string jsonString)
        {
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public static string TryGetFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Путь не может быть пустым.", nameof(path));

                var root = Singletone.Instance.Model.StaticDirectoryPath;

                // Нормализуем входной путь: заменяем '/' на системный разделитель
                var normalizedInputPath = path.Replace('/', Path.DirectorySeparatorChar);
                var targetParts = normalizedInputPath.Split(
                    new[] { Path.DirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (targetParts.Length == 0)
                    throw new FileNotFoundException(path);

                // Ищем все файлы с нужным именем во всех поддиректориях
                var fileName = targetParts[^1]; // последний элемент — имя файла
                var files = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories);

                // Находим первый файл, чей путь заканчивается на targetParts
                foreach (var fullPath in files)
                {
                    var normalizedFile = fullPath.Replace('/', Path.DirectorySeparatorChar);
                    var fileParts = normalizedFile.Split(
                        new[] { Path.DirectorySeparatorChar },
                        StringSplitOptions.RemoveEmptyEntries
                    );

                    // Проверяем, что в файловом пути достаточно сегментов
                    if (fileParts.Length < targetParts.Length)
                        continue;

                    // Сравниваем последние N сегментов
                    bool match = true;
                    for (int i = 0; i < targetParts.Length; i++)
                    {
                        var filePart = fileParts[fileParts.Length - targetParts.Length + i];
                        if (!string.Equals(filePart, targetParts[i], StringComparison.OrdinalIgnoreCase))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        return fullPath;
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.Instance.LogMess($"Директория не найдена {ex}");
            }
            catch (FileNotFoundException)
            {
                Logger.Instance.LogMess("Файл не найден");
            }
            catch (Exception)
            {
                Logger.Instance.LogMess("Ошибка при извлечении текста");
            }

            return Singletone.Instance.Model.StaticDirectoryPath + "404.html";
        }
    }
}
