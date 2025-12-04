using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace NTN_STORE.Models
{
    // 1. Phiếu Nhập Kho (Header)
    public class StockImport
    {
        public int Id { get; set; }
        public string Code { get; set; } // Mã phiếu (VD: IMP-20241129-01)

        public string UserId { get; set; } // Người nhập
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Note { get; set; } // Ghi chú (VD: Nhập hàng vụ Đông)
        public decimal TotalCost { get; set; } // Tổng tiền nhập

        public ICollection<StockImportDetail> Details { get; set; }
    }

    // 2. Chi tiết Phiếu Nhập (Body)
    public class StockImportDetail
    {
        public int Id { get; set; }
        public int StockImportId { get; set; }
        [ForeignKey("StockImportId")]
        public StockImport StockImport { get; set; }

        public int ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public ProductVariant Variant { get; set; }

        public int Quantity { get; set; } // Số lượng nhập
        public decimal UnitPrice { get; set; } // Giá nhập (lúc nhập)
    }

    // 3. Lịch sử biến động kho (Log)
    public class InventoryLog
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public ProductVariant Variant { get; set; }

        public string Action { get; set; } // "Import" (Nhập), "Sale" (Bán), "Return" (Trả hàng), "Adjustment" (Cân bằng)
        public int ChangeAmount { get; set; } // Số lượng thay đổi (+10 hoặc -5)
        public int RemainingStock { get; set; } // Tồn kho SAU KHI thay đổi (Snapshot)

        public string? ReferenceCode { get; set; } // Mã đơn hàng hoặc Mã phiếu nhập
        public string UserId { get; set; } // Ai làm?
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
