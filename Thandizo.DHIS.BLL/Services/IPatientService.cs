using System.Threading.Tasks;
using Thandizo.DataModels.General;

namespace Thandizo.DHIS.BLL.Services
{
    public interface IPatientService
    {
        Task<OutputResponse> Post(long patientId);
    }
}