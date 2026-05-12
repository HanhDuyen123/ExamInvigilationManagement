// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    function enhanceActionButtons() {
        document.querySelectorAll('.btn-icon').forEach(function (button) {
            if (!button.getAttribute('title')) {
                if (button.classList.contains('edit')) button.setAttribute('title', 'Cập nhật');
                else if (button.classList.contains('delete') || button.classList.contains('btn-delete')) button.setAttribute('title', 'Xóa');
                else if (button.classList.contains('detail')) button.setAttribute('title', 'Xem chi tiết');
            }

            if (!button.getAttribute('aria-label') && button.getAttribute('title')) {
                button.setAttribute('aria-label', button.getAttribute('title'));
            }
        });

        document.querySelectorAll('.btn-back').forEach(function (button) {
            if ((button.textContent || '').trim().toLowerCase() === 'back') {
                button.innerHTML = '<i class="bi bi-arrow-left"></i> Quay lại';
            }
            if (!button.getAttribute('title')) button.setAttribute('title', 'Quay lại');
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceActionButtons);
    } else {
        enhanceActionButtons();
    }

    window.AppEnhanceActionButtons = enhanceActionButtons;
})();
