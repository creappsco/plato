﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Plato.Discuss.Tags.Follow.NotificationTypes;
using Plato.Discuss.Models;
using Plato.Internal.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Models.Notifications;
using Plato.Internal.Notifications.Abstractions;

namespace Plato.Discuss.Tags.Follow.Notifications
{

    public class NewTagWeb : INotificationProvider<Topic>
    {
        
        private readonly IUserNotificationsManager<UserNotification> _userNotificationManager;
        private readonly ICapturedRouterUrlHelper _capturedRouterUrlHelper;

        public IHtmlLocalizer T { get; }

        public IStringLocalizer S { get; }
        
        public NewTagWeb(
            IHtmlLocalizer htmlLocalizer,
            IStringLocalizer stringLocalizer,
            IUserNotificationsManager<UserNotification> userNotificationManager,
            ICapturedRouterUrlHelper capturedRouterUrlHelper)
        {
            _userNotificationManager = userNotificationManager;
            _capturedRouterUrlHelper = capturedRouterUrlHelper;

            T = htmlLocalizer;
            S = stringLocalizer;

        }

        public async Task<ICommandResult<Topic>> SendAsync(INotificationContext<Topic> context)
        {
            
            // Ensure correct notification provider
            if (!context.Notification.Type.Name.Equals(WebNotifications.NewTag.Name, StringComparison.Ordinal))
            {
                return null;
            }

            // Create result
            var result = new CommandResult<Topic>();
            
            // Build user notification
            var baseUri = await _capturedRouterUrlHelper.GetBaseUrlAsync();

            var userNotification = new UserNotification()
            {
                NotificationName = context.Notification.Type.Name,
                UserId = context.Notification.To.Id,
                Title = context.Model.Title,
                Message = S["A topic has been posted with a tag your following"],
                CreatedUserId = context.Model.CreatedUserId,
                Url = _capturedRouterUrlHelper.GetRouteUrl(baseUri, new RouteValueDictionary()
                {
                    ["area"] = "Plato.Discuss",
                    ["controller"] = "Home",
                    ["action"] = "Display",
                    ["opts.id"] = context.Model.Id,
                    ["opts.alias"] = context.Model.Alias
                })
            };

            var userNotificationResult = await _userNotificationManager.CreateAsync(userNotification);
            if (userNotificationResult.Succeeded)
            {
                return result.Success(context.Model);
            }

            return result.Failed(userNotificationResult.Errors?.ToArray());
            
        }

    }
    
}
