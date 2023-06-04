namespace AnomalyDetector.Models
{
    public class AnomalyRespose
    {
        public DateTime Date { get; set; }
        public string VariableName { get; set; }
        public bool IsAnomaly { get; set; }
    }
}
