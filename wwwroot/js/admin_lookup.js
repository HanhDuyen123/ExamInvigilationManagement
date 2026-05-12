var AdminLookup = (function () {
    const instances = new Map();

    function create(options) {
        const cfg = $.extend({
            key: null,
            input: null,
            hidden: null,
            menu: null,
            button: null,
            url: '',
            queryParam: 'keyword',
            extraParams: null,
            valueField: 'id',
            textField: 'text',          // string hoặc function(item, index)
            searchTransform: null,      // function(keyword, api) => string | object
            debounce: 300,
            minLength: 0,
            loadOnOpen: true,
            closeOnSelect: true,
            initialValue: '',
            initialText: '',
            onSelect: null,
            onClear: null,
            onOpen: null,
            onClose: null,
            beforeSearch: null,
            blockedMessage: 'Vui lòng chọn dữ liệu liên quan trước'
        }, options || {});

        if (!cfg.key) cfg.key = cfg.input;

        const key = cfg.key;
        const ns = `.adminLookup-${key}`;

        const $input = $(cfg.input);
        const $hidden = $(cfg.hidden);
        const $menu = $(cfg.menu);
        const $button = cfg.button ? $(cfg.button) : $();

        let timer = null;
        let isOpen = false;
        let currentIndex = -1;
        let requestSeq = 0;
        let isInitializing = true;

        function escapeHtml(text) {
            return String(text ?? '')
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function resolveUrl() {
            return typeof cfg.url === 'function' ? cfg.url() : cfg.url;
        }

        function getItemValue(item) {
            return item ? item[cfg.valueField] : null;
        }

        function getItemText(item, index) {
            if (!item) return '';

            if (typeof cfg.textField === 'function') {
                const result = cfg.textField(item, index);
                return result == null ? '' : String(result);
            }

            const result = item[cfg.textField];
            return result == null ? '' : String(result);
        }

        function canSearch(keyword) {
            if (typeof cfg.beforeSearch === 'function') {
                const result = cfg.beforeSearch(keyword, api);
                if (result === false || typeof result === 'string') {
                    showBlockedMessage(typeof result === 'string' ? result : cfg.blockedMessage);
                    return false;
                }
            }
            return true;
        }

        function showBlockedMessage(message) {
            api._currentItems = [];
            currentIndex = -1;
            close();
            const $wrap = $input.closest('.lookup-modern');
            let $message = $wrap.find('.lookup-blocked-message').first();

            if (!$message.length) {
                $message = $('<div class="lookup-blocked-message" role="alert"></div>');
                $wrap.append($message);
            }

            $message.text(message || cfg.blockedMessage).show();
            $input.addClass('lookup-blocked-input');

        }

        function clearBlockedMessage() {
            $input.removeClass('lookup-blocked-input');
            $input.closest('.lookup-modern').find('.lookup-blocked-message').hide();
        }

        function render(items) {
            api._currentItems = Array.isArray(items) ? items : [];

            let html = '';

            if (!api._currentItems.length) {
                html = '<div class="dropdown-item text-muted lookup-empty">Không có dữ liệu</div>';
            } else {
                api._currentItems.forEach((item, idx) => {
                    const value = getItemValue(item);
                    const text = getItemText(item, idx);
                    const active = String(value) === String($hidden.val()) ? 'active' : '';

                    html += `
                        <button type="button"
                                class="dropdown-item lookup-item ${active}"
                                data-index="${idx}">
                            ${escapeHtml(text)}
                        </button>
                    `;
                });
            }

            $menu.html(html);
        }

        function buildParams(keyword) {
            let params = {};
            let q = keyword ?? '';

            if (typeof cfg.searchTransform === 'function') {
                const transformed = cfg.searchTransform(q, api);

                if (transformed && typeof transformed === 'object' && !Array.isArray(transformed)) {
                    params = $.extend({}, transformed);
                    q = '';
                } else {
                    q = transformed ?? '';
                }
            }

            if (typeof cfg.queryParam === 'string') {
                params[cfg.queryParam] = String(q).trim();
            } else {
                params.keyword = String(q).trim();
            }

            if (typeof cfg.extraParams === 'function') {
                $.extend(params, cfg.extraParams() || {});
            } else if (cfg.extraParams && typeof cfg.extraParams === 'object') {
                $.extend(params, cfg.extraParams);
            }

            return params;
        }

        function fetchData(keyword = '') {
            if (!canSearch(keyword)) return;
            clearBlockedMessage();

            const requestUrl = resolveUrl();
            if (!requestUrl) return;

            const seq = ++requestSeq;
            const params = buildParams(keyword);

            $.get(requestUrl, params, function (res) {
                if (seq !== requestSeq) return;

                const items = Array.isArray(res) ? res : (res?.items || res?.data || []);
                render(items);

                if (isOpen) $menu.show();
                currentIndex = -1;
            });
        }

        function open(keyword = '') {
            if (!canSearch(keyword)) return;
            clearBlockedMessage();

            isOpen = true;

            if (typeof cfg.onOpen === 'function') {
                cfg.onOpen(api);
            }

            if (cfg.loadOnOpen) {
                fetchData(keyword);
            } else {
                $menu.show();
            }
        }

        function close() {
            isOpen = false;
            currentIndex = -1;
            $menu.hide();
            $menu.find('.lookup-item').removeClass('active');

            if (typeof cfg.onClose === 'function') {
                cfg.onClose(api);
            }
        }

        function setValue(value, text) {
            $hidden.val(value ?? '');
            $input.val(text ?? '');
            clearBlockedMessage();
        }

        function clear() {
            $hidden.val('');
            $input.val('');
            currentIndex = -1;
            api._currentItems = [];
            $menu.find('.lookup-item').removeClass('active');

            if (typeof cfg.onClear === 'function') {
                cfg.onClear(api);
            }
        }

        function selectItem($item) {
            const index = parseInt($item.data('index'), 10);
            const item = Number.isFinite(index) ? api._currentItems[index] : null;

            const value = getItemValue(item);
            const text = getItemText(item, index);

            setValue(value, text);

            if (typeof cfg.onSelect === 'function') {
                cfg.onSelect({
                    value: value,
                    text: text,
                    item: item,
                    input: $input,
                    hidden: $hidden,
                    api: api
                });
            }

            if (cfg.closeOnSelect) {
                close();
            }
        }

        function move(step) {
            const $items = $menu.find('.lookup-item');
            if (!$items.length) return;

            currentIndex += step;

            if (currentIndex < 0) currentIndex = $items.length - 1;
            if (currentIndex >= $items.length) currentIndex = 0;

            $items.removeClass('active');
            const $current = $items.eq(currentIndex);

            $current.addClass('active');

            const item = api._currentItems[currentIndex];
            setValue(getItemValue(item), getItemText(item, currentIndex));

            if ($current[0] && $current[0].scrollIntoView) {
                $current[0].scrollIntoView({ block: 'nearest' });
            }
        }

        function bindEvents() {
            $input.off(ns);
            $menu.off(ns);
            $(document).off(ns);
            if ($button.length) $button.off(ns);

            $input.on(`keydown${ns}`, function (e) {
                const keyword = $input.val();

                if (e.key === 'Enter') {
                    e.preventDefault();
                    const $active = $menu.find('.lookup-item.active');
                    if ($active.length) selectItem($active);
                    return;
                }

                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    if (!isOpen) open(keyword);
                    else move(1);
                    return;
                }

                if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (!isOpen) open(keyword);
                    else move(-1);
                    return;
                }
            });

            $input.on(`keyup${ns}`, function (e) {
                if (['Enter', 'ArrowDown', 'ArrowUp'].includes(e.key)) return;

                clearTimeout(timer);
                timer = setTimeout(() => {
                    const keyword = $input.val();
                    if (keyword.length >= cfg.minLength) {
                        open(keyword);
                    } else {
                        close();
                    }
                }, cfg.debounce);
            });

            $input.on(`input${ns}`, function () {
                clearBlockedMessage();
                if (isInitializing) return;

                if (!$input.val().trim()) {
                    clear();
                }
            });

            $input.on(`blur${ns}`, function () {
                setTimeout(clearBlockedMessage, 120);
            });

            $menu.on(`click${ns}`, '.lookup-item', function () {
                selectItem($(this));
            });

            if ($button.length) {
                $button.on(`click${ns}`, function () {
                    open($input.val());
                    $input.focus();
                });
            }

            $(document).on(`click${ns}`, function (e) {
                const selectors = [cfg.input, cfg.hidden, cfg.menu, cfg.button].filter(Boolean).join(', ');
                if (!$(e.target).closest(selectors).length) {
                    close();
                }
            });
        }

        const api = {
            open: open,
            close: close,
            reload: fetchData,
            clear: clear,
            setValue: setValue,
            _currentItems: [],
            getValue: function () {
                return {
                    value: $hidden.val(),
                    text: $input.val()
                };
            }
        };

        instances.set(key, api);

        if (cfg.initialValue !== undefined && cfg.initialValue !== null && cfg.initialValue !== '') {
            $hidden.val(cfg.initialValue);
        }

        if (cfg.initialText !== undefined && cfg.initialText !== null && cfg.initialText !== '') {
            $input.val(cfg.initialText);
        }

        bindEvents();

        setTimeout(() => {
            isInitializing = false;
        }, 0);

        return api;
    }

    function get(key) {
        return instances.get(key);
    }

    function require(selector, message) {
        return $(selector).val() ? true : message;
    }

    return {
        create: create,
        get: get,
        require: require
    };
})();
