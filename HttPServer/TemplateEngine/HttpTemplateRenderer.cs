using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TemplateEngine
{
    /// <summary>
    /// Реализация простого шаблонизатора HTML.
    /// </summary>
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        /// <summary>
        /// Рендерит шаблон, загружая его из файла.
        /// </summary>
        /// <param name="filePath">Путь к файлу шаблона.</param>
        /// <param name="dataModel">Объект с данными.</param>
        /// <returns>Отрендеренный HTML.</returns>
        public string RenderFromFile(string filePath, object dataModel)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Template file not found", filePath);

            string template = File.ReadAllText(filePath);
            return RenderFromString(template, dataModel);
        }

        /// <summary>
        /// Рендерит шаблон из файла и сохраняет результат в новый файл.
        /// </summary>
        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result);
            return result;
        }

        /// <summary>
        /// Основной метод рендеринга из строки. Обрабатывает условия, циклы и переменные.
        /// </summary>
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (string.IsNullOrEmpty(htmlTemplate))
                return string.Empty;

            // 1. Сначала обрабатываем логику (if/foreach), так как внутри них могут быть переменные
            string processedHtml = ProcessLogic(htmlTemplate, dataModel);

            // 2. Затем заменяем оставшиеся переменные
            return ReplaceVariables(processedHtml, dataModel);
        }

        /// <summary>
        /// Рекурсивный метод обработки блоков $if и $foreach.
        /// </summary>
        private string ProcessLogic(string template, object model)
        {
            int ifIndex = template.IndexOf("$if");
            int forIndex = template.IndexOf("$foreach");

            // Базовый случай рекурсии: нет блоков
            if (ifIndex == -1 && forIndex == -1)
                return template;

            // Определяем, какой блок идет первым
            int effectiveIfIndex = ifIndex == -1 ? int.MaxValue : ifIndex;
            int effectiveForIndex = forIndex == -1 ? int.MaxValue : forIndex;

            if (effectiveIfIndex < effectiveForIndex)
            {
                return ProcessIfBlock(template, model, ifIndex);
            }
            else
            {
                return ProcessForeachBlock(template, model, forIndex);
            }
        }

        /// <summary>
        /// Обработка блока $if.
        /// </summary>
        private string ProcessIfBlock(string template, object model, int startIndex)
        {
            int endIndex = FindMatchingEndTag(template, startIndex, "$if", "$endif");
            if (endIndex == -1) return template; // Ошибка разметки, возвращаем как есть

            // Полный блок: $if(...) ... $else ... $endif
            string fullBlock = template.Substring(startIndex, endIndex - startIndex + "$endif".Length);
            
            // Парсим условие
            var match = Regex.Match(fullBlock, @"\$if\((.*?)\)");
            if (!match.Success) return template;

            string conditionPath = match.Groups[1].Value.Trim();
            
            // Тело внутри $if(...) и $endif
            string contentBody = fullBlock.Substring(match.Length, fullBlock.Length - match.Length - "$endif".Length);

            string trueBlock = contentBody;
            string falseBlock = string.Empty;

            // Проверка на наличие $else
            int elseIndex = contentBody.IndexOf("$else");
            if (elseIndex != -1)
            {
                trueBlock = contentBody.Substring(0, elseIndex); // $if(...)| ... | $else
                falseBlock = contentBody.Substring(elseIndex + "$else".Length); // $else | ... | $endif 
            }

            // Вычисляем условие
            bool conditionResult = false;
            try
            {
                object val = GetValue(model, conditionPath);
                if (val is bool b) conditionResult = b;
                else if (val != null) conditionResult = true; // Не null считаем true
            }
            catch { /* false */ }

            string resultBlock = conditionResult ? trueBlock : falseBlock;

            // Рекурсивно обрабатываем внутренности выбранного блока
            string processedResult = ProcessLogic(resultBlock, model);

            string templateBefore = template.Substring(0, startIndex);
            string templateAfter = template.Substring(endIndex + "$endif".Length);

            // Рекурсивно обрабатываем остаток строки
            return ProcessLogic(templateBefore + processedResult + templateAfter, model);
        }

        /// <summary>
        /// Обработка блока $foreach.
        /// </summary>
        private string ProcessForeachBlock(string template, object model, int startIndex)
        {
            int endIndex = FindMatchingEndTag(template, startIndex, "$foreach", "$endfor");
            if (endIndex == -1) return template;

            string fullBlock = template.Substring(startIndex, endIndex - startIndex + "$endfor".Length);
            
            // $foreach(var item in Collection)
            var match = Regex.Match(fullBlock, @"\$foreach\s*\(\s*var\s+(\w+)\s+in\s+([^\)]+)\s*\)");
            if (!match.Success) return template;

            string itemName = match.Groups[1].Value; // "item"
            string listPath = match.Groups[2].Value; // "Items"

            string loopBody = fullBlock.Substring(match.Length, fullBlock.Length - match.Length - "$endfor".Length);

            object collectionObj = GetValue(model, listPath);
            StringBuilder sb = new StringBuilder();

            if (collectionObj is IEnumerable list)
            {
                foreach (var item in list)
                {
                    // Создаем контекст: словарь, содержащий текущий элемент
                    // Если модель была словарем (вложенный цикл), копируем её значения
                    var loopContext = new Dictionary<string, object>();
                    if (model is IDictionary<string, object> parentDict)
                    {
                        foreach (var kvp in parentDict) loopContext[kvp.Key] = kvp.Value;
                    }
                    
                    loopContext[itemName] = item;

                    // 1. Раскрываем вложенную логику
                    string processedBody = ProcessLogic(loopBody, loopContext);
                    // 2. Подставляем переменные текущего уровня (например ${item.Name})
                    string materializedBody = ReplaceVariables(processedBody, loopContext);
                    
                    sb.Append(materializedBody);
                }
            }

            string templateBefore = template.Substring(0, startIndex);
            string templateAfter = template.Substring(endIndex + "$endfor".Length);

            return ProcessLogic(templateBefore + sb.ToString() + templateAfter, model);
        }

        /// <summary>
        /// Ищет индекс закрывающего тега с учетом вложенности.
        /// </summary>
        private int FindMatchingEndTag(string text, int startIndex, string openTag, string closeTag)
        {
            int balance = 0;
            int index = startIndex;

            while (index < text.Length)
            {
                if (IsSubstringAt(text, index, openTag))
                {
                    balance++;
                    index += openTag.Length;
                }
                else if (IsSubstringAt(text, index, closeTag))
                {
                    balance--;
                    if (balance == 0) return index;
                    index += closeTag.Length;
                }
                else
                {
                    index++;
                }
            }
            return -1;
        }

        private bool IsSubstringAt(string text, int index, string sub)
        {
            if (index + sub.Length > text.Length) return false;
            return text.Substring(index, sub.Length) == sub;
        }

        private string ReplaceVariables(string text, object model)
        {
            // Паттерн ${Variable}
            return Regex.Replace(text, @"\$\{(.*?)\}", match =>
            {
                string path = match.Groups[1].Value.Trim();
                object val = GetValue(model, path);
                return val?.ToString() ?? "";
            });
        }

        private object GetValue(object model, string path)
        {
            if (model == null || string.IsNullOrWhiteSpace(path)) return null;

            string[] parts = path.Split('.');
            object currentObj = model;

            // Спецобработка для словаря (контекста цикла)
            if (currentObj is IDictionary<string, object> dict)
            {
                if (dict.ContainsKey(parts[0]))
                {
                    currentObj = dict[parts[0]];
                    // Если путь был просто "${item}", возвращаем сам объект
                    if (parts.Length == 1) return currentObj;
                    
                    // Иначе сдвигаем массив ключей
                    var newParts = new string[parts.Length - 1];
                    Array.Copy(parts, 1, newParts, 0, newParts.Length);
                    parts = newParts;
                }
            }

            foreach (var propName in parts)
            {
                if (currentObj == null) return null;
                Type type = currentObj.GetType();
                PropertyInfo prop = type.GetProperty(propName);
                if (prop != null)
                    currentObj = prop.GetValue(currentObj);
                else
                    return null;
            }

            return currentObj;
        }
    }
}