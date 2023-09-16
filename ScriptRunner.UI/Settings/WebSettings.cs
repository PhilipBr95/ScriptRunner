namespace ScriptRunner.UI.Settings
{
    public class WebSettings
    {
        public string AdminAD { get; set; }
        public int MaxRequestLineSize { get; set; } = 30000000; //~30MB
    }
}
