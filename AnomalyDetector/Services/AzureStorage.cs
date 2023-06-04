using AnomalyDetector.Model;
using AnomalyDetector.Options;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using System.Text;

namespace AnomalyDetector.Services
{
    public class AzureStorage : IModelStorage
    {
        private readonly AzureStorageOptions _options;
        private readonly VariableStoreContext _context;

        public AzureStorage(IOptions<AzureStorageOptions> options, VariableStoreContext context) 
        {
            _options = options.Value;
            _context = context; 
        }

        public async Task<string>  CreateModelFromEntries(int deviceId)
        {
            var uri = $"https://{_options.AccountName}.blob.core.windows.net";
            var modelName = $"Model{deviceId}.csv";
            var records = _context.RecordItems.Where(e => e.DeviceId == deviceId)
                .ToList();

            var dateGrouping = records.GroupBy(e => e.Date).OrderBy(e => e.Key).ToList();
            var nameGrouping = records.GroupBy(e => e.RecordName).OrderBy(e => e.Key).ToList();

            // Create a blob service client
            var sharedKeyCredential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            var blobServiceClient = new BlobServiceClient(new Uri(uri), sharedKeyCredential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(modelName);

            // Construct csv, crude but will work. Groupings are to pad the empty values with 0's
            var st = new StringBuilder();
            st.Append("timestamp,");
            foreach (var nameEntry in nameGrouping)
            {
                // Create labels for csv
                st.Append($"{nameEntry.Key},");
            }
            st.Remove(st.Length - 1, 1).Append(Environment.NewLine);

            foreach (var dateEntry in dateGrouping)
            {
                st.Append($"{dateEntry.Key.ToString("yyyy-MM-ddTHH:mm:ssZ")},");
                foreach (var nameEntry in nameGrouping)
                {
                    var entryOfDate = dateEntry.FirstOrDefault(e => e.RecordName == nameEntry.Key);
                    st.Append($"{entryOfDate?.RecordValue ?? 0},");
                }
                // Remove last comma
                st.Remove(st.Length - 1, 1).Append(Environment.NewLine);
            }

            // Convert the content to a stream
            using var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(st.ToString());
            writer.Flush();
            stream.Position = 0;

            // Upload the stream as a blob
            try
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            } catch(Exception ex)
            {
                return string.Empty;
            }

            return $"{uri}/{_options.ContainerName}/{modelName}";
        }
    }
}
