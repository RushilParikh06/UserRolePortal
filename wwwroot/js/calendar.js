(function() {
    // Inject styles for the calendar
    const style = document.createElement('style');
    style.innerHTML = `
        .cal-container {
            position: absolute;
            z-index: 9999;
            width: 320px;
            background: #000000;
            border: 1px solid rgba(255, 91, 0, 0.25);
            box-shadow: 0 15px 45px rgba(0, 0, 0, 0.9), 0 0 12px rgba(255, 91, 0, 0.08);
            border-radius: 12px;
            font-family: 'Inter', sans-serif;
            padding: 16px;
            box-sizing: border-box;
            opacity: 0;
            transform: translateY(10px);
            transition: opacity 0.2s ease, transform 0.2s ease;
            pointer-events: none;
            display: none;
            color: #ffffff;
        }
        .cal-container.cal-visible {
            opacity: 1;
            transform: translateY(0);
            pointer-events: auto;
            display: block;
        }
        
        .cal-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 16px;
        }
        
        .cal-nav-btn {
            background: transparent;
            border: 1px solid rgba(255, 255, 255, 0.2);
            color: #ffffff;
            width: 32px;
            height: 32px;
            border-radius: 50%;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: border-color 0.2s ease, color 0.2s ease;
        }
        .cal-nav-btn:hover {
            border-color: #ff5b00;
            color: #ff5b00;
        }
        
        .cal-title-group {
            display: flex;
            gap: 8px;
        }
        
        .cal-title-btn {
            background: transparent;
            border: none;
            color: #ffffff;
            font-size: 16px;
            font-weight: 700;
            cursor: pointer;
            padding: 4px 8px;
            border-radius: 4px;
            font-family: 'Inter', sans-serif;
            transition: color 0.2s ease, background 0.2s ease;
        }
        .cal-title-btn:hover {
            color: #ff5b00;
            background: rgba(255, 91, 0, 0.1);
        }
        
        .cal-weekdays {
            display: grid;
            grid-template-columns: repeat(7, 1fr);
            text-align: center;
            margin-bottom: 8px;
        }
        .cal-weekday {
            color: rgba(255, 255, 255, 0.45);
            font-size: 12px;
            font-weight: 600;
        }
        
        .cal-days {
            display: grid;
            grid-template-columns: repeat(7, 1fr);
            gap: 4px;
        }
        
        .cal-day {
            height: 38px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 8px;
            color: rgba(255, 255, 255, 0.85);
            font-size: 14px;
            cursor: pointer;
            border: 1px solid transparent;
            background: transparent;
            transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
        }
        
        .cal-day:not(.cal-disabled):hover {
            background: rgba(255, 91, 0, 0.15);
            color: #ff5b00;
            border-color: rgba(255, 91, 0, 0.25);
        }
        
        .cal-day.cal-other-month {
            color: rgba(255, 255, 255, 0.2);
        }
        
        .cal-day.cal-today {
            border-color: #ff5b00;
            color: #ff5b00;
        }
        
        .cal-day.cal-selected {
            background: #ff5b00;
            color: #000000;
            font-weight: 700;
            border-color: #ff5b00;
        }
        
        .cal-day.cal-disabled {
            color: rgba(255, 255, 255, 0.1);
            pointer-events: none;
        }

        /* Month / Year picker grids */
        .cal-grid-view {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 8px;
        }
        
        .cal-grid-item {
            padding: 12px 4px;
            text-align: center;
            background: transparent;
            border: 1px solid transparent;
            color: #ffffff;
            border-radius: 8px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s ease;
        }
        .cal-grid-item:hover {
            background: rgba(255, 91, 0, 0.15);
            color: #ff5b00;
            border-color: rgba(255, 91, 0, 0.25);
        }
        .cal-grid-item.cal-selected {
            background: #ff5b00;
            color: #000000;
            font-weight: 700;
        }

        .cal-view-hidden {
            display: none !important;
        }
    `;
    document.head.appendChild(style);

    const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    const WEEKDAYS = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

    window.initCalendar = function(inputSelector, options = {}) {
        console.log("initCalendar called with", inputSelector);
        const input = document.querySelector(inputSelector);
        if (!input) {
            console.error("calendar target not found:", inputSelector);
            return;
        }

        let currentDate = new Date();
        let selectedDate = null;
        if (input.value) {
            const parsed = new Date(input.value);
            if (!isNaN(parsed.getTime())) {
                selectedDate = new Date(parsed);
                currentDate = new Date(parsed);
            }
        }

        let maxDate = null;
        if (options.maxDate === 'today') {
            maxDate = new Date();
            maxDate.setHours(0, 0, 0, 0);
        } else if (options.maxDate) {
            maxDate = new Date(options.maxDate);
        }

        let currentView = 'days'; // 'days', 'months', 'years'
        let currentYearBlockStart = currentDate.getFullYear() - 5;

        const container = document.createElement('div');
        container.className = 'cal-container';
        document.body.appendChild(container);

        // Build HTML structure
        container.innerHTML = `
            <div class="cal-header">
                <button type="button" class="cal-nav-btn cal-prev">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 18l-6-6 6-6"/></svg>
                </button>
                <div class="cal-title-group">
                    <button type="button" class="cal-title-btn cal-month-btn"></button>
                    <button type="button" class="cal-title-btn cal-year-btn"></button>
                </div>
                <button type="button" class="cal-nav-btn cal-next">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 18l6-6-6-6"/></svg>
                </button>
            </div>
            
            <div class="cal-body-days">
                <div class="cal-weekdays">
                    ${WEEKDAYS.map(d => `<div class="cal-weekday">${d}</div>`).join('')}
                </div>
                <div class="cal-days"></div>
            </div>

            <div class="cal-body-months cal-grid-view cal-view-hidden"></div>
            <div class="cal-body-years cal-grid-view cal-view-hidden"></div>
        `;

        const btnPrev = container.querySelector('.cal-prev');
        const btnNext = container.querySelector('.cal-next');
        const btnMonth = container.querySelector('.cal-month-btn');
        const btnYear = container.querySelector('.cal-year-btn');
        
        const viewDays = container.querySelector('.cal-body-days');
        const viewMonths = container.querySelector('.cal-body-months');
        const viewYears = container.querySelector('.cal-body-years');
        const daysGrid = container.querySelector('.cal-days');

        function render() {
            if (currentView === 'days') {
                viewDays.classList.remove('cal-view-hidden');
                viewMonths.classList.add('cal-view-hidden');
                viewYears.classList.add('cal-view-hidden');
                
                btnMonth.textContent = MONTHS[currentDate.getMonth()];
                btnYear.textContent = currentDate.getFullYear();
                
                renderDays();
            } else if (currentView === 'months') {
                viewDays.classList.add('cal-view-hidden');
                viewMonths.classList.remove('cal-view-hidden');
                viewYears.classList.add('cal-view-hidden');
                
                btnMonth.textContent = 'Select Month';
                btnYear.textContent = currentDate.getFullYear();
                
                renderMonths();
            } else if (currentView === 'years') {
                viewDays.classList.add('cal-view-hidden');
                viewMonths.classList.add('cal-view-hidden');
                viewYears.classList.remove('cal-view-hidden');
                
                btnMonth.textContent = '';
                btnYear.textContent = `${currentYearBlockStart} - ${currentYearBlockStart + 11}`;
                
                renderYears();
            }
        }

        function renderDays() {
            daysGrid.innerHTML = '';
            
            const year = currentDate.getFullYear();
            const month = currentDate.getMonth();
            
            const firstDay = new Date(year, month, 1);
            const lastDay = new Date(year, month + 1, 0);
            
            let startDayOfWeek = firstDay.getDay() - 1;
            if (startDayOfWeek === -1) startDayOfWeek = 6; // Sunday is 6
            
            const prevMonthLastDay = new Date(year, month, 0).getDate();
            
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            // Previous month days
            for (let i = startDayOfWeek - 1; i >= 0; i--) {
                const dayNum = prevMonthLastDay - i;
                const d = new Date(year, month - 1, dayNum);
                daysGrid.appendChild(createDayElement(d, dayNum, true));
            }
            
            // Current month days
            for (let i = 1; i <= lastDay.getDate(); i++) {
                const d = new Date(year, month, i);
                daysGrid.appendChild(createDayElement(d, i, false));
            }
            
            // Next month days (fill up to 42 days, 6 rows)
            const totalCells = daysGrid.children.length;
            const cellsToAdd = 42 - totalCells;
            for (let i = 1; i <= cellsToAdd; i++) {
                const d = new Date(year, month + 1, i);
                daysGrid.appendChild(createDayElement(d, i, true));
            }
        }

        function createDayElement(date, text, isOtherMonth) {
            date.setHours(0, 0, 0, 0);
            const el = document.createElement('div');
            el.className = 'cal-day';
            if (isOtherMonth) el.classList.add('cal-other-month');
            
            el.textContent = text;
            
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            
            if (date.getTime() === today.getTime()) {
                el.classList.add('cal-today');
            }
            
            if (selectedDate && date.getTime() === selectedDate.getTime()) {
                el.classList.add('cal-selected');
            }
            
            if (maxDate && date > maxDate) {
                el.classList.add('cal-disabled');
            }
            
            el.addEventListener('click', () => {
                if (el.classList.contains('cal-disabled')) return;
                
                selectedDate = new Date(date);
                
                const y = date.getFullYear();
                const m = String(date.getMonth() + 1).padStart(2, '0');
                const d = String(date.getDate()).padStart(2, '0');
                
                input.value = `${y}-${m}-${d}`;
                
                render();
                closeCalendar();
                
                if (options.onSelect) {
                    options.onSelect(input.value, selectedDate);
                }
            });
            
            return el;
        }

        function renderMonths() {
            viewMonths.innerHTML = '';
            MONTHS.forEach((m, idx) => {
                const el = document.createElement('div');
                el.className = 'cal-grid-item';
                el.textContent = m.substring(0, 3);
                
                if (currentDate.getMonth() === idx) {
                    el.classList.add('cal-selected');
                }
                
                el.addEventListener('click', () => {
                    currentDate.setMonth(idx);
                    currentView = 'days';
                    render();
                });
                viewMonths.appendChild(el);
            });
        }

        function renderYears() {
            viewYears.innerHTML = '';
            for (let i = 0; i < 12; i++) {
                const y = currentYearBlockStart + i;
                const el = document.createElement('div');
                el.className = 'cal-grid-item';
                el.textContent = y;
                
                if (currentDate.getFullYear() === y) {
                    el.classList.add('cal-selected');
                }
                
                if (maxDate && y > maxDate.getFullYear()) {
                    el.classList.add('cal-disabled');
                    el.style.opacity = '0.3';
                    el.style.pointerEvents = 'none';
                }
                
                el.addEventListener('click', () => {
                    currentDate.setFullYear(y);
                    currentView = 'months';
                    render();
                });
                viewYears.appendChild(el);
            }
        }

        // Navigation
        btnPrev.addEventListener('click', (e) => {
            e.stopPropagation();
            if (currentView === 'days') {
                currentDate.setMonth(currentDate.getMonth() - 1);
            } else if (currentView === 'months') {
                currentDate.setFullYear(currentDate.getFullYear() - 1);
            } else if (currentView === 'years') {
                currentYearBlockStart -= 12;
            }
            render();
        });

        btnNext.addEventListener('click', (e) => {
            e.stopPropagation();
            if (currentView === 'days') {
                currentDate.setMonth(currentDate.getMonth() + 1);
            } else if (currentView === 'months') {
                currentDate.setFullYear(currentDate.getFullYear() + 1);
            } else if (currentView === 'years') {
                currentYearBlockStart += 12;
            }
            render();
        });

        btnMonth.addEventListener('click', (e) => {
            e.stopPropagation();
            if (currentView === 'months') {
                currentView = 'days';
            } else {
                currentView = 'months';
            }
            render();
        });

        btnYear.addEventListener('click', (e) => {
            e.stopPropagation();
            if (currentView === 'years') {
                currentView = 'days';
            } else {
                currentYearBlockStart = currentDate.getFullYear() - 5;
                currentView = 'years';
            }
            render();
        });

        function positionCalendar() {
            const rect = input.getBoundingClientRect();
            const scrollY = window.scrollY || window.pageYOffset;
            const scrollX = window.scrollX || window.pageXOffset;
            
            const position = options.position || 'below';
            
            if (position === 'above') {
                container.style.top = (rect.top + scrollY - container.offsetHeight - 5) + 'px';
            } else {
                container.style.top = (rect.bottom + scrollY + 5) + 'px';
            }
            
            container.style.left = (rect.left + scrollX) + 'px';
        }

        function openCalendar() {
            console.log("openCalendar triggered");
            if (container.classList.contains('cal-visible')) return;
            
            // if input has value, sync it
            if (input.value) {
                const parsed = new Date(input.value);
                if (!isNaN(parsed.getTime())) {
                    selectedDate = new Date(parsed);
                    currentDate = new Date(parsed);
                    currentYearBlockStart = currentDate.getFullYear() - 5;
                }
            }
            currentView = 'days';
            render();
            
            container.classList.add('cal-visible');
            container.style.display = 'block'; // force layout calculation
            positionCalendar();
        }

        function closeCalendar() {
            container.classList.remove('cal-visible');
            setTimeout(() => {
                if (!container.classList.contains('cal-visible')) {
                    container.style.display = 'none';
                }
            }, 200);
        }

        input.addEventListener('focus', openCalendar);
        input.addEventListener('click', openCalendar);

        document.addEventListener('click', (e) => {
            if (!container.contains(e.target) && e.target !== input) {
                closeCalendar();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                closeCalendar();
            }
        });
        
        window.addEventListener('resize', () => {
            if (container.classList.contains('cal-visible')) {
                positionCalendar();
            }
        });
        
        window.addEventListener('scroll', () => {
            if (container.classList.contains('cal-visible')) {
                positionCalendar();
            }
        });
    };
})();
