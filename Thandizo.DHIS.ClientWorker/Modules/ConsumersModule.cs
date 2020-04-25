using Autofac;
using Thandizo.DHIS.ClientWorker.Consumers;

namespace Thandizo.DHIS.ClientWorker.Modules
{
    public class ConsumersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AddPatientToDhisConsumer>();
        }
    }
}
