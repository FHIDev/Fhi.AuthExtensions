using Fhi.Samples.WebApi.Services.Models;

namespace WebApi.Services
{
    public interface IHealthRecordService
    {
        public HealthRecordModel[] GetHealthRecords();
    }
    internal class HealthRecordService : IHealthRecordService
    {
        public HealthRecordModel[] GetHealthRecords()
        {
            return
           [
                new HealthRecordModel("01012005", "Health Record 1", "Description 1", DateTime.UtcNow),
                new HealthRecordModel("02021970", "Health Record 2", "Description 2", DateTime.UtcNow),
            ];
        }
    }
}


