using Autofac;
using Thandizo.DHIS.BLL.Services;

namespace Thandizo.DHIS.ClientWorker.Modules
{
    public class ServiceModule : Module
    {
        private readonly string _dhisApiUrl;
        private readonly string _dhisClientUserId;
        private readonly string _dhisClientPassword;

        public ServiceModule(string dhisApiUrl, string dhisClientUserId, string dhisClientPassword)
        {
            _dhisApiUrl = dhisApiUrl;
            _dhisClientPassword = dhisClientPassword;
            _dhisClientUserId = dhisClientUserId;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PatientService>()
                .As<IPatientService>()
                .WithParameter("dhisApiUrl", _dhisApiUrl)
                .WithParameter("clientUserId", _dhisClientUserId)
                .WithParameter("clientPassword", _dhisClientPassword);
        }
    }
}
