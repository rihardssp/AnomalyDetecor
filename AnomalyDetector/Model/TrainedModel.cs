namespace AnomalyDetector.Model;

public partial class TrainedModel
{
    public Guid Id { get; set; }

    public int DeviceId { get; set; }

    public virtual Device Device { get; set; } = null!;
}
