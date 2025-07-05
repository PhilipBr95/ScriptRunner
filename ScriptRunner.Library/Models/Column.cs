namespace ScriptRunner.Library.Models
{
    public class Column
    {
        public string ColumnName { get; set; }
        public string? Href { get; set; }

        public string? OnClick { get; set; }

        public string? Title { get; set; }

        public string? sType { get; set; }

        /// <summary>
        /// (Optional) The overriding custom CSS class to apply to the Href
        /// </summary>
        public string? HrefCss { get; set; }
    }
}
