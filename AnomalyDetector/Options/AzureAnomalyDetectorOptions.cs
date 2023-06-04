namespace AnomalyDetector.Options
{
    public class AzureAnomalyDetectorOptions
    {
        public const string Section = "AnomalyDetector";

        public string EndpointUri { get; set; }
        public string Credentials { get; set; }
    }
}
