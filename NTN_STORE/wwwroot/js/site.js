// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Hàm mở Mini Cart và load dữ liệu
function openMiniCart() {
    $('.offcanvas-minicart').addClass('open');
    // Load dữ liệu mới nhất
    $.get('/Cart/GetMiniCart', function (html) {
        $('#miniCartContent').html(html);
    });
}

function closeMiniCart() {
    $('.offcanvas-minicart').removeClass('open');
}

// Hàm xóa item từ Mini Cart
function removeFromMiniCart(id) {
    $.post('/Cart/RemoveFromMiniCart', { id: id }, function (res) {
        if (res.success) {
            // Reload lại nội dung cart
            $.get('/Cart/GetMiniCart', function (html) {
                $('#miniCartContent').html(html);
                // Cập nhật badge số lượng trên Header (nếu có function đó)
            });
        }
    });
}

// Bắt sự kiện khi bấm "Thêm vào giỏ" ở trang Detail hoặc Product Card
// Lưu ý: Cần sửa nút submit form thành button thường và gọi hàm này
function ajaxAddToCart(formId) {
    var form = $(formId);
    $.post(form.attr('action'), form.serialize(), function (res) {
        // Sau khi thêm thành công -> Mở Mini Cart
        openMiniCart();
    }).fail(function () {
        alert('Vui lòng đăng nhập để mua hàng!');
        window.location.href = '/Identity/Account/Login';
    });
    return false; // Chặn submit form thường
}