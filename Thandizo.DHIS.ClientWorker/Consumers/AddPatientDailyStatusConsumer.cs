using MassTransit;
using System.Threading.Tasks;
using Thandizo.DataModels.Contracts;
using Thandizo.DHIS.BLL.Services;

namespace Thandizo.DHIS.ClientWorker.Consumers
{
    public class AddPatientDailyStatusConsumer : IConsumer<IDhisPatientDailyStatusRequest>
    {
        private readonly IPatientDailyStatusService _service;

        public AddPatientDailyStatusConsumer(IPatientDailyStatusService service)
        {
            _service = service;
        }

        public async Task Consume(ConsumeContext<IDhisPatientDailyStatusRequest> context)
        {
            var request = context.Message;
            var response = await _service.Post(request.Statuses);
            await context.RespondAsync(response);
        }
    }
}
