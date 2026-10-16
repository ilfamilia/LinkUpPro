using Microsoft.AspNetCore.Identity;

namespace LinkUpPro.Infrastructure.Identity.Entities
{
    public class AppUser : IdentityUser
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string ProfilePicturePath { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
