using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Quickstart.UI
{
    public class ResetPasswordInputModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }
        [Required]

        public string Password { get; set; }
        [Required]
        [Compare("Password", ErrorMessage = "Please make sure your passwords match")]
        public string ConfirmPassword { get; set; }
    }
}