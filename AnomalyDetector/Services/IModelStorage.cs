namespace AnomalyDetector.Services
{
    public interface IModelStorage
    {
        Task<string> CreateModelFromEntries(int deviceId);
    }
}
