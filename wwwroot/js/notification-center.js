// wwwroot/js/notification-center.js
(function () {
    const bellUrl = '/Notification/Recent';
    const countUrl = '/Notification/UnreadCount';
    const markReadUrl = '/Notification/MarkAsRead';
    const markAllReadUrl = '/Notification/MarkAllAsRead';
    let suppressRealtimeToastUntil = 0;

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val()
            || $('meta[name="request-verification-token"]').attr('content')
            || '';
    }

    async function refreshBell() {
        try {
            const [html, countRes] = await Promise.all([
                fetch(bellUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.text()),
                fetch(countUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.json())
            ]);

            $('#notificationDropdownBody').html(html);

            const count = countRes?.unreadCount || 0;
            const $badge = $('#notificationBadge');

            if (count > 0) {
                $badge.text(count).removeClass('d-none');
            } else {
                $badge.addClass('d-none');
            }
        } catch (e) {
            console.error('Refresh notification failed', e);
        }
    }

    function showRealtimeToast() {
        const toast = document.createElement('a');
        toast.href = '/Notification';
        toast.className = 'notification-live-toast';
        toast.innerHTML = '<i class="bi bi-bell-fill"></i><span>Bạn có thông báo mới</span>';
        document.body.appendChild(toast);

        setTimeout(function () {
            toast.remove();
        }, 4500);
    }

    async function markRead(id) {
        try {
            suppressRealtimeToastUntil = Date.now() + 1500;
            await fetch(markReadUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ id })
            });

            await refreshBell();

            if (window.CrudPage && typeof CrudPage.loadData === 'function') {
                CrudPage.loadData(1);
            }
        } catch (e) {
            console.error('Mark read failed', e);
        }
    }

    async function markAllRead() {
        try {
            suppressRealtimeToastUntil = Date.now() + 1500;
            await fetch(markAllReadUrl, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken()
                }
            });

            await refreshBell();

            if (window.CrudPage && typeof CrudPage.loadData === 'function') {
                CrudPage.loadData(1);
            }
        } catch (e) {
            console.error('Mark all read failed', e);
        }
    }

    window.NotificationCenter = {
        refreshBell,
        markRead,
        markAllRead
    };

    $(function () {
        refreshBell();

        if (typeof signalR !== 'undefined') {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notifications')
                .withAutomaticReconnect()
                .build();

            connection.on('notification:changed', function () {
                refreshBell();
                if (Date.now() > suppressRealtimeToastUntil) {
                    showRealtimeToast();
                }

                if ($('#notificationTableWrap').length && window.CrudPage && typeof CrudPage.loadData === 'function') {
                    CrudPage.loadData(1);
                }
            });

            connection.onreconnected(function () {
                refreshBell();
            });

            connection.start()
                .then(refreshBell)
                .catch(console.error);
        }

        $(document).on('click', '.read-btn', function () {
            const id = $(this).data('id');
            if (id) markRead(id);
        });

        $(document).on('click', '#markAllReadBtn', function () {
            markAllRead();
        });
    });
})();
