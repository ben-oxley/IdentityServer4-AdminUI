using System;
using System.Reflection;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography.X509Certificates;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Action<DbContextOptionsBuilder> identityBuilder;
            Action<DbContextOptionsBuilder> identityServerBuilder;
            var identityConnectionString = Configuration.GetValue("IdentityConnectionString", Configuration.GetValue<string>("DbConnectionString"));
            var identityServerConnectionString = Configuration.GetValue("IdentityServerConnectionString", Configuration.GetValue<string>("DbConnectionString"));
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            switch (Configuration.GetValue<string>("DbProvider"))
            {
                case "SqlServer":
                    identityBuilder = x => x.UseSqlServer(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseSqlServer(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                case "MySql":
                    identityBuilder = x => x.UseMySql(identityConnectionString, options => {
                        options.MigrationsAssembly(migrationAssembly);
                        });
                    identityServerBuilder = x => x.UseMySql(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                case "PostgreSql":
                    identityBuilder = x => x.UseNpgsql(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseNpgsql(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                default:
                    identityBuilder = x => x.UseSqlite(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseSqlite(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
            }

            services.AddCors();

            services
                .AddIdentityExpressAdminUiConfiguration(identityBuilder) // ASP.NET Core Identity Registrations for AdminUI
                .AddDefaultTokenProviders()
                .AddIdentityExpressUserClaimsPrincipalFactory(); // Claims Principal Factory for loading AdminUI users as .NET Identities

            services.AddScoped<IUserStore<IdentityExpressUser>>(
                x => new IdentityExpressUserStore(x.GetService<IdentityExpressDbContext>())
                {
                    AutoSaveChanges = true
                });


            IIdentityServerBuilder idservBuilder = services.AddIdentityServer();

            try
            {
                string certlocation = Configuration.GetValue<string>("Certificate_Location");
                X509Certificate2 cert = new X509Certificate2(certlocation);
                idservBuilder = idservBuilder.AddSigningCredential(cert);
            }
            catch (Exception e)
            {
                throw new Exception("Could not load certificate. Please choose a valid file location for an X509 public/private keypair certificate (.pfx) and set as the environment variable \"Certificate_Location\"");
            }

            idservBuilder
                .AddOperationalStore(options => options.ConfigureDbContext = identityServerBuilder)
                .AddConfigurationStore(options => options.ConfigureDbContext = identityServerBuilder)
                .AddAspNetIdentity<IdentityExpressUser>(); // ASP.NET Core Identity Integration

            services.AddMvc();

            
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            if (env.IsDevelopment())
            {
                
                app.UseDeveloperExceptionPage();
            }
            

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
