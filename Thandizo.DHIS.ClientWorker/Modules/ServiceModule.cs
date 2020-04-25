using Autofac;
using Thandizo.DHIS.BLL.Services;

namespace Thandizo.DHIS.ClientWorker.Modules
{
    public class ServiceModule : Module
    {
        private readonly string _dhisApiUrl;

        public ServiceModule(string dhisApiUrl)
        {
            _dhisApiUrl = dhisApiUrl;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PatientService>()
                .As<IPatientService>()
                .WithParameter("dhisApiUrl", _dhisApiUrl);
        }
    }
}
