﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Plato.Ideas.Models;
using Plato.Ideas.Tags.Models;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.Alerts;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Tags.Stores;
using Plato.Entities.ViewModels;
using Plato.Internal.Data.Abstractions;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Layout;
using Plato.Internal.Layout.Titles;
using Plato.Internal.Navigation.Abstractions;
using Plato.Tags.Models;
using Plato.Tags.ViewModels;

namespace Plato.Ideas.Tags.Controllers
{

    public class HomeController : Controller, IUpdateModel
    {
        
        private readonly IViewProviderManager<Tag> _tagViewProvider;
        private readonly IBreadCrumbManager _breadCrumbManager;
        private readonly IPageTitleBuilder _pageTitleBuilder;
        private readonly IContextFacade _contextFacade;
        private readonly IFeatureFacade _featureFacade;
        private readonly ITagStore<TagBase> _tagStore;
        private readonly IAlerter _alerter;

        public IHtmlLocalizer T { get; }

        public IStringLocalizer S { get; }
        
        public HomeController(
            IHtmlLocalizer htmlLocalizer,
            IStringLocalizer stringLocalizer,
            IViewProviderManager<Tag> tagViewProvider,
            IBreadCrumbManager breadCrumbManager,
            IContextFacade contextFacade1,
            IFeatureFacade featureFacade,
            ITagStore<TagBase> tagStore,
            IPageTitleBuilder pageTitleBuilder,
            IAlerter alerter)
        {
            
            _breadCrumbManager = breadCrumbManager;
            _tagViewProvider = tagViewProvider;
            _contextFacade = contextFacade1;
            _featureFacade = featureFacade;
            _pageTitleBuilder = pageTitleBuilder;
            _tagStore = tagStore;
            _alerter = alerter;

            T = htmlLocalizer;
            S = stringLocalizer;

        }
        
        #region "Actions"

        public async Task<IActionResult> Index(TagIndexOptions opts, PagerOptions pager)
        {

            // Default options
            if (opts == null)
            {
                opts = new TagIndexOptions();
            }

            // Default pager
            if (pager == null)
            {
                pager = new PagerOptions();
            }
            
            // Get default options
            var defaultViewOptions = new TagIndexOptions();
            var defaultPagerOptions = new PagerOptions();

            // Add non default route data for pagination purposes
            if (opts.Search != defaultViewOptions.Search)
                this.RouteData.Values.Add("opts.search", opts.Search);
            if (opts.Sort != defaultViewOptions.Sort)
                this.RouteData.Values.Add("opts.sort", opts.Sort);
            if (opts.Order != defaultViewOptions.Order)
                this.RouteData.Values.Add("opts.order", opts.Order);
            if (pager.Page != defaultPagerOptions.Page)
                this.RouteData.Values.Add("pager.page", pager.Page);
            if (pager.Size != defaultPagerOptions.Size)
                this.RouteData.Values.Add("pager.size", pager.Size);
            
            // Build view model
            var viewModel = await GetIndexViewModelAsync(opts, pager);

            // Add view model to context
            HttpContext.Items[typeof(TagIndexViewModel<Tag>)] = viewModel;
            
            // If we have a pager.page querystring value return paged results
            if (int.TryParse(HttpContext.Request.Query["pager.page"], out var page))
            {
                if (page > 0)
                    return View("GetTags", viewModel);
            }
            
            // Breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Ideas"], ideas => ideas
                    .Action("Index", "Home", "Plato.Ideas")
                    .LocalNav()
                ).Add(S["Tags"]);
            });

            // Return view
            return View((LayoutViewModel) await _tagViewProvider.ProvideIndexAsync(new Tag(), this));

        }
        
        public async Task<IActionResult> Display(EntityIndexOptions opts, PagerOptions pager)
        {
            
            // Default options
            if (opts == null)
            {
                opts = new EntityIndexOptions();
            }

            // Default pager
            if (pager == null)
            {
                pager = new PagerOptions();
            }

            // Get tag
            var tag = await _tagStore.GetByIdAsync(opts.TagId);

            // Ensure tag exists
            if (tag == null)
            {
                return NotFound();
            }
            
            // Get default options
            var defaultViewOptions = new EntityIndexOptions();
            var defaultPagerOptions = new PagerOptions();

            // Add non default route data for pagination purposes
            if (opts.Search != defaultViewOptions.Search)
                this.RouteData.Values.Add("opts.search", opts.Search);
            if (opts.Sort != defaultViewOptions.Sort)
                this.RouteData.Values.Add("opts.sort", opts.Sort);
            if (opts.Order != defaultViewOptions.Order)
                this.RouteData.Values.Add("opts.order", opts.Order);
            if (opts.Filter != defaultViewOptions.Filter)
                this.RouteData.Values.Add("opts.filter", opts.Filter);
            if (pager.Page != defaultPagerOptions.Page)
                this.RouteData.Values.Add("pager.page", pager.Page);
            if (pager.Size != defaultPagerOptions.Size)
                this.RouteData.Values.Add("pager.size", pager.Size);

            // Build view model
            var viewModel =  await GetDisplayViewModelAsync(opts, pager);

            // Add view model to context
            HttpContext.Items[typeof(EntityIndexViewModel<Idea>)] = viewModel;

            // If we have a pager.page querystring value return paged results
            if (int.TryParse(HttpContext.Request.Query["pager.page"], out var page))
            {
                if (page > 0)
                    return View("GetIdeas", viewModel);
            }

            // Build page title
            _pageTitleBuilder.AddSegment(S[tag.Name], int.MaxValue);

            // Breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Ideas"], ideas => ideas
                    .Action("Index", "Home", "Plato.Ideas")
                    .LocalNav()
                ).Add(S["Tags"], labels => labels
                    .Action("Index", "Home", "Plato.Ideas.Tags")
                    .LocalNav()
                ).Add(S[tag.Name]);
            });

            // Return view
            return View((LayoutViewModel) await _tagViewProvider.ProvideDisplayAsync(new Tag(tag), this));

        }

        #endregion

        async Task<TagIndexViewModel<Tag>> GetIndexViewModelAsync(TagIndexOptions options, PagerOptions pager)
        {

            // Get current feature
            var feature = await _featureFacade.GetFeatureByIdAsync("Plato.Ideas");

            // Restrict results to current feature
            if (feature != null)
            {
                options.FeatureId = feature.Id;
            }

            if (options.Sort == TagSortBy.Auto)
            {
                options.Sort = TagSortBy.Entities;
                options.Order = OrderBy.Desc;
            }
            
            // Set pager call back Url
            pager.Url = _contextFacade.GetRouteUrl(pager.Route(RouteData));

            return new TagIndexViewModel<Tag>()
            {
                Options = options,
                Pager = pager
            };

        }


        async Task<EntityIndexViewModel<Idea>> GetDisplayViewModelAsync(EntityIndexOptions options, PagerOptions pager)
        {
            
            // Get current feature
            var feature = await _featureFacade.GetFeatureByIdAsync("Plato.Ideas");

            // Restrict results to current feature
            if (feature != null)
            {
                options.FeatureId = feature.Id;
            }

            // Set pager call back Url
            pager.Url = _contextFacade.GetRouteUrl(pager.Route(RouteData));
            
            return new EntityIndexViewModel<Idea>()
            {
                Options = options,
                Pager = pager
            };

        }

    }

}
