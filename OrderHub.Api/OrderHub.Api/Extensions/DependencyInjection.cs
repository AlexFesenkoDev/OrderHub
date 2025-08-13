using OrderHub.Api.Services;

namespace OrderHub.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            services.AddScoped<OrderService>();
            return services;
        }
    }
}
