// wwwroot/js/notification-center.js
(function () {
    const bellUrl = '/Notification/Recent';
    const countUrl = '/Notification/UnreadCount';
    const markReadUrl = '/Notification/MarkAsRead';
    const markAllReadUrl = '/Notification/MarkAllAsRead';
    let suppressRealtimeToastUntil = 0;

    function getStorageKey() {
        const userId = $('#notificationBell').data('user-id') || 'anonymous';
        return `notification:last-seen-id:${userId}`;
    }

    function getLastSeenNotificationId() {
        const raw = localStorage.getItem(getStorageKey());
        const value = Number(raw);
        return Number.isFinite(value) && value > 0 ? value : 0;
    }

    function setLastSeenNotificationId(id) {
        const value = Math.max(0, Number(id) || 0);
        if (value > 0) {
            localStorage.setItem(getStorageKey(), String(value));
        }
    }

    function getLatestDropdownNotificationId() {
        const ids = $('#notificationDropdownBody .notification-item')
            .map(function () { return Number($(this).data('id')) || 0; })
            .get();

        return ids.length ? Math.max(...ids) : 0;
    }

    function setBadgeCount(count) {
        const normalized = Math.max(0, Number(count) || 0);
        const $badge = $('#notificationBadge');

        if (normalized > 0) {
            $badge.text(normalized).removeClass('d-none');
        } else {
            $badge.text('0').addClass('d-none');
        }
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val()
            || $('meta[name="request-verification-token"]').attr('content')
            || '';
    }

    async function refreshBell(options) {
        const forceZeroBadge = options?.forceZeroBadge === true;
        const lastSeenId = getLastSeenNotificationId();
        const unreadCountUrl = lastSeenId > 0
            ? `${countUrl}?afterId=${encodeURIComponent(lastSeenId)}`
            : countUrl;

        try {
            const [html, countRes] = await Promise.all([
                fetch(bellUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.text()),
                fetch(unreadCountUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.json())
            ]);

            $('#notificationDropdownBody').html(html);

            const count = countRes?.unreadCount || 0;
            setBadgeCount(forceZeroBadge ? 0 : count);
        } catch (e) {
            console.error('Refresh notification failed', e);
        }
    }

    async function acknowledgeBellOpened() {
        setBadgeCount(0);
        await refreshBell({ forceZeroBadge: true });

        const latestId = getLatestDropdownNotificationId();
        if (latestId > 0) {
            setLastSeenNotificationId(latestId);
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

            await refreshBell({ forceZeroBadge: true });
            const latestId = getLatestDropdownNotificationId();
            if (latestId > 0) {
                setLastSeenNotificationId(latestId);
            }

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

            connection.on('notification:changed', function (payload) {
                const changeKind = payload?.changeKind || 'changed';
                const isNewNotification = changeKind === 'created' || changeKind === 'changed';

                refreshBell();
                if (isNewNotification && Date.now() > suppressRealtimeToastUntil) {
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
                .then(function () { return refreshBell(); })
                .catch(console.error);
        }

        $(document).on('click', '.read-btn', function () {
            const id = $(this).data('id');
            if (id) markRead(id);
        });

        $(document).on('click', '.mark-all-read-btn', function (event) {
            event.preventDefault();
            event.stopPropagation();
            markAllRead();
        });

        $('#notificationBell').on('shown.bs.dropdown', function () {
            acknowledgeBellOpened();
        });
    });
})();
