using Ape.Data; // Make sure this namespace is correct for your DbContext
using Ape.Models; // Make sure this namespace is correct for your UserProfile model
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Ape.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext dbContext) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly ApplicationDbContext _dbContext = dbContext;

        public string? Username { get; set; }

        [TempData]
        public required string StatusMessage { get; set; }

        [BindProperty]
        public required InputModel Input { get; set; }

        public class InputModel
        {
            // Cell Phone
            [Phone]
            [Display(Name = "Cell Phone")]
            [RegularExpression(@"^\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}$", ErrorMessage = "Not a valid format; try ### ###-####")]
            public string? PhoneNumber { get; set; }

            // Home Phone            
            [Phone]
            [Display(Name = "Home Phone")]
            [RegularExpression(@"^\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}$", ErrorMessage = "Not a valid format; try ### ###-####")]
            public string? HomePhoneNumber { get; set; }

            [Display(Name = "First Name")]
            public string? FirstName { get; set; }

            [Display(Name = "Middle Name")]
            public string? MiddleName { get; set; }

            [Display(Name = "Last Name")]
            public string? LastName { get; set; }

            [Display(Name = "Birthday")]
            [DataType(DataType.Date)]
            public string? Birthday { get; set; }

            [Display(Name = "Anniversary")]
            [DataType(DataType.Date)]
            public string? Anniversary { get; set; }

            [Display(Name = "Address Line 1")]
            public string? AddressLine1 { get; set; }

            [Display(Name = "Address Line 2")]
            public string? AddressLine2 { get; set; }

            [Display(Name = "City")]
            public string? City { get; set; }

            [Display(Name = "State")]
            public string? State { get; set; }

            [Display(Name = "Zip Code")]
            public string? ZipCode { get; set; }

            //[Display(Name = "Plot")]
            //public string? Plot { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            var userProfile = await _dbContext.UserProfiles.FindAsync(user.Id);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber, // Add this line to load the Cell Phone number
                FirstName = userProfile?.FirstName ?? string.Empty,
                MiddleName = userProfile?.MiddleName,
                LastName = userProfile?.LastName ?? string.Empty,
                Birthday = userProfile?.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty,
                Anniversary = userProfile?.Anniversary?.ToString("yyyy-MM-dd") ?? string.Empty,
                AddressLine1 = userProfile?.AddressLine1,
                AddressLine2 = userProfile?.AddressLine2,
                City = userProfile?.City,
                State = userProfile?.State,
                ZipCode = userProfile?.ZipCode,
                HomePhoneNumber = userProfile?.HomePhoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update UserProfile (This part is independent of the PhoneNumber update)
            var userProfile = await _dbContext.UserProfiles.FindAsync(user.Id);
            if (userProfile == null)
            {
                userProfile = new UserProfiles { UserId = user.Id, User = user };
                _dbContext.UserProfiles.Add(userProfile);
            }

            userProfile.FirstName = Input.FirstName;
            userProfile.MiddleName = Input.MiddleName;
            userProfile.LastName = Input.LastName;
            userProfile.Birthday = string.IsNullOrEmpty(Input.Birthday) ? (DateTime?)null : DateTime.Parse(Input.Birthday);
            userProfile.Anniversary = string.IsNullOrEmpty(Input.Anniversary) ? (DateTime?)null : DateTime.Parse(Input.Anniversary);
            userProfile.AddressLine1 = Input.AddressLine1;
            userProfile.AddressLine2 = Input.AddressLine2;
            userProfile.City = Input.City;
            userProfile.State = Input.State;
            userProfile.ZipCode = Input.ZipCode;
            //userProfile.Plot = Input.Plot;
            userProfile.HomePhoneNumber = Input.HomePhoneNumber;

            await _dbContext.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}