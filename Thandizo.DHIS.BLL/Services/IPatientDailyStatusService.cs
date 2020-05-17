using System.Collections.Generic;
using System.Threading.Tasks;
using Thandizo.DataModels.General;
using Thandizo.DataModels.Patients;

namespace Thandizo.DHIS.BLL.Services
{
    public interface IPatientDailyStatusService
    {
        Task<OutputResponse> Post(IEnumerable<PatientDailyStatusDTO> statuses);
    }
}