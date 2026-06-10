/**
 * Custom Dropdown Component
 * Replaces native <select> with a styled dropdown.
 *
 * Usage:
 *   Add class "custom-select" to any <select> element.
 *   Add data-variant="dark" or data-variant="light" for theme.
 *   The script auto-initializes on DOMContentLoaded.
 */

(function () {
    function createDropdown(selectEl) {
        var variant = selectEl.getAttribute('data-variant') || 'dark';
        var options = selectEl.querySelectorAll('option');

        // Hide original select
        selectEl.style.display = 'none';

        // Create wrapper
        var wrapper = document.createElement('div');
        wrapper.className = 'custom-dropdown custom-dropdown--' + variant;

        // Create trigger
        var trigger = document.createElement('div');
        trigger.className = 'custom-dropdown-trigger';
        trigger.setAttribute('tabindex', '0');

        var triggerText = document.createElement('span');
        triggerText.className = 'dd-text';

        var arrow = document.createElement('i');
        arrow.className = 'fas fa-chevron-down dd-arrow';

        trigger.appendChild(triggerText);
        trigger.appendChild(arrow);
        wrapper.appendChild(trigger);

        // Create options panel
        var optionsPanel = document.createElement('div');
        optionsPanel.className = 'custom-dropdown-options';

        var optionsInner = document.createElement('div');
        optionsInner.className = 'custom-dropdown-options-inner';

        options.forEach(function (opt) {
            var optDiv = document.createElement('div');
            optDiv.className = 'custom-dropdown-option';
            optDiv.setAttribute('data-value', opt.value);

            var check = document.createElement('i');
            check.className = 'fas fa-check dd-check';

            var label = document.createElement('span');
            label.textContent = opt.textContent;

            optDiv.appendChild(check);
            optDiv.appendChild(label);

            // Mark selected
            if (opt.selected && opt.value !== '') {
                optDiv.classList.add('selected');
                triggerText.textContent = opt.textContent;
                triggerText.classList.remove('dd-placeholder');
            }

            optDiv.addEventListener('click', function () {
                // Update native select
                selectEl.value = opt.value;

                // Fire change event
                var event = new Event('change', { bubbles: true });
                selectEl.dispatchEvent(event);

                // Update UI
                optionsInner.querySelectorAll('.custom-dropdown-option').forEach(function (o) {
                    o.classList.remove('selected');
                });
                optDiv.classList.add('selected');

                if (opt.value === '') {
                    triggerText.textContent = opt.textContent;
                    triggerText.classList.add('dd-placeholder');
                } else {
                    triggerText.textContent = opt.textContent;
                    triggerText.classList.remove('dd-placeholder');
                }

                // Close dropdown
                wrapper.classList.remove('open');
            });

            optionsInner.appendChild(optDiv);
        });

        optionsPanel.appendChild(optionsInner);
        wrapper.appendChild(optionsPanel);

        // Set initial text
        var selectedOpt = selectEl.options[selectEl.selectedIndex];
        if (selectedOpt) {
            triggerText.textContent = selectedOpt.textContent;
            if (selectedOpt.value === '') {
                triggerText.classList.add('dd-placeholder');
            }
        }

        // Insert after select
        selectEl.parentNode.insertBefore(wrapper, selectEl.nextSibling);

        // Toggle open/close
        trigger.addEventListener('click', function (e) {
            e.stopPropagation();

            // Close all other dropdowns
            document.querySelectorAll('.custom-dropdown.open').forEach(function (d) {
                if (d !== wrapper) d.classList.remove('open');
            });

            wrapper.classList.toggle('open');

            // Scroll selected option into view
            if (wrapper.classList.contains('open')) {
                var sel = optionsInner.querySelector('.selected');
                if (sel) {
                    sel.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
                }
            }
        });

        // Keyboard support
        trigger.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                trigger.click();
            } else if (e.key === 'Escape') {
                wrapper.classList.remove('open');
            }
        });

        // Expose a method to programmatically update the display
        wrapper._updateDisplay = function () {
            var idx = selectEl.selectedIndex;
            var opt = selectEl.options[idx];
            if (opt) {
                triggerText.textContent = opt.textContent;
                triggerText.classList.toggle('dd-placeholder', opt.value === '');

                optionsInner.querySelectorAll('.custom-dropdown-option').forEach(function (o) {
                    o.classList.toggle('selected', o.getAttribute('data-value') === opt.value);
                });
            }
        };

        return wrapper;
    }

    // Close on outside click
    document.addEventListener('click', function () {
        document.querySelectorAll('.custom-dropdown.open').forEach(function (d) {
            d.classList.remove('open');
        });
    });

    // Auto-initialize
    function initDropdowns() {
        document.querySelectorAll('select.custom-select').forEach(function (sel) {
            if (!sel._customDropdownInit) {
                createDropdown(sel);
                sel._customDropdownInit = true;
            }
        });
    }

    // Run on ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDropdowns);
    } else {
        initDropdowns();
    }

    // Expose globally for dynamic re-init
    window.initCustomDropdowns = initDropdowns;
})();
