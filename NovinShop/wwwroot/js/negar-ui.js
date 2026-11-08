// ===== اسکریپت تعاملی رابط کاربری نگارشاپ (اسلایدرها، درگ موس، تایمر زنده و شورتکات جستجو) =====
(function () {
    var d = document, b = d.body;
    var on = function (id, fn) { var e = d.getElementById(id); if (e) e.onclick = fn; };

    // ۱. منوی موبایل، مگامنو شناور دسکتاپ و اسکرول به بالا
    var openNav = function () { b.classList.add('nav-open'); };
    var closeNav = function () { b.classList.remove('nav-open'); };
    on('menuBtn', openNav);
    on('bnavCats', openNav);
    on('navShade', closeNav);
    on('navClose', closeNav);
    on('toTop', function () { window.scrollTo({ top: 0, behavior: 'smooth' }); });

    // کنترل کلیک و هاور مگامنو شناور (دسته بندی ها)
    var mega = d.getElementById('megaMenu'), megaBtn = d.getElementById('megaBtn');
    if (mega && megaBtn) {
        megaBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = mega.classList.toggle('open');
            megaBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
        d.addEventListener('click', function (e) {
            if (!mega.contains(e.target)) {
                mega.classList.remove('open');
                megaBtn.setAttribute('aria-expanded', 'false');
            }
        });
        d.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                mega.classList.remove('open');
                megaBtn.setAttribute('aria-expanded', 'false');
            }
        });
    }

    // ۲. سایه هدر هنگام اسکرول
    var hd = d.querySelector('.hd');
    if (hd) {
        window.addEventListener('scroll', function () {
            hd.classList.toggle('scrolled', window.scrollY > 10);
        }, { passive: true });
    }

    // ۳. شورتکات Ctrl+K برای فوکوس روی جستجوی هدر
    var searchInp = d.getElementById('hdSearchInput');
    window.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            if (searchInp) {
                e.preventDefault();
                searchInp.focus();
                searchInp.select();
            }
        }
    });

    // ۴. تغییر حالت روشن / تیره
    on('themeBtn', function () {
        var n = d.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        d.documentElement.setAttribute('data-theme', n);
        try { localStorage.setItem('theme', n); } catch (e) { }
    });

    // ۵. مخفی‌سازی خودکار پیام‌های سیستم
    d.querySelectorAll('.alert').forEach(function (a) {
        setTimeout(function () { a.classList.add('hide'); }, 4500);
    });
    d.querySelectorAll('details.fdet').forEach(function (x) {
        if (matchMedia('(min-width:821px)').matches) x.open = true;
    });

    // ۶. دکمه‌های ناوبری اسلایدرهای افقی محصولات و مقالات + دکمه‌های تعداد (+ و -)
    d.addEventListener('click', function (e) {
        var s = e.target.closest('[data-step]');
        if (s) {
            var inp = s.parentElement.querySelector('input');
            if (inp) inp.value = Math.max(+inp.min || 1, Math.min(+inp.max || 10, (+inp.value || 1) + (+s.dataset.step)));
            return;
        }
        var r = e.target.closest('[data-rail]');
        if (r) {
            var wrap = r.closest('.rail-wrap');
            var rail = wrap ? wrap.querySelector('.rail') : null;
            if (rail) {
                var rtl = getComputedStyle(rail).direction === 'rtl';
                var step = rail.clientWidth * 0.78;
                rail.scrollBy({
                    left: (r.dataset.rail === 'next' ? 1 : -1) * (rtl ? -1 : 1) * step,
                    behavior: 'smooth'
                });
            }
        }
    });

    // ۷. قابلیت Drag با موس روی اسلایدرهای افقی (مشابه Swiper)
    d.querySelectorAll('.rail').forEach(function (rail) {
        var isDown = false, startX = 0, scrollLeft = 0, moved = false;
        rail.addEventListener('mousedown', function (e) {
            if (e.button !== 0) return;
            isDown = true;
            moved = false;
            rail.classList.add('dragging');
            startX = e.pageX - rail.offsetLeft;
            scrollLeft = rail.scrollLeft;
        });
        window.addEventListener('mouseup', function () {
            if (!isDown) return;
            isDown = false;
            rail.classList.remove('dragging');
        });
        rail.addEventListener('mousemove', function (e) {
            if (!isDown) return;
            var x = e.pageX - rail.offsetLeft;
            var walk = (x - startX) * 1.4;
            if (Math.abs(walk) > 6) {
                moved = true;
                e.preventDefault();
                rail.scrollLeft = scrollLeft - walk;
            }
        });
        rail.addEventListener('click', function (e) {
            if (moved) {
                e.preventDefault();
                e.stopPropagation();
                moved = false;
            }
        }, true);
    });

    // ۸. اسلایدر بنر اصلی (Hero Full-Bleed)
    var slider = d.getElementById('slider');
    if (slider) {
        var slides = [].slice.call(slider.querySelectorAll('.slide')),
            dots = d.getElementById('dots'),
            pv = d.getElementById('slPrev'),
            nx = d.getElementById('slNext'),
            idx = 0, timer;
        if (slides.length > 1 && dots) {
            slides.forEach(function (_, k) {
                var dot = d.createElement('i');
                dot.onclick = function () { goSlide(k); };
                dots.appendChild(dot);
            });
            function goSlide(k) {
                slides[idx].classList.remove('on');
                if (dots.children[idx]) dots.children[idx].classList.remove('on');
                idx = (k + slides.length) % slides.length;
                slides[idx].classList.add('on');
                if (dots.children[idx]) dots.children[idx].classList.add('on');
                clearInterval(timer);
                timer = setInterval(function () { goSlide(idx + 1); }, 5500);
            }
            if (pv) pv.onclick = function () { goSlide(idx - 1); };
            if (nx) nx.onclick = function () { goSlide(idx + 1); };
            goSlide(0);
        } else if (dots) {
            dots.style.display = 'none';
            if (pv) pv.style.display = 'none';
            if (nx) nx.style.display = 'none';
        }
    }

    // ۹. تایمر معکوس زنده پیشنهادهای شگفت‌انگیز (با اعداد فارسی)
    var cd = d.querySelector('.cd[data-end]');
    if (cd) {
        var toFa = function (n) {
            return String(n).padStart(2, '0').replace(/\d/g, function (x) { return '۰۱۲۳۴۵۶۷۸۹'[x]; });
        };
        var rawEnd = cd.dataset.end ? new Date(cd.dataset.end).getTime() : NaN;
        // اگر تاریخ پایان تنظیم نشده یا گذشته باشد، یک بازه پیش‌فرض ۱۴ ساعته برای نمایش زنده تایمر در نظر می‌گیریم
        if (isNaN(rawEnd) || rawEnd <= Date.now()) {
            rawEnd = Date.now() + ((14 * 3600 + 28 * 60 + 45) * 1000);
        }
        var elD = cd.querySelector('[data-u=d]'),
            elH = cd.querySelector('[data-u=h]'),
            elM = cd.querySelector('[data-u=m]'),
            elS = cd.querySelector('[data-u=s]');
        var tick = function () {
            var s = Math.max(0, Math.floor((rawEnd - Date.now()) / 1000));
            var totalHours = Math.floor(s / 3600);
            var days = Math.floor(s / 86400);
            var hrs = elD ? Math.floor((s % 86400) / 3600) : Math.min(99, totalHours);
            var mins = Math.floor((s % 3600) / 60);
            var secs = s % 60;
            if (elD) elD.textContent = toFa(days);
            if (elH) elH.textContent = toFa(hrs);
            if (elM) elM.textContent = toFa(mins);
            if (elS) elS.textContent = toFa(secs);
        };
        tick();
        setInterval(tick, 1000);
    }
})();
