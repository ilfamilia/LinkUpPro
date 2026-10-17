using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LinkUpPro.Core.Application
{
    public static class ServicesRegistration
    {
        public static void AddApplicationLayerIoc(this IServiceCollection services)
        {
            #region Configurations
            services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());
            #endregion

            #region Services IOC
            #endregion
        }
    }
}