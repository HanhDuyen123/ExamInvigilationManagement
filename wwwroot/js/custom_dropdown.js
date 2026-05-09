var AppDropdown = (function () {

    function init(options) {

        let $input = $(options.input);
        let $dropdown = $(options.dropdown);
        let $hidden = $(options.hidden);

        let timeout;
        let index = -1;
        let isOpen = false;

        // ================= LOAD =================
        function load(keyword = "") {

            $.get(options.api, { keyword: keyword.trim() }, function (data) {

                let html = "";

                if (!data.length) {
                    html = `<div class="dropdown-item text-muted">No data</div>`;
                } else {
                    data.forEach((x, i) => {

                        let id = x[options.valueField];
                        let name = x[options.textField];

                        let active = "";

                        if (options.selectedId && id == options.selectedId) {
                            active = "active";
                            index = i;
                        }

                        html += `<div class="dropdown-item ${active}"
                                        data-id="${id}"
                                        data-name="${name}">
                                    ${name}
                                 </div>`;
                    });
                }

                $dropdown.html(html);

                if (isOpen) $dropdown.show();
            });
        }

        // ================= OPEN/CLOSE =================
        function openAll() {
            isOpen = true;
            load("");
            $dropdown.show();
        }

        function close() {
            isOpen = false;
            $dropdown.hide();
            index = -1;
        }

        // ================= MOVE =================
        function move(step) {

            let items = $dropdown.find('.dropdown-item');
            if (!items.length) return;

            index += step;

            if (index < 0) index = items.length - 1;
            if (index >= items.length) index = 0;

            items.removeClass('active');

            let current = items.eq(index);
            current.addClass('active');

            $input.val(current.data('name'));
            $hidden.val(current.data('id'));

            current[0].scrollIntoView({ block: "nearest" });
        }

        // ================= SELECT =================
        function select() {

            let item = $dropdown.find('.active');
            if (!item.length) return;

            $input.val(item.data('name'));
            $hidden.val(item.data('id'));

            close();

            if (options.onChange) {
                options.onChange(item.data('id'));
            }
        }

        // ================= INIT VALUE (EDIT) =================
        if (options.selectedText) {
            $input.val(options.selectedText);
        }

        // ================= EVENTS =================

        $input.on('keydown', function (e) {

            if (e.key === "Enter") {
                e.preventDefault();
                select();
                return;
            }

            if (e.key === "ArrowDown") {
                e.preventDefault();
                if (!isOpen) return openAll();
                move(1);
            }

            if (e.key === "ArrowUp") {
                e.preventDefault();
                if (!isOpen) return openAll();
                move(-1);
            }
        });

        $input.on('keyup', function (e) {

            if (["Enter", "ArrowUp", "ArrowDown"].includes(e.key)) return;

            let keyword = $input.val();

            clearTimeout(timeout);
            timeout = setTimeout(() => {

                if (keyword.length > 0) {
                    isOpen = true;
                    load(keyword);
                    $dropdown.show();
                } else {
                    close();
                }

            }, 300);
        });

        $input.on('input', function () {
            if (!$input.val().trim()) {
                $hidden.val("");
                index = -1;
                $dropdown.find('.dropdown-item').removeClass('active');

                if (options.onChange) {
                    options.onChange("");
                }
            }
        });

        $(document).on('click', options.dropdown + ' .dropdown-item', function () {

            $input.val($(this).data('name'));
            $hidden.val($(this).data('id'));

            close();

            if (options.onChange) {
                options.onChange($(this).data('id'));
            }
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest(options.input + ',' + options.dropdown).length) {
                close();
            }
        });

    }

    return { init };

})();