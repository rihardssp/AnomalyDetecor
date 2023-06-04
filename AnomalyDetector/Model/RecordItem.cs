using System;
using System.Collections.Generic;

namespace AnomalyDetector.Model;

public partial class RecordItem
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public string RecordName { get; set; } = null!;

    public double RecordValue { get; set; }

    public DateTime Date { get; set; }

    public virtual Device Device { get; set; } = null!;
}
