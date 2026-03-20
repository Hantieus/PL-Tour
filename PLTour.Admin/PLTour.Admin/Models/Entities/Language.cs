using System.ComponentModel.DataAnnotations;

namespace PLTour.Admin.Models.Entities
{
    public class Language
    {
        [Key]
        public int LanguageId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Tiếng Việt, English, 中文, ...

        [Required]
        [StringLength(10)]
        public string Code { get; set; } // vi, en, zh, ko, ja, ...

        [StringLength(50)]
        public string FlagIcon { get; set; } // flag-icon-vn, flag-icon-us, ...

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}