var CrudPage = (function () {
    let config = {};
    let deleteId = null;
    let deleteModal = null;
    let currentPage = 1;
    let restoring = false;

    function init(options) {
        config = $.extend({
            api: '',
            tableId: '#crudTable',
            filterSelector: '.crud-filter',
            pageSize: null,
            deleteUrl: '',
            deleteName: '#deleteName',
            deleteModalId: '#deleteModal',
            extraData: null,
            autoLoad: true
        }, options || {});

        restoreState();
        normalizePageSizeSelect();
        bindEvents();
        ensureClearFiltersButton();

        if (config.autoLoad) {
            loadData(currentPage || 1);
        }
    }

    function stateKey() {
        const path = window.location.pathname.toLowerCase();
        const api = (config.api || '').toLowerCase();
        return 'crud-list-state:' + path + ':' + api;
    }

    function getStateControls() {
        const selectors = [];

        if (config.filterSelector) selectors.push(config.filterSelector);
        if (config.pageSize) selectors.push(config.pageSize);

        const controls = selectors.length
            ? $(selectors.join(',')).filter('input,select,textarea')
            : $();

        const extraControls = $('input[type="hidden"][id], .lookup-modern input[id]').filter('input,select,textarea');
        return controls.add(extraControls).filter('[id]').toArray();
    }

    function readControlValue(el) {
        if (el.type === 'checkbox') return el.checked;
        if (el.type === 'radio') return el.checked ? el.value : null;
        return $(el).val();
    }

    function writeControlValue(el, value) {
        if (value === undefined || value === null) return;
        if (el.type === 'checkbox') {
            el.checked = value === true || value === 'true';
            return;
        }
        if (el.type === 'radio') {
            el.checked = el.value === value;
            return;
        }
        $(el).val(value);
    }

    function saveState(page) {
        if (restoring) return;

        const filters = {};
        getStateControls().forEach(function (el) {
            filters[el.id] = readControlValue(el);
        });

        localStorage.setItem(stateKey(), JSON.stringify({
            page: page || currentPage || 1,
            filters: filters,
            savedAt: Date.now()
        }));
    }

    function restoreState() {
        const raw = localStorage.getItem(stateKey());
        if (!raw) return;

        let state;
        try { state = JSON.parse(raw); } catch { return; }
        if (!state || !state.filters) return;

        restoring = true;
        getStateControls().forEach(function (el) {
            if (Object.prototype.hasOwnProperty.call(state.filters, el.id)) {
                writeControlValue(el, state.filters[el.id]);
            }
        });
        currentPage = Number(state.page || 1) || 1;
        restoring = false;
    }

    function clearState() {
        localStorage.removeItem(stateKey());
    }

    function getExtraData() {
        if (typeof config.extraData === 'function') {
            const data = config.extraData();
            return data && typeof data === 'object' ? data : {};
        }

        return config.extraData && typeof config.extraData === 'object'
            ? config.extraData
            : {};
    }

    function loadData(page = 1) {
        currentPage = Number(page || 1) || 1;
        const data = {
            page: currentPage,
            pageSize: config.pageSize ? $(config.pageSize).val() : 5
        };

        if (config.searchBox) {
            data.keyword = $(config.searchBox).val();
        }

        $.extend(data, getExtraData());
        saveState(currentPage);

        $(config.tableId).fadeTo(100, 0.4);

        $.get(config.api, data, function (res) {
            $(config.tableId).html(res);
            if (window.AppEnhanceActionButtons) window.AppEnhanceActionButtons();
            $(config.tableId).fadeTo(100, 1);
        });
    }

    function bindEvents() {
        let timeout;

        function triggerLoad() {
            clearTimeout(timeout);
            timeout = setTimeout(() => loadData(1), 300);
        }

        // bind cho tất cả input có class chung
        $(document)
            .off('input.crudFilter change.crudFilter keyup.crudFilter', config.filterSelector)
            .on('input.crudFilter change.crudFilter keyup.crudFilter', config.filterSelector, triggerLoad);

        if (config.pageSize) {
            $(config.pageSize).off('.crud').on('change.crud', function () {
                loadData(1);
            });
        }

        $(document)
            .off('click.crudPage', '.pagination a[data-page]')
            .on('click.crudPage', '.pagination a[data-page]', function (e) {
                e.preventDefault();
                const $item = $(this).closest('.page-item');
                if ($item.hasClass('disabled') || $item.hasClass('active')) return;
                loadData(Number($(this).data('page')) || 1);
            });

        $(document)
            .off('click.crudClearFilters', '.crud-clear-filters')
            .on('click.crudClearFilters', '.crud-clear-filters', function (e) {
                e.preventDefault();
                clearState();
                const controls = getStateControls();
                controls.forEach(function (el) {
                    if (config.pageSize && ('#' + el.id) === config.pageSize) return;
                    if (el.type === 'checkbox' || el.type === 'radio') el.checked = false;
                    else $(el).val('');
                });
                loadData(1);
            });

        if (config.deleteUrl) {
            const deleteModalEl = document.querySelector(config.deleteModalId);
            if (deleteModalEl) {
                deleteModal = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
            }

            $(document).off('click.crudDelete').on('click.crudDelete', '.btn-delete', function (e) {
                e.preventDefault();

                deleteId = $(this).data('id');
                const name = $(this).data('name');

                if (config.deleteName) {
                    $(config.deleteName).text(name || '');
                }

                if (deleteModal) {
                    deleteModal.show();
                }
            });

            $('#confirmDelete').off('.crudDelete').on('click.crudDelete', function () {
                if (!deleteId) return;

                const token = $('meta[name="request-verification-token"]').attr('content') || '';

                const form = $('<form>', {
                    method: 'post',
                    action: config.deleteUrl + '/' + deleteId
                });

                form.append($('<input>', {
                    type: 'hidden',
                    name: '__RequestVerificationToken',
                    value: token
                }));

                $('body').append(form);
                form.trigger('submit');
            });
        }
    }

    function normalizePageSizeSelect() {
        if (!config.pageSize) return;

        const $select = $(config.pageSize);
        if (!$select.length) return;

        const allowed = [5, 10, 20, 50, 100];
        let current = Number($select.val() || 5);
        if (!allowed.includes(current)) current = 5;

        $select.empty();
        allowed.forEach(function (size) {
            $select.append($('<option>', {
                value: size,
                text: size + '/trang',
                selected: size === current
            }));
        });
    }

    function ensureClearFiltersButton() {
        const $filterBox = $(config.filterSelector).closest('.filter-glass').first();
        if (!$filterBox.length) return;

        const hasClearAction = $filterBox.find('.crud-clear-filters').length ||
            $filterBox.find('a,button').filter(function () {
                return ($(this).text() || '').trim().toLowerCase().includes('xóa lọc');
            }).length;

        if (hasClearAction) return;

        const count = $filterBox.find('input,select,textarea')
            .filter(function () {
                const id = this.id || '';
                if (!id) return false;
                if (config.pageSize && ('#' + id) === config.pageSize) return false;
                if (this.type === 'hidden') return false;
                return true;
            }).length;

        if (count < 2) return;

        const $btn = $('<button type="button" class="btn btn-sm btn-outline-secondary crud-clear-filters"><i class="bi bi-x-circle"></i> Xóa lọc</button>');
        const $head = $filterBox.find('.filter-head').first();
        const $tools = $filterBox.find('.role-filter-tools, .exam-search-tools').first();
        if ($tools.length) $tools.append($btn);
        else if ($head.length) $head.append($btn);
        else $filterBox.prepend($('<div class="filter-actions-row"></div>').append($btn));
    }

    return {
        init: init,
        loadData: loadData,
        clearState: clearState
    };
})();
