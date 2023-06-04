using AnomalyDetector.Model;
using AnomalyDetector.Models;
using AnomalyDetector.Services;
using Azure.AI.AnomalyDetector;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AnomalyDetector.Controllers
{
    [Route("api/[controller]")]
    public class Entry : Controller
    {
        private readonly VariableStoreContext _context;
        private readonly IAnomalyDetector _anomalyDetector;
        private readonly IModelStorage _storage;

        public Entry(VariableStoreContext context, IAnomalyDetector anomalyDetector, IModelStorage storage)
        {
            _context = context;
            _anomalyDetector = anomalyDetector;
            _storage = storage;
        }

        [HttpPost("[action]/{deviceName}")]
        public ActionResult<IEnumerable<AnomalyState>> Add(string deviceName, [FromBody] Record record)
        {
            if (record.Values.Length != record.Names.Length)
            {
                return BadRequest();
            };

            var device = _context.Devices.FirstOrDefault(e => e.Name == deviceName);
            if (device == null)
            {
                device = new Device { Name = deviceName };
                _context.Devices.Add(device);
                _context.SaveChanges();
            }

            // Add an entry first, it'll be queried with the latest n entries on detection
            try
            {
                for (var i = 0; i < record.Names.Length; i++)
                {
                    _context.RecordItems.Add(new RecordItem {
                        DeviceId = device.Id,
                        Date = record.Date,
                        RecordName = record.Names[i],
                        RecordValue = record.Values[i]
                    });
                }
                _context.SaveChanges();

            } catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // Check if has model
            if (_anomalyDetector.DoesModelExist(device.Id))
            {
                var anomalies = _anomalyDetector.GetAnomalies(device.Id);
                if (anomalies != null)
                {
                    return Ok(anomalies);
                }
            }

            return NoContent();
        }

        [HttpPost("[action]/{deviceName}")]
        public ActionResult AddBatch(string deviceName, [FromBody] string csv)
        {
            var device = _context.Devices.FirstOrDefault(e => e.Name == deviceName);
            if (device == null)
            {
                device = new Device { Name = deviceName };
                _context.Devices.Add(device);
                _context.SaveChanges();
            }

            // Add batch of entries
            var csvData = ReadCsvFile(csv);
            try
            {
                foreach (var row in csvData)
                {
                    var values = row.Where(e => e.Value is float).ToList();
                    var date = (DateTime)row.First(e => e.Value is DateTime).Value;
                    foreach (var col in values)
                    {
                        _context.RecordItems.Add(new RecordItem
                        {
                            DeviceId = device.Id,
                            RecordName = col.Key,
                            Date = date,
                            RecordValue = (float)col.Value
                        });
                    }
                    
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var msg = new StringBuilder();
                msg.AppendLine(ex.Message);
                msg.AppendLine(ex.StackTrace);
                return BadRequest(msg.ToString());
            }

            return NoContent();
        }

        [HttpGet("[action]/{deviceName}")]
        public async Task<ActionResult<string>> Train(string deviceName)
        {
            var device = _context.Devices.FirstOrDefault(e => e.Name == deviceName);
            if (device == null)
            {
                return NotFound();
            }

            if (_anomalyDetector.DoesModelExist(device.Id))
            {
                return NoContent();
            }

            var modelLocation = await _storage.CreateModelFromEntries(device.Id);
            var success = _anomalyDetector.TrainModel(device.Id, modelLocation);

            return Ok($"Created csv file in {modelLocation}, trained: {success}");
        }


        public static List<Dictionary<string, object>> ReadCsvFile(string csv)
        {
            var csvData = new List<Dictionary<string, object>>();

            using var reader = new StringReader(csv);            
            var columnNames = reader.ReadLine()?.Split(',');

            while (reader.Peek() != -1)
            {
                var rowData = reader.ReadLine()?.Split(',');

                if (rowData != null && rowData.Length == columnNames.Length)
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < columnNames.Length; i++)
                    {

                        row[columnNames[i]] = float.TryParse(rowData[i], out var fres) ? fres : DateTime.Parse(rowData[i]);
                    }

                    csvData.Add(row);
                }
            }
            

            return csvData;
        }
    }
}
