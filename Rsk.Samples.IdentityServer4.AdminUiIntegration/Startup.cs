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
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.EntityFramework.DbContexts;

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
                    identityBuilder = x => x.UseMySql(identityConnectionString, options =>
                    {
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
            IIdentityServerBuilder idservBuilder = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Public_Origin")))
                {
                    idservBuilder = services.AddIdentityServer(options =>
                    {
                        options.PublicOrigin = Configuration.GetValue<string>("Public_Origin");
                        options.IssuerUri = Configuration.GetValue<string>("Public_Origin");
                    });
                }
            }
            catch (Exception e)
            {
                idservBuilder = services.AddIdentityServer();
            }

            if (idservBuilder == null)
            {
                idservBuilder = services.AddIdentityServer();
            }


            try
            {
                string certlocation = Configuration.GetValue<string>("Certificate_Location");
                string certPassword = Configuration.GetValue<string>("Certificate_Password");

                X509Certificate2 cert = String.IsNullOrWhiteSpace(certPassword) ? new X509Certificate2(certlocation) : new X509Certificate2(certlocation, certPassword);

                idservBuilder = idservBuilder.AddSigningCredential(cert);
            }
            catch (Exception e)
            {
                throw new Exception("Error: Could not load certificate. Please choose a valid file location for an X509 public/private keypair certificate (.pfx) and set as the environment variable \"Certificate_Location\"" + e.Message, e);
            }


            idservBuilder
                .AddOperationalStore(options => options.ConfigureDbContext = identityServerBuilder)
                            .AddConfigurationStore(options => options.ConfigureDbContext = identityServerBuilder)
                            .AddAspNetIdentity<IdentityExpressUser>(); // ASP.NET Core Identity Integration

            services.AddMvc();

            services.AddAuthentication("Bearer")
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = "http://ids:5003";
                options.RequireHttpsMetadata = false;
                options.ApiName = "admin_ui_webhooks";
            });

            //https://www.identityserver.com/articles/extending-adminui-with-newuser-and-passwordreset-webhooks
            services.AddAuthorization(options =>
            {
                options.AddPolicy("webhook", builder =>
                {
                    builder.AddAuthenticationSchemes("Bearer");
                    builder.RequireScope("admin_ui_webhooks");
                });
            });

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
