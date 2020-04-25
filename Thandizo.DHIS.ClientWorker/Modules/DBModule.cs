using Autofac;
using Microsoft.EntityFrameworkCore;
using Thandizo.DAL.Models;

namespace Thandizo.DHIS.ClientWorker.Modules
{
    public class DBModule : Module
    {
        private readonly string _connectionString;

        public DBModule(string connectionString)
        {
            _connectionString = connectionString;
        }
        protected override void Load(ContainerBuilder builder)
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<thandizoContext>().UseNpgsql(_connectionString);

            builder.RegisterType<thandizoContext>()
                .WithParameter("options", dbContextOptionsBuilder.Options)
                .InstancePerLifetimeScope();
        }
    }
}
