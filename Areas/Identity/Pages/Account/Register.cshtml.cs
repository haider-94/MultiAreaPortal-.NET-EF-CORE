using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MultiAreaPortal.Data;

namespace MultiAreaPortal.Areas.Identity.Pages.Account;

// Overrides the default Identity UI Register page so every self-service sign-up
// is created and placed into the "Customer" role, then signed in immediately.
[AllowAnonymous]
public class RegisterModel(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid)
            return Page();

        var user = new IdentityUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            logger.LogInformation("New customer account created.");
            await userManager.AddToRoleAsync(user, DbSeeder.CustomerRole);
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
