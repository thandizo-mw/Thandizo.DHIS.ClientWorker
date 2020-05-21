using Autofac;
using Thandizo.DHIS.BLL.Models;
using Thandizo.DHIS.BLL.Services;

namespace Thandizo.DHIS.ClientWorker.Modules
{
    public class ServiceModule : Module
    {
        private readonly DhisConfiguration _dhisConfiguration;

        public ServiceModule(DhisConfiguration dhisConfiguration)
        {
            _dhisConfiguration = dhisConfiguration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PatientService>()
                .As<IPatientService>()
                .WithParameter("dhisConfiguration", _dhisConfiguration);

            builder.RegisterType<PatientDailyStatusService>()
                .As<IPatientDailyStatusService>()
                .WithParameter("dhisConfiguration", _dhisConfiguration);
        }
    }
}
