﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Plato.Users.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plato.Internal.Abstractions.Extensions;
using Plato.Internal.Layout;
using Plato.Internal.Layout.Alerts;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Models.Users;
using Plato.Internal.Navigation.Abstractions;
using Plato.Internal.Security.Abstractions;
using Plato.Internal.Stores.Abstractions.Users;
using Plato.Users.Services;

namespace Plato.Users.Controllers
{

    public class AccountController : Controller, IUpdateModel
    {

        #region "Constructor"

        private readonly IViewProviderManager<UserRegistration> _registerViewProvider;
        private readonly IViewProviderManager<UserLogin> _loginViewProvider;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly IPlatoUserManager<User> _platoUserManager;
        private readonly IBreadCrumbManager _breadCrumbManager;
        private readonly IPlatoUserStore<User> _platoUserStore;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;        
        private readonly UserManager<User> _userManager;
        private readonly IUserEmails _userEmails;
        private readonly IAlerter _alerter;
 
        public IHtmlLocalizer T { get; }

        public IStringLocalizer S { get; }

        public AccountController(
            IHtmlLocalizer htmlLocalizer,
            IStringLocalizer stringLocalizer,
            IViewProviderManager<UserRegistration> registerViewProvider,
            IViewProviderManager<UserLogin> loginViewProvider,
            IOptions<IdentityOptions> identityOptions,
            IPlatoUserManager<User> platoUserManager,
            IPlatoUserStore<User> platoUserStore,
            IBreadCrumbManager breadCrumbManager,
            ILogger<AccountController> logger,            
            SignInManager<User> signInManage,
            UserManager<User> userManager,
            IUserEmails userEmails,
            IAlerter alerter)
        {   
            _registerViewProvider = registerViewProvider;
            _breadCrumbManager = breadCrumbManager;
            _loginViewProvider = loginViewProvider;
            _platoUserManager = platoUserManager;
            _identityOptions = identityOptions;
            _platoUserStore = platoUserStore;
            _signInManager = signInManage;
            _userManager = userManager;
            _userEmails = userEmails;
            _alerter = alerter;            
            _logger = logger;

            T = htmlLocalizer;
            S = stringLocalizer;

        }

        #endregion

        #region "Actions"

