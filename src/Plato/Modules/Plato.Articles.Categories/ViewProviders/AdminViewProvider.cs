﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Plato.Categories.Models;
using Plato.Categories.Services;
using Plato.Categories.Stores;
using Plato.Articles.Categories.Models;
using Plato.Articles.Categories.ViewModels;
using Plato.Categories.ViewModels;
using Plato.Internal.Abstractions.Extensions;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Models.Features;

namespace Plato.Articles.Categories.ViewProviders
{
    public class AdminViewProvider : BaseViewProvider<Category>
    {
     
        private readonly ICategoryStore<CategoryHome> _categoryStore;
        private readonly ICategoryManager<CategoryHome> _categoryManager;
        private readonly IFeatureFacade _featureFacade;

        public IStringLocalizer S { get; }
        
        public AdminViewProvider(
            ICategoryStore<CategoryHome> categoryStore,
            ICategoryManager<CategoryHome> categoryManager,
            IStringLocalizer<AdminViewProvider> stringLocalizer,
            IFeatureFacade featureFacade)
        {
            _categoryStore = categoryStore;
            _categoryManager = categoryManager;

            S = stringLocalizer;
            _featureFacade = featureFacade;
        }

        #region "Implementation"

        public override Task<IViewProviderResult> BuildIndexAsync(Category categoryBase, IViewProviderContext updater)
        {

            var viewModel = new CategoryIndexViewModel()
            {
                Options = new CategoryIndexOptions()
                {
                    ChannelId = categoryBase?.Id ?? 0,
                    EnableEdit = true
                }
            };
            
            return Task.FromResult(Views(
                View<CategoryBase>("Admin.Index.Header", model => categoryBase).Zone("header").Order(1),
                View<CategoryIndexViewModel>("Admin.Index.Tools", model => viewModel).Zone("tools").Order(1),
                View<CategoryIndexViewModel>("Admin.Index.Content", model => viewModel).Zone("content").Order(1)
            ));

        }

        public override Task<IViewProviderResult> BuildDisplayAsync(Category viewModel, IViewProviderContext updater)
        {
            return Task.FromResult(default(IViewProviderResult));
        }
        
        public override async Task<IViewProviderResult> BuildEditAsync(Category categoryBase, IViewProviderContext updater)
        {

            var defaultIcons = new DefaultIcons();

            EditChannelViewModel editChannelViewModel = null;
            if (categoryBase.Id == 0)
            {
                editChannelViewModel = new EditChannelViewModel()
                {
                    IconPrefix = defaultIcons.Prefix,
                    ChannelIcons = defaultIcons,
                    IsNewChannel = true,
                    ParentId = categoryBase.ParentId,
                    AvailableChannels = await GetAvailableCategories()
                };
            }
            else
            {
                editChannelViewModel = new EditChannelViewModel()
                {
                    Id = categoryBase.Id,
                    ParentId = categoryBase.ParentId,
                    Name = categoryBase.Name,
                    Description = categoryBase.Description,
                    ForeColor = categoryBase.ForeColor,
                    BackColor = categoryBase.BackColor,
                    IconCss = categoryBase.IconCss,
                    IconPrefix = defaultIcons.Prefix,
                    ChannelIcons = defaultIcons,
                    AvailableChannels = await GetAvailableCategories()
                };
            }
            
            return Views(
                View<EditChannelViewModel>("Admin.Edit.Header", model => editChannelViewModel).Zone("header").Order(1),
                View<EditChannelViewModel>("Admin.Edit.Content", model => editChannelViewModel).Zone("content").Order(1),
                View<EditChannelViewModel>("Admin.Edit.Actions", model => editChannelViewModel).Zone("actions").Order(1),
                View<EditChannelViewModel>("Admin.Edit.Footer", model => editChannelViewModel).Zone("footer").Order(1)
            );
        }

        public override async Task<IViewProviderResult> BuildUpdateAsync(Category categoryBase, IViewProviderContext context)
        {

            var model = new EditChannelViewModel();

            if (!await context.Updater.TryUpdateModelAsync(model))
            {
                return await BuildEditAsync(categoryBase, context);
            }

            model.Name = model.Name?.Trim();
            model.Description = model.Description?.Trim();
  
            if (context.Updater.ModelState.IsValid)
            {

                var iconCss = model.IconCss;
                if (!string.IsNullOrEmpty(iconCss))
                {
                    iconCss = model.IconPrefix + iconCss;
                }

                var result = await _categoryManager.UpdateAsync(new CategoryHome()
                {
                    Id = categoryBase.Id,
                    FeatureId = categoryBase.FeatureId,
                    ParentId = model.ParentId,
                    Name = model.Name,
                    Description = model.Description,
                    ForeColor = model.ForeColor,
                    BackColor = model.BackColor,
                    IconCss = iconCss,
                    SortOrder = categoryBase.SortOrder
                });

                foreach (var error in result.Errors)
                {
                    context.Updater.ModelState.AddModelError(string.Empty, error.Description);
                }

            }

            return await BuildEditAsync(categoryBase, context);
            
        }

        #endregion

        #region "Private Methods"

        async Task<IEnumerable<SelectListItem>> GetAvailableCategories()
        {

            var output = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = S["root channel"],
                    Value = "0"
                }
            };
            
            var feature = await GetCurrentFeature();
            var channels = await _categoryStore.GetByFeatureIdAsync(feature.Id);
            if (channels != null)
            {
                var items = await RecurseChannels(channels);
                foreach (var item in items)
                {
                    output.Add(item);
                }
            }
          
            return output;

        }

        Task<IList<SelectListItem>> RecurseChannels(
            IEnumerable<ICategory> input,
            IList<SelectListItem> output = null,
            int id = 0)
        {

            if (output == null)
            {
                output = new List<SelectListItem>();
            }
            
            var categories = input.ToList();
            foreach (var category in categories)
            {
                if (category.ParentId == id)
                {
                    var indent = "-".Repeat(category.Depth);
                    if (!string.IsNullOrEmpty(indent))
                    {
                        indent += " ";
                    }
                    output.Add(new SelectListItem
                    {
                        Text = indent + category.Name,
                        Value = category.Id.ToString()
                    });
                    RecurseChannels(categories, output, category.Id);
                }
            }

            return Task.FromResult(output);

        }
        
        async Task<IShellFeature> GetCurrentFeature()
        {
            var featureId = "Plato.Articles.Categories";
            var feature = await _featureFacade.GetFeatureByIdAsync(featureId);
            if (feature == null)
            {
                throw new Exception($"No feature could be found for the Id '{featureId}'");
            }
            return feature;
        }

        #endregion

    }
}
