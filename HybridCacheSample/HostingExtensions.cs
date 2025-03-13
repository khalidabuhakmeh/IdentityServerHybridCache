using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Services;
using HybridCacheSample;
using HybridCacheSample.Caches;
using HybridCacheSample.Pages.Admin.ApiScopes;
using HybridCacheSample.Pages.Admin.Clients;
using HybridCacheSample.Pages.Admin.IdentityScopes;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace HybridCacheSample;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        var isBuilder = builder.Services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    //options.Caching.ClientStoreExpiration = TimeSpan.FromMilliseconds(1);

                    // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                    options.EmitStaticAudienceClaim = true;
                })
                .AddTestUsers(TestUsers.Users)
                // this adds the config data from DB (clients, resources, CORS)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.LogTo(Log.Logger.Information, [DbLoggerCategory.Database.Command.Name])
                            .UseSqlServer(connectionString,
                                dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                })
                // this is something you will want in production to reduce load on and requests to the DB
                //.AddConfigurationStoreCache()
                //
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.LogTo(Log.Logger.Information, [DbLoggerCategory.Database.Command.Name])
                            .UseSqlServer(connectionString,
                                dbOpts => dbOpts
                                    .MigrationsAssembly(typeof(Program).Assembly.FullName)
                            );
                })
                // use caches
                .AddConfigurationStoreCache()
            ;


        builder.Services.AddAuthentication();
        // .AddGoogle(options =>
        // {
        //     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        //
        //     // register your IdentityServer with Google at https://console.developers.google.com
        //     // enable the Google+ API
        //     // set the redirect URI to https://localhost:5001/signin-google
        //     options.ClientId = "copy client ID from Google here";
        //     options.ClientSecret = "copy client secret from Google here";
        // });


        // this adds the necessary config for the simple admin/config pages
        {
            builder.Services.AddAuthorization(options =>
                options.AddPolicy("admin",
                    policy => policy.RequireClaim("sub", "1"))
            );

            builder.Services.Configure<RazorPagesOptions>(options =>
                options.Conventions.AuthorizeFolder("/Admin", "admin"));

            builder.Services.AddTransient<HybridCacheSample.Pages.Portal.ClientRepository>();
            builder.Services.AddTransient<ClientRepository>();
            builder.Services.AddTransient<IdentityScopeRepository>();
            builder.Services.AddTransient<ApiScopeRepository>();
        }

        // if you want to use server-side sessions: https://blog.duendesoftware.com/posts/20220406_session_management/
        // then enable it
        //isBuilder.AddServerSideSessions();
        //
        // and put some authorization on the admin/management pages using the same policy created above
        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));

        // overwrite cache implementation
        // builder.Services.AddHybridCache(options =>
        // {
        //     options.DefaultEntryOptions = new()
        //     {
        //         LocalCacheExpiration = TimeSpan.FromMinutes(5),
        //         Expiration = TimeSpan.FromMinutes(5)
        //     };
        // });
        //
        // builder.Services.RemoveAll(typeof(ICache<>));
        // builder.Services.Insert(0,
        //     new ServiceDescriptor(
        //         typeof(ICache<>),
        //         typeof(NoOpCache<>),
        //         ServiceLifetime.Transient
        //     )
        // );

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}