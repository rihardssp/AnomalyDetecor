namespace AnomalyDetector.Models
{
    public class Record
    {
        public DateTime Date { get; set; }
        public string[] Names { get; set; }
        public float[] Values { get; set; }
    }
}
