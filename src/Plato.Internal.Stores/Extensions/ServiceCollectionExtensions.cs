﻿using Microsoft.Extensions.DependencyInjection;
using Plato.Internal.Data.Abstractions;
using Plato.Internal.Models.Features;
using Plato.Internal.Stores.Settings;
using Plato.Internal.Stores.Roles;
using Plato.Internal.Stores.Users;
using Plato.Internal.Models.Users;
using Plato.Internal.Stores.Abstract;
using Plato.Internal.Stores.Abstractions.Files;
using Plato.Internal.Stores.Abstractions.Roles;
using Plato.Internal.Stores.Abstractions.Settings;
using Plato.Internal.Stores.Abstractions.Shell;
using Plato.Internal.Stores.Abstractions.Users;
using Plato.Internal.Stores.Files;
using Plato.Internal.Stores.Shell;

namespace Plato.Internal.Stores.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddStores(
         this IServiceCollection services)
        {

            // Abstract stores 
            services.AddScoped<IDictionaryStore, DictionaryStore>();
            services.AddScoped<IDocumentStore, DocumentStore>();

            // Ensure query is aware of current db context
            services.AddScoped<IDbQueryConfiguration, DbQueryConfiguration>();
            
            // Files
            services.AddScoped<IFileStore, FileStore>();

            // Shell features
            services.AddScoped<IShellDescriptorStore, ShellDescriptorStore>();
            services.AddScoped<IShellFeatureStore<ShellFeature>, ShellFeatureStore>();

            // Site Settings
            services.AddScoped<ISiteSettingsStore, SiteSettingsStore>();
            
            // Roles
            services.AddScoped<IPlatoRoleStore, PlatoRoleStore>();
            services.AddScoped<IPlatoUserRoleStore<UserRole>, PlatoUserRolesStore>();

            // Users
            services.AddScoped<IPlatoUserStore<User>, PlatoUserStore>();
            services.AddScoped<IUserPhotoStore<UserPhoto>, UserPhotoStore>();

            //services.AddScoped<IUserBannerStore<UserBanner>, UserBannerStore>();

            services.AddScoped<IUserDataItemStore<UserData>, UserDataItemStore>();
            services.AddScoped<IUserDataStore<UserData>, UserDataStore>();

            //services.AddScoped<IUserDetailsStore<UserDetail>, UserDetailsStore>();

            return services;


        }

    }
}
