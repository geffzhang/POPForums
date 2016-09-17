﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PopForums.Configuration;
using PopForums.Data.Sql;
using PopForums.Extensions;
using PopForums.ExternalLogin;
using PopForums.Web.Areas.Forums.Authorization;
using PopForums.Web.Areas.Forums.Extensions;

namespace PopForums.Web
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			// Setup configuration sources.
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("config.json")
				.AddEnvironmentVariables();
			Configuration = builder.Build();

			Config.SetPopForumsAppEnvironment(env.ContentRootPath);
		}

		public IConfigurationRoot Configuration { get; set; }

		// This method gets called by the runtime.
		public void ConfigureServices(IServiceCollection services)
		{
			// needed for social logins in POP Forums
			services.AddAuthentication(options => options.SignInScheme = ExternalUserAssociationManager.AuthenticationContextName);

			services.Configure<AuthorizationOptions>(options =>
			{
				// sets claims policies for admin and moderator functions in POP Forums
				options.AddPopForumsPolicies();
			});

			services.AddMvc(options =>
			{
				// identifies users on POP Forums actions
				options.Filters.Add(typeof(PopForumsUserAttribute));
			});

			// TODO: go to primary nuget
			services.AddSignalR();

			// sets up the dependencies for the base, SQL and web libraries in POP Forums
			services.AddPopForumsBase();
			services.AddPopForumsSql();
			services.AddPopForumsWeb();

			// creates an instance of the background services for POP Forums
			services.AddPopForumsBackgroundServices();
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();
			// records exceptions and info to the POP Forums database
			loggerFactory.AddPopForumsLogger(app);

			if (env.IsDevelopment())
			{
				//app.UseBrowserLink();
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			app.UseStatusCodePages();

			app.UseStaticFiles();

			// sets up POP Forums auth and includes social logins if setup in admin
			app.UseCookieAuthenticationForPopForums();

			app.UseSignalR();

			// Add MVC to the request pipeline.
			app.UseMvc(routes =>
			{

				// POP Forums routes
				routes.AddPopForumsRoutes(app);

				// app routes

				routes.MapRoute(
					name: "areaRoute",
					template: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
