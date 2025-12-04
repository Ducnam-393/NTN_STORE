using Microsoft.Extensions.Caching.Memory;
using System;

namespace NTN_STORE.Services
{
    public class QrLoginService
    {
        private readonly IMemoryCache _cache;

        public QrLoginService(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Tạo mã Token mới cho phiên đăng nhập
        public string GenerateToken()
        {
            var token = Guid.NewGuid().ToString();
            // Lưu trạng thái "Pending" trong 5 phút
            _cache.Set(token, "Pending", TimeSpan.FromMinutes(60));
            return token;
        }

        // Điện thoại gọi hàm này để xác nhận đăng nhập
        public bool ConfirmToken(string token, string userId)
        {
            if (_cache.TryGetValue(token, out _))
            {
                // Cập nhật trạng thái token thành UserId của người quét
                _cache.Set(token, userId, TimeSpan.FromMinutes(5));
                return true;
            }
            return false;
        }

        // Trình duyệt gọi hàm này để kiểm tra trạng thái
        public string GetTokenStatus(string token)
        {
            if (_cache.TryGetValue(token, out string status))
            {
                return status; // Trả về "Pending" hoặc "UserId"
            }
            return "Expired"; // Mã đã hết hạn
        }
    }
}