        // -----------------
        // Login
        // -----------------

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
                        
            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Login"]);
            });

            // Persist returnUrl
            ViewData["ReturnUrl"] = returnUrl;

            // Return view
            return View((LayoutViewModel) await _loginViewProvider.ProvideIndexAsync(new UserLogin(), this));
          
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {

            // Persist returnUrl
            ViewData["ReturnUrl"] = returnUrl;

            // Build view provider model
            var userLogin = new UserLogin()
            {
                UserName = model.UserName,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            // Validate model state within all involved view providers
            if (await _loginViewProvider.IsModelStateValidAsync(userLogin, this))
            {

                // Get composed type from all involved view providers
                userLogin = await _loginViewProvider.ComposeModelAsync(userLogin, this);

                // Get sign in result
                var result = await _signInManager.PasswordSignInAsync(
                    userLogin.UserName,
                    userLogin.Password,
                    userLogin.RememberMe,
                    lockoutOnFailure: false);

                // Success
                if (result.Succeeded)
                {
                    
                    // Post-authentication checks
                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (user != null)
                    {

                        var signOut = false;
                        if (user.IsBanned)
                        {
                            signOut = true;
                            ModelState.AddModelError(string.Empty, "You cannot login as your account has been banned");
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(1, $"A banned user \"{user.UserName}\" attempted to login but was automatically denied.");
                            }
                        }
                        
                        if (signOut)
                        {
                            await _signInManager.SignOutAsync();
                        }
                        
                    }

                    // Execute view providers update method
                    var viewResult = await _loginViewProvider.ProvideUpdateAsync(userLogin, this);

                    // No further errors have occurred perform final redirect
                    if (ModelState.ErrorCount == 0)
                    {
                        // Redirect to returnUrl
                        return RedirectToLocal(returnUrl);
                    }

                    // Display errors from Update method
                    return View((LayoutViewModel) viewResult);

                }

                if (result.RequiresTwoFactor)
                {
                    //return RedirectToAction(nameof(LoginWith2fa), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    if (ModelState.ErrorCount == 0)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Account requires two factor authentication.");
                    }
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    if (ModelState.ErrorCount == 0)
                    {
                        ModelState.AddModelError(string.Empty, "Account locked out.");
                    }
                }

                // Inform the user the account requires confirmation
                if (_identityOptions.Value.SignIn.RequireConfirmedEmail)
                {
                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (user != null)
                    {
                        var validPassword = await _userManager.CheckPasswordAsync(user, model.Password);
                        if (validPassword)
                        {
                            // Valid credentials entered
                            if (ModelState.ErrorCount == 0)
                            {
                                ModelState.AddModelError(string.Empty,
                                    "Before you can login you must first confirm your email address. Use the \"Confirm your email address\" link below to resend your account confirmation email.");
                            }
                        }
                    }
                }
                
                // Invalid login credentials
                if (ModelState.ErrorCount == 0)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }

            }

            // if we reach this point some view model validation
            // failed within a view provider, display model state errors
            foreach (var modelState in ViewData.ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    //_alerter.Danger(T[error.ErrorMessage]);
                }
            }

            return await Login(returnUrl);

        }

        // -----------------
        // Register
        // -----------------

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
       
            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Register"]);
            });

            // Add return Url to viewData
            ViewData["ReturnUrl"] = returnUrl;
                        
            // Return view
            return View((LayoutViewModel) await _registerViewProvider.ProvideIndexAsync(new UserRegistration(), this));

        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel viewModel,  string returnUrl = null)
        {

            // Persist returnUrl
            ViewData["ReturnUrl"] = returnUrl;

            // Build model for view providers
            var registration = new UserRegistration()
            {
                UserName = viewModel.UserName,
                Email = viewModel.Email,
                Password = viewModel.Password,
                ConfirmPassword = viewModel.ConfirmPassword
            };

            // Validate model state within all involved view providers
            if (await _registerViewProvider.IsModelStateValidAsync(registration, this))
            {

                // Get composed type from all involved view providers
                registration = await _registerViewProvider.ComposeModelAsync(registration, this);

                // Create the user from composed type
                var result = await _platoUserManager.CreateAsync(
                    registration.UserName,
                    registration.Email,
                    registration.Password);
        
                //var result = await _userManager.CreateAsync(registerViewModel, registerViewModel.Password);
                if (result.Succeeded)
                {

                    // Indicate new flag to allow optional update
                    // on first creation within any involved view provider
                    registration.IsNewUser = true;

                    // Execute ProvideUpdateAsync
                    await _registerViewProvider.ProvideUpdateAsync(registration, this);
                    
                    // Success - Redirect to confirmation page
                    return RedirectToAction(nameof(RegisterConfirmation));

                }
                else
                {
                    // Report errors that may have occurred whilst creating the user
                    foreach (var error in result.Errors)
                    {
                        ViewData.ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
             
            }
        
            // if we reach this point some view model validation
            // failed within a view provider, display model state errors
            foreach (var modelState in ViewData.ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    //_alerter.Danger(T[error.ErrorMessage]);
                }
            }

            return await Register(returnUrl);

        }

        [HttpGet, AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Register"]);
            });

            return View();
        }
        
        // -----------------
        // Logoff
        // -----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return Redirect("~/");
        }
        
        // -----------------
        // Confirm Email
        // -----------------

        [HttpGet, AllowAnonymous]
        public IActionResult ConfirmEmail()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Confirm Email"]);
            });

            return View(new ConfirmEmailViewModel());
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
        {

            if (ModelState.IsValid)
            {
                var result = await _platoUserManager.GetEmailConfirmationUserAsync(model.UserIdentifier);
                if (result.Succeeded)
                {
                    var user = result.Response;
                    if (user != null)
                    {
                        user.ConfirmationToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.ConfirmationToken));
                        var emailResult = await _userEmails.SendEmailConfirmationTokenAsync(user);
                        if (!emailResult.Succeeded)
                        {
                            foreach (var error in emailResult.Errors)
                            {
                                ViewData.ModelState.AddModelError(string.Empty, error.Description);
                            }

                            return View(model);
                        }
                    }
                }
            }

            return RedirectToAction(nameof(ConfirmEmailConfirmation));
        }
        
        [HttpGet, AllowAnonymous]
        public IActionResult ConfirmEmailConfirmation()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Confirm Email"]);
            });

            return View();
        }
        
        // -----------------
        // Activate Account
        // -----------------

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ActivateAccount(string code = null)
        {
            
            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Activate Account"]);
            });

            var isValidConfirmationToken = false;
            if (!String.IsNullOrEmpty(code))
            {
                if (code.IsBase64String())
                {
                    var user = await _platoUserStore.GetByConfirmationToken(
                        Encoding.UTF8.GetString(Convert.FromBase64String(code)));
                    if (user != null)
                    {
                        isValidConfirmationToken = true;
                    }
                }
            }

            return View(new ActivateAccountViewModel
            {
                IsValidConfirmationToken = isValidConfirmationToken,
                ConfirmationToken = code
            });

        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAccount(ActivateAccountViewModel model)
        {

            var rolesToAdd = new List<string>()
            {
                DefaultRoles.Member
            };
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Ensure the user account matches the confirmation token
                    var confirmationToken = Encoding.UTF8.GetString(Convert.FromBase64String(model.ConfirmationToken));
                    if (user.ConfirmationToken == confirmationToken)
                    {

                        // Update EmailConfirmed
                        var result = await _platoUserManager.ConfirmEmailAsync(model.Email, confirmationToken);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ViewData.ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }

                        return RedirectToLocal(Url.Action("ActivateAccountConfirmation"));

                    }

                }

            }

            // If we reach this point the found user's confirmation token does not match the supplied confirmation code
            ViewData.ModelState.AddModelError(string.Empty, "The email address does not match the confirmation token");
            return await ActivateAccount(model.ConfirmationToken);
        }
        
        [HttpGet, AllowAnonymous]
        public IActionResult ActivateAccountConfirmation()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Activate Account"]);
            });
            
            return View();

        }
        
        // -----------------
        // Forgot Password
        // -----------------

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Forgot Password"]);
            });
            
            return View(new ForgotPasswordViewModel());
        }
        
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
          
            if (ModelState.IsValid)
            {
                var result = await _platoUserManager.GetForgotPasswordUserAsync(model.UserIdentifier);
                if (result.Succeeded)
                {
                    var user = result.Response;
                    if (user != null)
                    {
                        // Ensure account has been confirmed
                        if (await _userManager.IsEmailConfirmedAsync(user))
                        {
                            user.ResetToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.ResetToken));
                            var emailResult = await _userEmails.SendPasswordResetTokenAsync(user);
                            if (!emailResult.Succeeded)
                            {
                                foreach (var error in emailResult.Errors)
                                {
                                    ViewData.ModelState.AddModelError(string.Empty, error.Description);
                                }

                                return View(model);
                            }
                        }
                    }
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
            
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Forgot Password"]);
            });

            return View();
        }
        
        // -----------------
        // Reset Password
        // -----------------

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string code = null)
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Reset Password"]);
            });

            // Check token
            var isValidResetToken = false;
            if (!String.IsNullOrEmpty(code))
            {
                if (code.IsBase64String())
                {
                    var user = await _platoUserStore.GetByResetToken(Encoding.UTF8.GetString(Convert.FromBase64String(code)));
                    if (user != null)
                    {
                        isValidResetToken = true;
                    }
                }
                else
                {
                    var user = await _platoUserStore.GetByResetToken(code);
                    if (user != null)
                    {
                        isValidResetToken = true;
                    }
                }
            }

            // Return view
            return View(new ResetPasswordViewModel
            {
                IsValidResetToken = isValidResetToken,
                ResetToken = code
            });
        }
        
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
         
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Ensure the user account matches the reset token
                    var resetToken = Encoding.UTF8.GetString(Convert.FromBase64String(model.ResetToken));
                    if (user.ResetToken == resetToken)
                    {
                        var result = await _platoUserManager.ResetPasswordAsync(
                            model.Email,
                            resetToken,
                            model.NewPassword);
                        if (result.Succeeded)
                        {
                            return RedirectToLocal(Url.Action("ResetPasswordConfirmation"));
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                ViewData.ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                }
            }

            // If we reach this point the found user's reset token does not match the supplied reset token
            ViewData.ModelState.AddModelError(string.Empty, "The email address does not match the reset token");
            return await ResetPassword(model.ResetToken);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {

            // Build breadcrumb
            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Home", "Plato.Core")
                    .LocalNav()
                ).Add(S["Reset Password"]);
            });

            return View();
        }
        
        // -----------------
        // Lock out
        // -----------------

        [HttpGet, AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        // -----------------
        // Two factor
        // -----------------

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                }

                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        #endregion

        #region "Private Methods"

        IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("~/");
            }
        }
        
        #endregion

    }

}
