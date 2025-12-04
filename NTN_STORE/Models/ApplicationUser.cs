using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        public string? ProfilePicture { get; set; } // Đường dẫn ảnh avatar

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; } // "Nam", "Nữ", "Khác"

        // Liên kết với sổ địa chỉ
        public virtual ICollection<UserAddress> Addresses { get; set; }
    }
}