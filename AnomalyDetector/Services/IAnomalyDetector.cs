using AnomalyDetector.Models;
using Azure.AI.AnomalyDetector;

namespace AnomalyDetector.Services
{
    public interface IAnomalyDetector
    {
        bool DoesModelExist(int deviceId);
        IEnumerable<AnomalyState> GetAnomalies(int deviceId);
        bool TrainModel(int deviceId, string modelFileLocation);

        bool DeleteModel(int deviceId);
    }
}
