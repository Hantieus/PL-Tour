using System.ComponentModel.DataAnnotations;

namespace PLTour.Admin.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string Role { get; set; } // "Admin", "Manager", "Staff"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }
    }
}