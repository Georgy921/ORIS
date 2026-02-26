using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TemplateEngine
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromFile(string filePath, object dataModel)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Template file not found", filePath);

            string template = File.ReadAllText(filePath, Encoding.UTF8);
            Console.WriteLine($" Шаблон загружен, длина: {template.Length} символов");
            Console.WriteLine($"   Содержит $foreach: {template.Contains("$foreach")}");
            Console.WriteLine($"   Содержит $if: {template.Contains("$if")}");

            return RenderFromString(template, dataModel);
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result, Encoding.UTF8);
            return result;
        }

        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (string.IsNullOrEmpty(htmlTemplate))
                return string.Empty;

            Console.WriteLine("🔧 Начинаю обработку шаблона...");

            // 1. Сначала обрабатываем циклы и условия
            string processedHtml = ProcessLogic(htmlTemplate, dataModel);

            // 2. Затем заменяем переменные
            string result = ReplaceVariables(processedHtml, dataModel);

            // 3. Чистим экранированные символы
            result = result.Replace("\\r\\n", "").Replace("\\n", "");

            Console.WriteLine($" Обработка завершена. Результат: {result.Length} символов");
            Console.WriteLine($"   Осталось $foreach: {result.Contains("$foreach")}");
            Console.WriteLine($"   Осталось $if: {result.Contains("$if")}");
            Console.WriteLine($"   Осталось ${{: {result.Contains("${")}");

            return result;
        }

        private string ProcessLogic(string template, object model)
        {
            int ifIndex = template.IndexOf("$if", StringComparison.OrdinalIgnoreCase);
            int forIndex = template.IndexOf("$foreach", StringComparison.OrdinalIgnoreCase);

            if (ifIndex == -1 && forIndex == -1)
                return template;

            int effectiveIfIndex = ifIndex == -1 ? int.MaxValue : ifIndex;
            int effectiveForIndex = forIndex == -1 ? int.MaxValue : forIndex;

            if (effectiveIfIndex < effectiveForIndex)
            {
                Console.WriteLine($"Обрабатываю $if на позиции {ifIndex}");
                return ProcessIfBlock(template, model, ifIndex);
            }
            else
            {
                Console.WriteLine($"Обрабатываю $foreach на позиции {forIndex}");
                return ProcessForeachBlock(template, model, forIndex);
            }
        }

        private string ProcessIfBlock(string template, object model, int startIndex)
        {
            int endIndex = FindMatchingEndTag(template, startIndex, "$if", "$endif");
            if (endIndex == -1)
            {
                Console.WriteLine($"❌ Не найден $endif для $if на позиции {startIndex}");
                return template;
            }

            string fullBlock = template.Substring(startIndex, endIndex - startIndex + "$endif".Length);

            var match = Regex.Match(fullBlock, @"\$if\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"❌ Не удалось распарсить условие $if");
                return template;
            }

            string conditionPath = match.Groups[1].Value.Trim();
            string contentBody = fullBlock.Substring(match.Length, fullBlock.Length - match.Length - "$endif".Length);

            string trueBlock = contentBody;
            string falseBlock = string.Empty;

            int elseIndex = contentBody.IndexOf("$else", StringComparison.OrdinalIgnoreCase);
            if (elseIndex != -1)
            {
                trueBlock = contentBody.Substring(0, elseIndex);
                falseBlock = contentBody.Substring(elseIndex + "$else".Length);
            }

            bool conditionResult = EvaluateCondition(model, conditionPath);
            Console.WriteLine($"   Условие '{conditionPath}' = {conditionResult}");

            string resultBlock = conditionResult ? trueBlock : falseBlock;
            string processedResult = ProcessLogic(resultBlock, model);

            string templateBefore = template.Substring(0, startIndex);
            string templateAfter = template.Substring(endIndex + "$endif".Length);

            return ProcessLogic(templateBefore + processedResult + templateAfter, model);
        }

        private string ProcessForeachBlock(string template, object model, int startIndex)
        {
            int endIndex = FindMatchingEndTag(template, startIndex, "$foreach", "$endfor");
            if (endIndex == -1)
            {
                Console.WriteLine($"Не найден $endfor для $foreach на позиции {startIndex}");
                return template;
            }

            string fullBlock = template.Substring(startIndex, endIndex - startIndex + "$endfor".Length);

            var match = Regex.Match(fullBlock,
                @"\$foreach\s*\(\s*(?:var\s+)?(\w+)\s+in\s+([^\)]+)\s*\)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                Console.WriteLine($"Не удалось распарсить цикл $foreach");
                Console.WriteLine($"   Блок: {fullBlock.Substring(0, Math.Min(100, fullBlock.Length))}...");
                return template;
            }

            string itemName = match.Groups[1].Value;
            string listPath = match.Groups[2].Value.Trim();

            Console.WriteLine($"   Переменная: {itemName}, Коллекция: {listPath}");

            string loopBody = fullBlock.Substring(match.Length,
                fullBlock.Length - match.Length - "$endfor".Length);

            object collectionObj = GetValue(model, listPath);
            StringBuilder sb = new StringBuilder();

            if (collectionObj is IEnumerable list && collectionObj is not string)
            {
                int count = 0;
                foreach (var item in list)
                {
                    count++;
                    var loopContext = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    if (model is IDictionary<string, object> parentDict)
                    {
                        foreach (var kvp in parentDict)
                            loopContext[kvp.Key] = kvp.Value;
                    }

                    loopContext[itemName] = item;

                    string processedBody = ProcessLogic(loopBody, loopContext);
                    string materializedBody = ReplaceVariables(processedBody, loopContext);

                    sb.Append(materializedBody);
                }
                Console.WriteLine($"Обработано элементов: {count}");
            }
            else
            {
                Console.WriteLine($"Коллекция пуста или не найдена: {listPath}");
            }

            string templateBefore = template.Substring(0, startIndex);
            string templateAfter = template.Substring(endIndex + "$endfor".Length);

            return ProcessLogic(templateBefore + sb.ToString() + templateAfter, model);
        }

        private bool EvaluateCondition(object model, string conditionPath)
        {
            object val = GetValue(model, conditionPath);

            if (val == null) return false;
            if (val is bool b) return b;
            if (val is string s) return !string.IsNullOrWhiteSpace(s);
            if (val is int i) return i != 0;
            if (val is decimal d) return d != 0;
            if (val is IEnumerable enumerable && enumerable is not string)
                return enumerable.Cast<object>().Any();

            return true;
        }

        private int FindMatchingEndTag(string text, int startIndex, string openTag, string closeTag)
        {
            int balance = 0;
            int index = startIndex;
            int openLen = openTag.Length;
            int closeLen = closeTag.Length;

            while (index < text.Length)
            {
                if (index + openLen <= text.Length)
                {
                    string substring = text.Substring(index, openLen);
                    if (substring.Equals(openTag, StringComparison.OrdinalIgnoreCase))
                    {
                        if (index + openLen < text.Length &&
                            (text[index + openLen] == '(' || char.IsWhiteSpace(text[index + openLen])))
                        {
                            balance++;
                            index += openLen;
                            continue;
                        }
                    }
                }

                if (index + closeLen <= text.Length)
                {
                    string substring = text.Substring(index, closeLen);
                    if (substring.Equals(closeTag, StringComparison.OrdinalIgnoreCase))
                    {
                        balance--;
                        if (balance == 0) return index;
                        index += closeLen;
                        continue;
                    }
                }

                index++;
            }
            return -1;
        }

        private string ReplaceVariables(string text, object model)
        {
            return Regex.Replace(text, @"\$\{([^}]+)\}", match =>
            {
                string path = match.Groups[1].Value.Trim();
                object val = GetValue(model, path);
                return val?.ToString() ?? "";
            }, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        private object GetValue(object model, string path)
        {
            if (model == null || string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();
            string[] parts = path.Split('.');
            object currentObj = model;

            if (currentObj is IDictionary<string, object> dict)
            {
                string keyToFind = parts[0];
                object foundValue = null;
                bool found = false;

                if (dict.TryGetValue(keyToFind, out foundValue))
                {
                    found = true;
                }
                else
                {
                    foreach (var kvp in dict)
                    {
                        if (kvp.Key.Equals(keyToFind, StringComparison.OrdinalIgnoreCase))
                        {
                            foundValue = kvp.Value;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    currentObj = foundValue;
                    if (parts.Length == 1) return currentObj;

                    var newParts = new string[parts.Length - 1];
                    Array.Copy(parts, 1, newParts, 0, newParts.Length);
                    parts = newParts;
                }
                else
                {
                    return null;
                }
            }

            foreach (var propName in parts)
            {
                if (currentObj == null) return null;

                Type type = currentObj.GetType();

                PropertyInfo prop = type.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop != null)
                {
                    currentObj = prop.GetValue(currentObj);
                }
                else
                {
                    return null;
                }
            }

            return currentObj;
        }
    }
}