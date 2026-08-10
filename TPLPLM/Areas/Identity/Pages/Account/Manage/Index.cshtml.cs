using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TPLPLM.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> UserList { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "User Name")]
            public string SelectedUserId { get; set; }

            [Display(Name = "User Role")]
            public string SelectedRole { get; set; }
        }

        private async Task LoadAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            Username = user.UserName;
            Input.SelectedUserId = user.Id;
            Input.SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            // Load all roles
            AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            // Load all users for Admin
            UserList = _userManager.Users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName
            }).ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            var userId = User.IsInRole("Admin") && !string.IsNullOrEmpty(Input.SelectedUserId)
                ? Input.SelectedUserId
                : currentUser.Id;

            await LoadAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            var targetUserId = User.IsInRole("Admin") && !string.IsNullOrEmpty(Input.SelectedUserId)
                ? Input.SelectedUserId
                : currentUser.Id;

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null)
            {
                ModelState.AddModelError(string.Empty, "Selected user not found.");
                await LoadAsync(currentUser.Id);
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(targetUser.Id);
                return Page();
            }

            // Role update logic
            var currentRoles = await _userManager.GetRolesAsync(targetUser);
            if (!currentRoles.Contains(Input.SelectedRole))
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, currentRoles);
                var addResult = await _userManager.AddToRoleAsync(targetUser, Input.SelectedRole);

                if (!removeResult.Succeeded || !addResult.Succeeded)
                {
                    StatusMessage = "Error updating user role.";
                    await LoadAsync(targetUser.Id);
                    return Page();
                }
            }

            // Refresh sign-in if self
            if (targetUser.Id == currentUser.Id)
                await _signInManager.RefreshSignInAsync(currentUser);

            StatusMessage = "User profile updated successfully.";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnGetUserRolesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            return new JsonResult(roles);
        }
    }
}