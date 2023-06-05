using AnomalyDetector.Model;
using AnomalyDetector.Options;
using Azure;
using Azure.AI.AnomalyDetector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnomalyDetector.Services
{
    public class AzureAnomalyDetector : IAnomalyDetector
    {
        private readonly AzureAnomalyDetectorOptions _options;
        private readonly AnomalyDetectorClient _client;
        private readonly VariableStoreContext _context;
        private readonly int _maxTryout = 200;
        private readonly int _slidingWindow = 30;

        public AzureAnomalyDetector(IOptions<AzureAnomalyDetectorOptions> options, VariableStoreContext context)
        {
            _context = context;
            _options = options.Value;
            _client = new AnomalyDetectorClient(new Uri(_options.EndpointUri), new AzureKeyCredential(_options.Credentials));
        }

        public bool DeleteModel(int deviceId)
        {
            var model = _context.TrainedModels.FirstOrDefault(e => e.DeviceId == deviceId);
            if (model != null)
            {
                _context.TrainedModels.Where(e => e.DeviceId == deviceId).ExecuteDelete();
                _client.DeleteMultivariateModel(model.Id.ToString());
            }
            
            return model != null;
        }

        public bool DoesModelExist(int deviceId)
        {
            return _context.TrainedModels.Any(e => e.DeviceId == deviceId);
        }

        public IEnumerable<AnomalyState> GetAnomalies(int deviceId)
        {
            var model = _context.TrainedModels.First(e => e.DeviceId == deviceId);

            var variableTypeCount = _context.RecordItems
                .Where(e => e.DeviceId == deviceId)
                .GroupBy(e => e.RecordName)
                .Count();

            var recordTypes = _context.RecordItems
                .Where(e => e.DeviceId == deviceId)
                .OrderByDescending(e => e.Date)
                .Take(_slidingWindow)
                .ToList();

            var records = _context.RecordItems
                .Where(e => e.DeviceId == deviceId)
                .OrderByDescending(e => e.Date)
                .Take(_slidingWindow * variableTypeCount) // We end up with at least _slidingWindow date entries
                .ToList();

            var dateGrouping = records.GroupBy(e => e.Date).OrderBy(e => e.Key).ToList();
            var nameGrouping = records.GroupBy(e => e.RecordName).OrderBy(e => e.Key).ToList();

            // Construct csv, crude but will work. Groupings are to pad the empty values with 0's

            var variables = new List<VariableValues>();
            foreach (var nameEntry in nameGrouping)
            {
                var variableName = nameEntry.Key;
                var dates = new List<string>();
                var values = new List<float>();
                foreach (var dateEntry in dateGrouping)
                {
                    var entryOfDate = dateEntry.FirstOrDefault(e => e.RecordName == nameEntry.Key);
                    if (entryOfDate != null)
                    {
                        dates.Add(dateEntry.Key.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        values.Add(entryOfDate == null ? 0 : Convert.ToSingle(entryOfDate.RecordValue));
                    }
                }
                variables.Add(new VariableValues(
                    variableName,
                    dates,
                    values
                ));
            }

            Console.WriteLine("Start batch detection, this might take a few minutes...");
            var response = _client.DetectMultivariateLastAnomaly(model.Id.ToString(), new MultivariateLastDetectionOptions(variables));

            return response?.Value?.Results;
        }

        public bool TrainModel(int deviceId, string modelFileLocation)
        {
            // Already trained
            if (_context.TrainedModels.Any(e => e.DeviceId == deviceId))
            {
                return true;
            }

            try
            {
                Console.WriteLine("Training new model...");
                var request = new ModelInfo(modelFileLocation, DateTime.Now.AddYears(-10), DateTime.Now);
                request.SlidingWindow = _slidingWindow;

                Console.WriteLine("Training new model...(it may take a few minutes)");
                AnomalyDetectionModel response = _client.TrainMultivariateModel(request);
                var trained_model_id = response.ModelId;
                Console.WriteLine(string.Format("Training model id is {0}", trained_model_id));

                // Wait until the model is ready. It usually takes several minutes
                ModelStatus? model_status = null;
                int tryout_count = 1;
                response = _client.GetMultivariateModel(trained_model_id);
                while (tryout_count < _maxTryout & model_status != ModelStatus.Ready & model_status != ModelStatus.Failed)
                {
                    Thread.Sleep(1000);
                    response = _client.GetMultivariateModel(trained_model_id);
                    model_status = response.ModelInfo.Status;
                    Console.WriteLine(string.Format("try {0}, model_id: {1}, status: {2}.", tryout_count, trained_model_id, model_status));
                    tryout_count += 1;
                };

                if (model_status == ModelStatus.Ready)
                {
                    _context.TrainedModels.Add(new TrainedModel { DeviceId = deviceId, Id = new Guid(response.ModelId) });
                    _context.SaveChanges();
                    return true;
                }

                if (model_status == ModelStatus.Failed)
                {
                    Console.WriteLine("Creating model failed.");
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Train error. {0}", e.Message));
                throw;
            }
        }
    }
}
