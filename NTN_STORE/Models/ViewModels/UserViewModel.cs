using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace NTN_STORE.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsLocked { get; set; } // Trạng thái khóa
        public IEnumerable<string> Roles { get; set; } // Danh sách quyền hiện tại
    }

    public class UserDetailViewModel
    {
        public IdentityUser User { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<Order> Orders { get; set; } // Lịch sử mua hàng
        public List<string> AllRoles { get; set; } // Tất cả quyền có trong hệ thống (để chọn cấp quyền)
    }
}