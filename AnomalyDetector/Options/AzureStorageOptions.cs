namespace AnomalyDetector.Options
{
    public class AzureStorageOptions
    {
        public const string Section = "AzureStorage";

        public string AccountName { get; set; }
        public string ContainerName { get; set; }
        public string ConnectionString { get; set; }
        public string AccountKey { get; set; }
    }
}
