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
// Hàm thêm vào giỏ hàng bằng AJAX
function ajaxAddToCart(formElement) {
    // 1. Chặn hành động chuyển trang mặc định
    event.preventDefault();

    var form = $(formElement);
    var url = form.attr('action');
    var data = form.serialize(); // Lấy dữ liệu trong form (ProductId, Quantity...)

    // 2. Gửi dữ liệu ngầm lên Server
    $.post(url, data, function (response) {

        // 3. Xử lý kết quả trả về
        if (response.success) {
            // THÀNH CÔNG: Hiện thông báo đẹp góc trên
            Swal.fire({
                icon: 'success',
                title: 'Thành công',
                text: response.message,
                showConfirmButton: false,
                timer: 1500,
                toast: true,
                position: 'top-end'
            });

            // Cập nhật lại số lượng trên icon giỏ hàng (nếu có hàm này)
            updateCartBadge();
        }
        else {
            // THẤT BẠI (Hết hàng / Quá số lượng): Hiện Popup báo lỗi giữa màn hình
            Swal.fire({
                icon: 'error',
                title: 'Không thể thêm vào giỏ',
                text: response.message, // Thông báo từ Controller: "Chỉ còn 5 sản phẩm..."
                confirmButtonColor: '#f58220',
                confirmButtonText: 'Đã hiểu'
            });
        }
    }).fail(function () {
        // Lỗi mạng hoặc chưa đăng nhập
        Swal.fire({
            icon: 'warning',
            title: 'Yêu cầu đăng nhập',
            text: 'Vui lòng đăng nhập để mua hàng!',
            showCancelButton: true,
            confirmButtonText: 'Đăng nhập',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#f58220'
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = '/Identity/Account/Login';
            }
        });
    });

    return false;
}

// Hàm cập nhật badge giỏ hàng (Optional)
function updateCartBadge() {
    $.get('/Cart/GetMiniCart', function (html) {
    });
}
$(document).ready(function () {
    // 1. Gắn sự kiện click cho icon giỏ hàng
    // Giả sử icon giỏ hàng của bạn có class là ".btn-cart-trigger" hoặc thẻ <a>
    $(document).on('click', '.btn-cart-trigger', function (e) {
        e.preventDefault(); // Chặn chuyển trang nếu là thẻ a
        openMiniCart();
    });

    // 2. Gắn sự kiện click cho nút đóng (dấu X) và màn nền đen (overlay)
    $(document).on('click', '.btn-close-cart, .offcanvas-overlay', function () {
        closeMiniCart();
    });
});

// Sửa lại hàm updateCartBadge (Code của bạn đang bị thiếu logic cập nhật số)
function updateCartBadge() {
    $.get('/Cart/GetCount', function (count) { // Bạn cần có Action GetCount trả về số lượng
        $('.cart-count-badge').text(count);
    });
    // Hoặc nếu muốn đơn giản thì reload lại mini cart
    // $.get('/Cart/GetMiniCart', function (html) { ... });
}
$(document).ready(function () {
    // 1. Gọi hàm load ngay khi trang web chạy xong
    loadHoverCart();

    // 2. Khi di chuột vào (đề phòng trường hợp chưa load kịp)
    $('.cart-hover-container').on('mouseenter', function () {
        if ($('#headerMiniCart .cart-dropdown-content').children().length === 0) {
            loadHoverCart();
        }
    });
});

// Hàm lấy dữ liệu từ server
function loadHoverCart() {
    // Gọi Action GetMiniCart trong CartController
    $.get('/Cart/GetMiniCart', function (html) {
        // Nhét HTML nhận được vào khung dropdown
        $('#headerMiniCart .cart-dropdown-content').html(html);
    });
}

