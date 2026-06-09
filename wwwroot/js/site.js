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

(function () {
    const defaultMessage = 'Hệ thống đang thực hiện, vui lòng chờ trong giây lát.';

    function getOverlay() {
        return document.getElementById('appLoadingOverlay');
    }

    function setMessage(message) {
        const target = document.getElementById('appLoadingMessage');
        if (target) target.textContent = message || defaultMessage;
    }

    function show(message) {
        const overlay = getOverlay();
        if (!overlay) return;

        setMessage(message);
        overlay.classList.add('show');
        overlay.setAttribute('aria-hidden', 'false');
        document.body.classList.add('app-loading-active');
    }

    function hide() {
        const overlay = getOverlay();
        if (!overlay) return;

        overlay.classList.remove('show');
        overlay.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('app-loading-active');
    }

    function bindLoadingForms() {
        document.querySelectorAll('form[data-loading-message]').forEach(function (form) {
            if (form.dataset.loadingBound === '1') return;
            form.dataset.loadingBound = '1';

            form.addEventListener('submit', function (event) {
                if (form.dataset.submitting === '1') return;

                const validator = window.jQuery && window.jQuery.fn && window.jQuery.fn.valid
                    ? window.jQuery(form).valid()
                    : true;
                if (!validator) return;

                const submitter = event.submitter || form.querySelector('[type="submit"]');
                window.setTimeout(function () {
                    if (event.defaultPrevented || form.dataset.submitting === '1') return;

                    form.dataset.submitting = '1';
                    if (submitter) {
                        submitter.disabled = true;
                        submitter.classList.add('disabled');
                    }
                    show(form.getAttribute('data-loading-message'));
                }, 0);
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindLoadingForms);
    } else {
        bindLoadingForms();
    }

    window.AppLoading = {
        show,
        hide,
        bind: bindLoadingForms
    };
})();
