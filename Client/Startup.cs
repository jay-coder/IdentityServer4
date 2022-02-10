using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Client
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var authority = Configuration.GetValue<string>("IdentityServer:Authority");
            var clientId = Configuration.GetValue<string>("IdentityServer:ClientId");
            var clientSecret = Configuration.GetValue<string>("IdentityServer:ClientSecret");
            var responseType = Configuration.GetValue<string>("IdentityServer:ResponseType");
            var usePkce = Configuration.GetValue<bool>("IdentityServer:UsePkce");
            var responseMode = Configuration.GetValue<string>("IdentityServer:ResponseMode");
            var scopes = Configuration.GetValue<string>("IdentityServer:Scopes");
            var saveTokens = Configuration.GetValue<bool>("IdentityServer:SaveTokens");
            var containerHost = Configuration.GetValue<string>("IdentityServer:ContainerHost");

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("cookie")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = responseType;
                options.UsePkce = usePkce;
                options.ResponseMode = responseMode;
                // options.CallbackPath = "/signin-oidc"; // default redirect URI
                // options.Scope.Add("oidc"); // default scope
                // options.Scope.Add("profile"); // default scope
                var scopeList = scopes.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (scopeList != null && scopeList.Any())
                {
                    foreach (var scope in scopeList)
                    {
                        options.Scope.Add(scope);
                    }
                }
                options.SaveTokens = saveTokens;
                //// Allow HTTP: Disable HTTPS
                //options.RequireHttpsMetadata = false;

                // DEV ONLY
                // Container Identity Server replace well-known endpoint
                // e.g. Replace http://is4 to http://localhost:5000, as localhost is unknown
                options.MetadataAddress = $"{containerHost}/.well-known/openid-configuration";
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            // DEV ONLY
            // Container Identity Server Redirection
            // e.g. Replace http://is4 to http://localhost:5000
            app.Use(async (httpcontext, next) =>
            {
                await next();
                if (httpcontext.Response.StatusCode == StatusCodes.Status302Found)
                {
                    var containerHost = Configuration.GetValue<string>("IdentityServer:ContainerHost");
                    var authority = Configuration.GetValue<string>("IdentityServer:Authority");

                    if (!containerHost.Equals(authority, StringComparison.OrdinalIgnoreCase))
                    {
                        string location = httpcontext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location];
                        httpcontext.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location] =
                                location.Replace(containerHost, authority);
                    }

                }

            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
