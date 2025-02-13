﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plato.Email.Stores;
using Plato.Email.ViewModels;
using Plato.Internal.Abstractions.Settings;
using Plato.Internal.Emails.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Models.Shell;

namespace Plato.Email.ViewProviders
{
    public class AdminViewProvider : BaseViewProvider<EmailSettings>
    {

        private readonly IEmailSettingsStore<EmailSettings> _emailSettingsStore;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<AdminViewProvider> _logger;
        private readonly IShellSettings _shellSettings;
        private readonly IPlatoHost _platoHost;

        private readonly PlatoOptions _platoOptions;

        public AdminViewProvider(
            IEmailSettingsStore<EmailSettings> emailSettingsStore,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<AdminViewProvider> logger,
            IShellSettings shellSettings,
            IOptions<PlatoOptions> platoOptions,
            IPlatoHost platoHost)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _emailSettingsStore = emailSettingsStore;
            _shellSettings = shellSettings;
            _platoHost = platoHost;
            _platoOptions = platoOptions.Value;
            _logger = logger;
        }
        
        public override Task<IViewProviderResult> BuildIndexAsync(EmailSettings settings, IViewProviderContext context)
        {
            return Task.FromResult(default(IViewProviderResult));
        }
        
        public override Task<IViewProviderResult> BuildDisplayAsync(EmailSettings settings, IViewProviderContext context)
        {
            return Task.FromResult(default(IViewProviderResult));
        }

        public override async Task<IViewProviderResult> BuildEditAsync(EmailSettings settings, IViewProviderContext context)
        {
            var viewModel = await GetModel();
            return Views(
                View<EmailSettingsViewModel>("Admin.Edit.Header", model => viewModel).Zone("header").Order(1),
                View<EmailSettingsViewModel>("Admin.Edit.Tools", model => viewModel).Zone("tools").Order(1),
                View<EmailSettingsViewModel>("Admin.Edit.Content", model => viewModel).Zone("content").Order(1)
            );
        }

        public override async Task<IViewProviderResult> BuildUpdateAsync(EmailSettings settings, IViewProviderContext context)
        {
            
            var model = new EmailSettingsViewModel();

            // Validate model
            if (!await context.Updater.TryUpdateModelAsync(model))
            {
                return await BuildEditAsync(settings, context);
            }
            
            // Update settings
            if (context.Updater.ModelState.IsValid)
            {

                // Encrypt the password
                var username = model.SmtpSettings.UserName;
                var password = string.Empty;
                if (!string.IsNullOrWhiteSpace(model.SmtpSettings.Password))
                {
                    try
                    {
                        var protector = _dataProtectionProvider.CreateProtector(nameof(SmtpSettings));
                        password = protector.Protect(model.SmtpSettings.Password);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"There was a problem encrypting the SMTP password. {e.Message}");
                    }
                }
                
                settings = new EmailSettings()
                {
                    SmtpSettings = new SmtpSettings()
                    {
                        DefaultFrom = model.SmtpSettings.DefaultFrom,
                        Host = model.SmtpSettings.Host,
                        Port = model.SmtpSettings.Port,
                        UserName = username,
                        Password = password,
                        RequireCredentials = model.SmtpSettings.RequireCredentials,
                        EnableSsl = model.SmtpSettings.EnableSsl,
                        PollingInterval = model.SmtpSettings.PollInterval,
                        BatchSize = model.SmtpSettings.BatchSize,
                        SendAttempts = model.SmtpSettings.SendAttempts,
                        EnablePolling = model.SmtpSettings.EnablePolling
                    }
                };

                var result = await _emailSettingsStore.SaveAsync(settings);
                if (result != null)
                {
                    // Recycle shell context to ensure changes take effect
                    _platoHost.RecycleShellContext(_shellSettings);
                }
              
            }

            return await BuildEditAsync(settings, context);

        }
        
        async Task<EmailSettingsViewModel> GetModel()
        {

            var settings = await _emailSettingsStore.GetAsync();
            if (settings != null)
            {
                return new EmailSettingsViewModel()
                {
                    SmtpSettings = new SmtpSettingsViewModel()
                    {
                        DefaultFrom = _platoOptions.DemoMode
                            ? "email@example.com"
                            : settings.SmtpSettings.DefaultFrom,
                        Host = _platoOptions.DemoMode
                            ? "smtp.example.com"
                            : settings.SmtpSettings.Host,                        
                        Port = settings.SmtpSettings.Port,
                        UserName = _platoOptions.DemoMode
                            ? "email@example.com"
                            : settings.SmtpSettings.UserName,
                        Password = _platoOptions.DemoMode
                            ? "" 
                            : settings.SmtpSettings.Password,
                        RequireCredentials = settings.SmtpSettings.RequireCredentials,
                        EnableSsl = settings.SmtpSettings.EnableSsl,
                        PollInterval = settings.SmtpSettings.PollingInterval,
                        BatchSize = settings.SmtpSettings.BatchSize,
                        SendAttempts = settings.SmtpSettings.SendAttempts,
                        EnablePolling = settings.SmtpSettings.EnablePolling
                    }
                };
            }

            // return default settings
            return new EmailSettingsViewModel()
            {
                SmtpSettings = new SmtpSettingsViewModel()
            };

        }
        
    }

}
