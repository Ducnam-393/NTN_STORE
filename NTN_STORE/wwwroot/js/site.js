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
// Hàm xóa item từ Mini Cart (AJAX)
function removeFromMiniCart(id) {
     var btn = event.currentTarget;
    $(btn).closest('li').css('opacity', '0.5');
    $.post('/Cart/RemoveFromMiniCart', { id: id }, function (res) {
        if (res.success) {
            // Reload lại nội dung cart sau khi xóa thành công
            $.get('/Cart/GetMiniCart', function (html) {
                $('#miniCartContent').html(html);
                
                 updateCartBadge(); 
            });
        } else {
            alert('Có lỗi xảy ra khi xóa sản phẩm.');
            $(btn).closest('li').css('opacity', '1'); // Hồi phục nếu lỗi
        }
    });
}

// Hàm đóng Mini Cart
function closeMiniCart() {
    $('.offcanvas-minicart').removeClass('open');
}
function ajaxAddWishlist(e, id, element) {
    // Hiệu ứng click (nảy nhẹ)
    $(element).find('i').addClass('animate__animated animate__heartBeat');

    $.post('/Wishlist/AddAjax', { id: id }, function (res) {
        if (res.success) {
            // 1. Đổi icon trái tim rỗng (far) thành đặc (fas) và màu đỏ
            var icon = $('#wish-icon-' + id);
            icon.removeClass('far').addClass('fas text-danger');

            // 2. Cập nhật số lượng trên Header (nếu bạn có span class="badge-counter" cho wishlist)
            // Giả sử icon wishlist trên header có id="wishlist-badge-count"
            $('.wishlist-badge-count').text(res.count);
            $('.wishlist-badge-count').show(); // Hiện nếu đang ẩn

            // 3. Thông báo nhỏ (Toast hoặc Alert) - Tùy chọn
            // alert(res.message); 
        } else {
            if (res.requireLogin) {
                // Nếu chưa đăng nhập -> chuyển sang trang login
                window.location.href = '/Identity/Account/Login';
            } else {
                // Đã có rồi -> Thông báo
                alert(res.message);
                // Hoặc đổi màu icon để báo đã có
                $('#wish-icon-' + id).removeClass('far').addClass('fas text-danger');
            }
        }
    });
}