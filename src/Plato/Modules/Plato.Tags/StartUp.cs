﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Plato.Internal.Assets.Abstractions;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Models.Shell;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Messaging.Abstractions;
using Plato.Internal.Search.Abstractions;
using Plato.Tags.Assets;
using Plato.Tags.Handlers;
using Plato.Tags.Models;
using Plato.Tags.Repositories;
using Plato.Tags.Services;
using Plato.Tags.Stores;
using Plato.Tags.Subscribers;

namespace Plato.Tags
{
    public class Startup : StartupBase
    {
        private readonly IShellSettings _shellSettings;

        public Startup(IShellSettings shellSettings)
        {
            _shellSettings = shellSettings;
        }

        public override void ConfigureServices(IServiceCollection services)
        {

            // Feature event handler
            services.AddScoped<IFeatureEventHandler, FeatureEventHandler>();
            
            // Repositories
            services.AddScoped<ITagRepository<TagBase>, TagRepository<TagBase>>();
            services.AddScoped<IEntityTagsRepository<EntityTag>, EntityTagsRepository>();

            // Stores
            services.AddScoped<ITagStore<TagBase>, TagStore<TagBase>>();
            services.AddScoped<IEntityTagStore<EntityTag>, EntityTagStore>();

            // Managers
            services.AddScoped<ITagManager<TagBase>, TagManager<TagBase>>();
            services.AddScoped<IEntityTagManager<EntityTag>, EntityTagManager>();
         
            // Services
            services.AddScoped<ITagService<TagBase>, TagService<TagBase>>();
            services.AddScoped<ITagOccurrencesUpdater<TagBase>, TagOccurrencesUpdater<TagBase>>();

            // Register client assets
            services.AddScoped<IAssetProvider, AssetProvider>();

            // Full text index providers
            services.AddScoped<IFullTextIndexProvider, FullTextIndexes>();
            
        }

        public override void Configure(
            IApplicationBuilder app,
            IRouteBuilder routes,
            IServiceProvider serviceProvider)
        {

            routes.MapAreaRoute(
                name: "TagsWebApi",
                areaName: "Plato.Tags",
                template: "api/tags/{controller}/{action}/{id?}",
                defaults: new { controller = "Tag", action = "Get" }
            );

        }

    }

}