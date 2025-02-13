﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plato.Internal.Abstractions.Routing;
using Plato.Internal.Abstractions.Settings;
using Plato.Internal.Data.Extensions;
using Plato.Internal.FileSystem;
using Plato.Internal.FileSystem.Abstractions;
using Plato.Internal.Hosting.Extensions;
using Plato.Internal.Modules.Extensions;
using Plato.Internal.Repositories.Extensions;
using Plato.Internal.Shell.Extensions;
using Plato.Internal.Stores.Extensions;
using Plato.Internal.Cache.Extensions;
using Plato.Internal.Features.Extensions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Hosting.Web.Routing;
using Plato.Internal.Layout.Extensions;
using Plato.Internal.Security.Extensions;
using Plato.Internal.Logging.Extensions;
using Plato.Internal.Messaging.Extensions;
using Plato.Internal.Assets.Extensions;
using Plato.Internal.Badges.Extensions;
using Plato.Internal.Drawing.Extensions;
using Plato.Internal.Hosting.Web.Configuration;
using Plato.Internal.Localization.Extensions;
using Plato.Internal.Navigation.Extensions;
using Plato.Internal.Net.Extensions;
using Plato.Internal.Notifications.Extensions;
using Plato.Internal.Reputations.Extensions;
using Plato.Internal.Scripting.Extensions;
using Plato.Internal.Search.Extensions;
using Plato.Internal.Tasks.Extensions;
using Plato.Internal.Text.Extensions;
using Plato.Internal.Theming.Extensions;
using Microsoft.AspNetCore.Mvc;
using Plato.Internal.Layout.ViewFeatures;
using Plato.Internal.Layout.LocationExpander;
using Plato.Internal.Modules;
using Plato.Internal.Modules.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace Plato.Internal.Hosting.Web.Extensions
{

    public static class ServiceCollectionExtensions
    {
        
        public static IServiceCollection AddPlato(this IServiceCollection services)
        {
            services.AddPlatoHost();
            services.ConfigureShell("Sites");
            services.AddPlatoDataProtection();
            services.AddPlatoAuthorization();
            services.AddPlatoAuthentication();
            services.AddPlatoMvc();
            return services;
        }

        public static IServiceCollection AddPlatoHost(this IServiceCollection services)
        {
            return services.AddHPlatoTennetHost(internalServices =>
            {

                services.AddHttpContextAccessor();
                internalServices.AddLogging();
                internalServices.AddLogging(loggingBuilder =>
                {
                    //loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddDebug();
                });

                internalServices.AddSingleton<IHostEnvironment, WebHostEnvironment>();
                internalServices.AddSingleton<IPlatoFileSystem, HostedFileSystem>();
                internalServices.AddPlatoContextAccessor();
                internalServices.AddPlatoRouting();

                internalServices.AddOptions();
                internalServices.AddLocalization();
                
                internalServices.AddPlatoOptions();
                internalServices.AddPlatoLocalization();
                internalServices.AddPlatoCaching();
                internalServices.AddPlatoText();
                internalServices.AddPlatoNotifications();
                internalServices.AddPlatoModules();
                internalServices.AddPlatoTheming();
                internalServices.AddPlatoViewFeature();
                internalServices.AddPlatoViewLocalization();
                internalServices.AddPlatoNavigation();
                internalServices.AddPlatoAssets();
                internalServices.AddPlatoNet();
                internalServices.AddPlatoScripting();
                internalServices.AddPlatoShellFeatures();
                internalServices.AddPlatoMessaging();
                internalServices.AddPlatoLogging();
                internalServices.AddPlatoDbContext();
                internalServices.AddPlatoRepositories();
                internalServices.AddPlatoStores();
                internalServices.AddPlatoReputations();
                internalServices.AddPlatoBadges();
                internalServices.AddPlatoDrawing();
                internalServices.AddPlatoTasks();
                internalServices.AddPlatoSearch();

            });

        }

        public static IServiceCollection AddHPlatoTennetHost(this IServiceCollection services, Action<IServiceCollection> configure)
        {

            // Add host
            services.AddPlatoDefaultHost();

            // Add shell
            services.AddPlatoShell();

            // Let the app change the default tenant behavior and set of features
            configure?.Invoke(services);

            // Register the list of services to be resolved later on via ShellContainerFactory
            services.AddSingleton(_ => services);

            return services;

        }

        public static IServiceCollection AddPlatoAuthentication(this IServiceCollection services)
        {

            // Configure antiForgery options
            services.TryAddEnumerable(ServiceDescriptor
                .Transient<IConfigureOptions<AntiforgeryOptions>, AntiForgeryOptionsConfiguration>());

            // Configure authentication services
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                    options => { options.LoginPath = new PathString("/login"); })
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    options.LoginPath = new PathString("/login");
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async context =>
                        {
                            await SecurityStampValidator.ValidatePrincipalAsync(context);
                        }
                    };
                })
                .AddCookie(IdentityConstants.ExternalScheme, options =>
                {
                    options.Cookie.Name = IdentityConstants.ExternalScheme;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddCookie(IdentityConstants.TwoFactorRememberMeScheme,
                    options => { options.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme; })
                .AddCookie(IdentityConstants.TwoFactorUserIdScheme, IdentityConstants.TwoFactorUserIdScheme, options =>
                {
                    options.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                });

            return services;

        }

        public static IServiceCollection AddPlatoMvc(this IServiceCollection services)
        {

            // Configure site options
            services.Configure<SiteOptions>(options => { options.SiteName = "Plato"; });

            // Add mvc core services
            // --------------

            // localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Razor & Views
            var builder = services
                .AddMvcCore()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddViews()
                .AddCacheTagHelper()
                .AddRazorViewEngine();

            // Add default framework parts
            AddDefaultFrameworkParts(builder.PartManager);

            // Add modular application parts
            services.AddPlatoModularAppParts(builder.PartManager);

            // View adapters
            services.AddPlatoViewAdapters();

            // Razor
            //services.AddPlatoRazor();

            // Add module mvc
            services.AddPlatoModuleMvc();

            // Custom view model valiation
            services.AddPlatoModelValidation();

            // Add json formatter
            builder.AddJsonFormatters();

            return services;

        }
        
        private static IServiceCollection AddPlatoModularAppParts(this IServiceCollection services, ApplicationPartManager partManager)
        {
            var serviceProvider = services.BuildServiceProvider();
            partManager.ApplicationParts.Insert(0, new ModularFeatureApplicationPart(serviceProvider));
            partManager.FeatureProviders.Add(new ModuleViewFeatureProvider(serviceProvider));
            return services;
        }
          
        public static IServiceCollection AddPlatoModuleMvc(this IServiceCollection services)
        {
            
            // Location expander            
            services.AddScoped<IViewLocationExpanderProvider, ModularViewLocationExpander>();
            services.AddScoped<IViewLocationExpanderProvider, AreaViewLocationExpander>();
                        
            // Configure Razor options
            services.Configure<RazorViewEngineOptions>(options =>
            {

                options.AllowRecompilingViewsOnFileChange = false;

                // Add composite view location expander
                options.ViewLocationExpanders.Add(new CompositeViewLocationExpander());

                // To let the application behave as a module, its razor files are requested under the virtual
                // "Areas" folder, but they are still served from the file system by this custom provider.
                options.FileProviders.Insert(0, new ModuleViewFileProvider(services.BuildServiceProvider()));

                // Add compilation references for development mode
                // TODO: To be removed
                AddAdditionalCompilationReferences(services, options);

            });

            // implement our own conventions to automatically add [areas] route attributes
            // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model?view=aspnetcore-2.1
            services.TryAddEnumerable(ServiceDescriptor
                .Transient<IApplicationModelProvider, ModuleApplicationModelProvider>());

            return services;

        }

        public static void AddAdditionalCompilationReferences(this IServiceCollection services, RazorViewEngineOptions options)
        {

            // We don't need this for production as views are pre-compiled
            var hostingEnvironment = services.BuildServiceProvider().GetService<IHostingEnvironment>();            
            if (!hostingEnvironment.IsDevelopment())
            {
                return;
            }

            //Ensure loaded modules are aware of current context
            var moduleManager = services.BuildServiceProvider().GetService<IModuleManager>();            
            var assemblies = moduleManager.LoadModuleAssembliesAsync().Result;
            var moduleReferences = assemblies
                .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .ToList();
                 
            foreach (var moduleReference in moduleReferences)
            {
                // TODO: Need to resolve for .NET 3.0 - AdditionalCompilationReferences is marked obsolete in 2.2.1
                // Apps using these APIs to add assembly references to the 
                // compilation context for runtime compilation should instead
                // use ApplicationPartManager.AddApplicationPart to add application
                // parts for each assembly reference, or switch to a built-time compilation model(see Create reusable UI using the Razor Class Library project).
                // https://github.com/aspnet/Announcements/issues/312
                // https://github.com/aspnet/Mvc/issues/4497
                // https://github.com/aspnet/Razor/issues/834
                options.AdditionalCompilationReferences.Add(moduleReference);
            }

        }

        public static IServiceCollection AddPlatoOptions(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<PlatoOptions>, PlatoOptionsConfiguration>();
            return services;
        }

        public static IServiceCollection AddPlatoRouting(this IServiceCollection services)
        {
            services.AddSingleton<ICapturedRouter, CapturedRouter>();
            services.AddSingleton<ICapturedRouterUrlHelper, CapturedRouterUrlHelper>();
            services.AddScoped<IHomeRouteManager, HomeRouteManager>();
            return services;
        }

        public static IServiceCollection AddPlatoContextAccessor(this IServiceCollection services)
        {
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<ICapturedHttpContext, CapturedHttpContext>();
            services.AddTransient<IContextFacade, ContextFacade>();
            return services;
        }

        private static void AddDefaultFrameworkParts(ApplicationPartManager partManager)
        {

            var mvcTagHelpersAssembly = typeof(InputTagHelper).Assembly;
            if (partManager.ApplicationParts.OfType<AssemblyPart>().All(p => p.Assembly != mvcTagHelpersAssembly))
            {
                partManager.ApplicationParts.Add(new AssemblyPart(mvcTagHelpersAssembly));
            }

            var mvcTagHelpersInternalAssembly = typeof(CacheTagHelperMemoryCacheFactory).Assembly;
            if (partManager.ApplicationParts.OfType<AssemblyPart>().All(p => p.Assembly != mvcTagHelpersInternalAssembly))
            {
                partManager.ApplicationParts.Add(new AssemblyPart(mvcTagHelpersInternalAssembly));
            }

            var mvcRazorAssembly = typeof(UrlResolutionTagHelper).Assembly;
            if (partManager.ApplicationParts.OfType<AssemblyPart>().All(p => p.Assembly != mvcRazorAssembly))
            {
                partManager.ApplicationParts.Add(new AssemblyPart(mvcRazorAssembly));
            }

        }

    }

}