namespace ScriptRunner.Library.Services
{
    public class TypeConverter
    {
        private readonly string _type;

        public TypeConverter(string type)
        {
            _type = type.Trim().ToLower();
        }

        internal string ConvertToHtml()
        {
            return _type switch
            {
                "datetime-local" or "datetime" => "datetime-local",
                "text" or "string" or "varchar" => "text",
                "number" or "int" => "number",
                "file" => "file",
                _ => "text"
            };
        }
    }

}
