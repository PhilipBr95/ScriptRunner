namespace ScriptRunner.Library.Models
{
    public class Activity<T>
    {
        public string System { get; set; }
        public string Description { get; set; }
        public T? Data { get; set; }
        public string ActionedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool Success { get; set; }
    }
}