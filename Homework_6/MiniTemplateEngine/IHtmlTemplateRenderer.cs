namespace TemplateEngine
{
    /// <summary>
    /// Интерфейс для рендеринга HTML шаблонов.
    /// </summary>
    public interface IHtmlTemplateRenderer
    {
        /// <summary>
        /// Рендерит шаблон из строки.
        /// </summary>
        string RenderFromString(string htmlTemplate, object dataModel);

        /// <summary>
        /// Рендерит шаблон из файла.
        /// </summary>
        string RenderFromFile(string filePath, object dataModel);

        /// <summary>
        /// Рендерит шаблон из файла и сохраняет результат в файл.
        /// </summary>
        string RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
    }
}