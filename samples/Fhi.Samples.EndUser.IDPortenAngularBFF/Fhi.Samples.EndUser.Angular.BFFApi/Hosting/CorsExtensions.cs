namespace Fhi.Samples.EndUser.Angular.BFFApi.Hosting
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddAngularCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}