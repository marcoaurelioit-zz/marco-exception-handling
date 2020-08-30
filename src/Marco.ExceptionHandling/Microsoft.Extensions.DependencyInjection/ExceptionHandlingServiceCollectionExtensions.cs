using Marco.ExceptionHandling;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExceptionHandlingServiceCollectionExtensions
    {
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddScoped<IExceptionHandler, ExceptionHandler>();

            return services;
        }
    }
}