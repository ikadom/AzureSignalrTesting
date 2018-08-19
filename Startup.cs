using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Newtonsoft.Json.Linq;

namespace AzureSignalrTesting
{
    public class Startup
    {
        private const string GitHubClientId = "GitHubClientId";
        private const string GitHubClientSecret = "GitHubClientSecret";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();
            services.AddMvc();
            services.AddSignalR().AddAzureSignalR();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
            .AddGitHub(options =>
            {
                options.ClientId = Configuration[GitHubClientId];
                options.ClientSecret = Configuration[GitHubClientSecret];
                options.Scope.Add("user:email");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = GetUserCompanyInfoAsync
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //app.UseMvc();
            app.UseFileServer();
            app.UseAzureSignalR(routes =>
            {
                routes.MapHub<Chat>("/chat");
            });

            app.UseAuthentication();
            /*
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            */
        }
        private static async Task GetUserCompanyInfoAsync(OAuthCreatingTicketContext context)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

            var response = await context.Backchannel.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);

            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (user.ContainsKey("company"))
            {
                var company = user["company"].ToString();
                var companyIdentity = new ClaimsIdentity(new[]
                {
                    new Claim("Company", company)
                });
                context.Principal.AddIdentity(companyIdentity);
            }
        }
/*
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR().AddAzureSignalR();
        }
*/
    }
}
