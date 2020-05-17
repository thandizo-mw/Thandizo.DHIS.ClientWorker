using MassTransit;
using System.Threading.Tasks;
using Thandizo.DataModels.Contracts;
using Thandizo.DHIS.BLL.Services;

namespace Thandizo.DHIS.ClientWorker.Consumers
{
    public class AddPatientConsumer: IConsumer<IDhisPatientModelRequest>
    {
        private readonly IPatientService _service;

        public AddPatientConsumer(IPatientService service)
        {
            _service = service;
        }

        public async Task Consume(ConsumeContext<IDhisPatientModelRequest> context)
        {
            var request = context.Message;
            var response = await _service.Post(request.PatientId);
            await context.RespondAsync(response);
        }
    }
}
