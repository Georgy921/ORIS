<script>
        // === Инициализация слайдера цен ===
    const minPriceInput = document.getElementById('min-price');
    const maxPriceInput = document.getElementById('max-price');
    const minPriceValue = document.getElementById('minPriceValue');
    const maxPriceValue = document.getElementById('maxPriceValue');
    const priceFill = document.getElementById('priceFill');

    const API_URL = 'controllers/tours';

    function formatPrice(price) {
            return parseInt(price).toLocaleString('ru-RU') + ' ₽';
        }

    function updatePriceSlider() {
        let min = parseInt(minPriceInput.value);
    let max = parseInt(maxPriceInput.value);
    const minRange = parseInt(minPriceInput.min);
    const maxRange = parseInt(maxPriceInput.max);
            
            if (min > max) [min, max] = [max, min];
    minPriceInput.value = min;
    maxPriceInput.value = max;

    minPriceValue.textContent = formatPrice(min);
    maxPriceValue.textContent = formatPrice(max);

    const minPercent = ((min - minRange) / (maxRange - minRange)) * 100;
    const maxPercent = ((max - minRange) / (maxRange - minRange)) * 100;
    priceFill.style.left = minPercent + '%';
    priceFill.style.right = (100 - maxPercent) + '%';
        }

    // === Отправка фильтров на сервер ===
    async function sendFiltersToServer() {
            try {
                const searchForm = document.querySelector('.search-form');
    const from = searchForm?.querySelector('#from-field')?.value?.trim() || '';
    const to = searchForm?.querySelector('#to-field')?.value?.trim() || '';
    const date = searchForm?.querySelector('#date-field')?.value?.trim() || '';
    const duration = searchForm?.querySelector('#duration-field')?.value?.trim() || '';
    const people = searchForm?.querySelector('#people-field')?.value?.trim() || '';

    const filterSections = document.querySelectorAll('.filter-section');
    const checkboxFilters = { };
                filterSections.forEach(section => {
                    const headerText = section.querySelector('.filter-header h3')?.textContent?.trim() || '';
    const checkboxes = section.querySelectorAll('input[type="checkbox"]:checked');
                    if (checkboxes.length > 0) {
        checkboxFilters[headerText] = Array.from(checkboxes).map(cb => cb.value);
                    }
                });

    const minPrice = parseInt(document.getElementById('min-price')?.value) || 0;
    const maxPrice = parseInt(document.getElementById('max-price')?.value) || 2000000;
    const hotelSearch = document.getElementById('hotel-search')?.value?.trim() || '';

    const payload = {
        search: {from, to, date, duration, people},
    filters: checkboxFilters,
    price: {min: minPrice, max: maxPrice },
    hotel_search: hotelSearch
                };

    console.log('[Filter Sync] Sending:', payload);
    const response = await fetch('/tours/filters', {
        method: 'POST',
    headers: {'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
    body: JSON.stringify(payload)
                });
    const result = await response.json();
    console.log('📥 Ответ сервера:', result); // ← Добавьте этот лог
    console.log('📥 result.tours:', result.tours);

    if (result.success) {
        renderTours(result.tours); // 🔥 Вот здесь магия!
                }
    return result;
            } catch (error) {
        console.error('[Filter Sync ERROR]', error);
    throw error;
            }
        }

    let filterTimeout;
        minPriceInput?.addEventListener('input', () => {
        updatePriceSlider();
    clearTimeout(filterTimeout);
    filterTimeout = setTimeout(sendFiltersToServer, 200); // Debounce 300ms
        });

        maxPriceInput?.addEventListener('input', () => {
        updatePriceSlider();
    clearTimeout(filterTimeout);
    filterTimeout = setTimeout(sendFiltersToServer, 200);
        });

    function renderTours(tours) {
            const slider = document.getElementById('hot-tours-slider');
    const res = document.getElementById('remove_search');

    // 1. Проверка существования контейнера
    if (!slider) {
        console.error('❌ КРИТИЧЕСКАЯ ОШИБКА: Элемент #hot-tours-slider не найден в DOM!');
    return;
            }

    // 3. Обновление заголовка
    if (tours.length == 0) {
        res.textContent = `Ничего не найдено`;
            }
    else res.textContent = `Найдено оч много чего`

    try {
        // 4. Генерация HTML с защитой от ошибок данных
        slider.innerHTML = tours.map(tour => {

            console.log(tour.hotel_name);
            console.log(tour.hotel_name);
            console.log(tour.hotel_name);
            console.log(`<div class="tour-card">
    <img src="${tour.image_url}" alt="${tour.hotel_name}">`);
            console.log(`${hotel_name}`)
            console.log(`${tour.hotelname}`);
            console.log(`${hotelname}`)

            // Защитное получение данных (если поле null, будет пустая строка или 0)
            const imgUrl = tour.image_url;
            const hotelName = tour.hotel_name || 'Отель без названия';
            const arrivalCity = tour.arrival_city || '';
            const tourName = tour.name || '';
            const bonus = tour.bonus || 0;
            const price = parseFloat(tour.tour_price) || 0;
            const monthly = Math.floor(price / 6);

            // Формируем иконки услуг безопасно
            const servicesHtml = [];

            if (tour.wifi && String(tour.wifi).toLowerCase() !== 'нет') {
                servicesHtml.push(`<div class="services-icon"><img src="src/wifi.png" alt="wifi"><span>` + tour.wifi + `</span></div>`);
            }
            if (tour.distance_to_airport) {
                servicesHtml.push(`<div class="services-icon"><img src="src/plane_depart.png" alt="plane" ><span>Не далее ` + tour.distance_to_airport + ` км от аэропорта</span></div>`);
            }
            if (tour.has_kids_club) {
                servicesHtml.push(`<div class="services-icon"><img src="src/kids_club.png" alt="kids"><span>Детский клуб</span></div>`);
            }
            if (tour.has_aquapark) {
                servicesHtml.push(`<div class="services-icon"><img src="src/aquapark.png" alt="aquapark" ><span>Аквапарк</span></div>`);
            }
            if (tour.has_spa) {
                servicesHtml.push(`<div class="services-icon"><img src="src/Spa.png" alt="spa" ><span>SPA-центр</span></div>`);
            }
            if (tour.room_type) {
                servicesHtml.push(`<div class="services-icon"><img src="src/bedroom.png" alt="room" ><span>` + tour.room_type + `</span></div>`);
            }

            // Сборка карточки
            return `
            <div class="tour-card" onclick="window.open('/tours/details?id=${tour.id}', '_blank')" style="display: flex; background: white; border-radius: 14px; overflow: hidden; border: 1px solid #ddd; margin-bottom: 16px; width: 910px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
                <!-- Изображение -->
                <div class="card-image" style="position: relative; overflow: hidden; width: 230px; height: 312px; flex-shrink: 0; background: #f0f0f0;">
                    <img src="`+ imgUrl + `" 
                         alt="`+ hotelName + `" 
                         style="width: 100%; height: 100%; object-fit: cover;"
                    
                    <!-- Кнопка избранного -->
                    <div style="position: absolute; top: 8px; left: 8px; background: white; border-radius: 50%; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 2px 4px rgba(0,0,0,0.1); z-index: 2;">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#333" stroke-width="2">
                            <path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z" />
                        </svg>
                    </div>

                    <!-- Бейдж бонуса -->
                    <div class="labels" style="position: absolute; top: 8px; right: 8px; display: flex; flex-direction: column; gap: 4px; z-index: 2;">
                        <div style="background: #ECECFD; color: #FF5722; padding: 4px 4px; border-radius: 14px; font-size: 12px; font-weight: bold; display: flex; align-items: center; gap: 4px; white-space: nowrap;">
                            <img src="src/bonus.png" alt="bonus" style="max-height: 16px; max-width: 16px;">
                            от `+ bonus + `
                        </div>
                    </div>
                </div>

                <!-- Информация -->
                <div class="card-info" style="flex: 1; padding: 16px; display: flex; flex-direction: column; justify-content: space-between;">
                    <div>
                        <h3 style="font-size: 18px; font-weight: bold; margin: 0 0 4px 0; color: #333;">`+ hotelName + `</h3>
                        <div style="font-size: 14px; color: #666; margin-bottom: 12px;">
                            `+ arrivalCity + ` ` + tourName + `
                        </div>
                    </div>

                    <!-- Иконки -->
                    <div style="display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 12px; font-size: 13px; color: #555;">
                        `+ servicesHtml.join('') + `
                    </div>

                    <!-- Даты -->
                    <div style="display: flex; align-items: center; gap: 12px; font-size: 14px; color: #555; margin-bottom: 8px;">
                        <div style="display: flex; align-items: center; gap: 6px; font-size: 16px; color: #333;">
                            <img src="src/plane_depart.png" alt="plane">
                            <span>`+ tour.departure_date + `</span>
                            <span>•</span>
                            <span>`+ tour.adults_count + ` взрослых.</span>
                            <span>•</span>
                            <span>`+ tour.nights_count + ` ночей</span>
                        </div>
                    </div>

                    <!-- Цена и кнопка -->
                    <div style="display: flex; align-items: center; justify-content: space-between; ">
                        <div style="font-size: 14px; color: #333; display: flex; align-items: center; gap: 4px;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 14l-4-4h2v-4h4v4h2l-4 4z" />
                            </svg>
                            <span>Частями, в рассрочку или кредит от `+ monthly.toLocaleString('ru-RU') + ` ₽/мес.</span>
                        </div>
                        
                        <button style="background: #ffe135; color: #333; border: none; padding: 10px 24px; border-radius: 8px; font-weight: bold; font-size: 16px; cursor: pointer; min-width: 140px; transition: background 0.2s;"
                                onmouseover="this.style.background='#ffd700'" onmouseout="this.style.background='#ffe135'">
                            от `+ price.toLocaleString('ru-RU') + ` ₽
                        </button>
                    </div>
                </div>
            </div>`;
        }).join('');

    console.log(`✅ Успешно отрисовано `+tours.length+` карточек.`);

            } catch (e) {
        console.error('💥 Ошибка при генерации HTML:', e);
            }
        }

    // === Аккордеон фильтров ===
    function toggleFilter(header) {
        header.parentElement.classList.toggle('active');
        }



    // === Модальное окно авторизации ===
    function showAuthModal() {
        document.getElementById('auth-modal').style.display = 'flex';
        }
    function hideAuthModal() {
        document.getElementById('auth-modal').style.display = 'none';
        }


    async function submitAuth() {
            const username = document.getElementById('auth-username')?.value.trim() || '';
    const email = document.getElementById('auth-email')?.value.trim() || '';
    const password = document.getElementById('auth-password')?.value || '';
    const rememberMe = document.getElementById('remember-me')?.checked || false;

    if (!email || !password) {
        alert("Email и пароль обязательны.");
    return;
            }

    const payload = {
        email,
        password,
        rememberMe: rememberMe.toString() // Отправляем как строку "true"/"false"
            };

    if (username) {
        payload.username = username;
            }

    try {
                const res = await fetch('/auth/auth', { // Проверьте путь: '/auth' или '/login/auth'
        method: 'POST',
    headers: {'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    credentials: 'include' // 🔥 Важно для cookies
                });

    const data = await res.json();

    if (data.success) {
        // Обновляем UI
        updateAuthUI(true, data.user);
    hideAuthModal();

    // Если нужно, сохраняем токен (хотя кука уже установлена сервером)
    if (data.token) {
        console.log(' Token received');
                    }
                } else {
        alert(data.message || "Ошибка авторизации");
                }
            } catch (err) {
        console.error(" Fetch error:", err);
    alert("Не удалось подключиться к серверу");
            }
        }

    function goToTourDetails(tourId) {
            if (!tourId) {
        console.error(' ID тура не передан');
    return;
            }

    console.log(` Переход к деталям тура #${tourId}...`);

    // Редирект на страницу деталей с GET-параметром
    // Убедитесь, что путь совпадает с вашим [HttpGet("details")] в C#
    window.location.href = `/tours/details?id=${tourId}`;
        }

        /**
         * Обновляет интерфейс в зависимости от статуса авторизации
         * @param {boolean} isAuthenticated - Авторизован ли пользователь
    * @param {object | null} user - Данные пользователя или null
    */
    function updateAuthUI(isAuthenticated, user) {
            // === ЭЛЕМЕНТЫ DOM ===
            const authIcon = document.getElementById('auth-icon');
    const loginBtn = document.getElementById('login-btn');
    const logoutBtn = document.getElementById('logout-btn');
    const authModal = document.getElementById('auth-modal');

    // === ПУТИ К ИКОНКАМ (настройте под свой проект) ===
    const ICON_AUTHORIZED = 'src/AfterAuthUser.png';    //  Аватарка после входа
    const ICON_GUEST = 'src/BeforeAuthUser.png';           //  Дефолтная аватарка

    if (isAuthenticated && user) {
        //  ПОЛЬЗОВАТЕЛЬ АВТОРИЗОВАН

        console.log(` UI: Авторизован как ${user.username}`);

    // Обновляем аватарку
    if (authIcon) {
        authIcon.src = ICON_AUTHORIZED;
    authIcon.alt = user.username;
    authIcon.title = `${user.username} (${user.role})`;
    authIcon.style.cursor = 'pointer';
    authIcon.style.display = 'block';
                }


    if (authModal) authModal.style.display = 'none';

            } else {


        console.log(' UI: Пользователь не авторизован');

    // Сбрасываем аватарку на дефолтную
    if (authIcon) {
        authIcon.src = ICON_GUEST;
    authIcon.alt = 'Гость';
    authIcon.title = 'Войти в аккаунт';
    authIcon.style.cursor = 'pointer';
    authIcon.style.display = 'block';
                }

            }
        }



    async function logout() {
        await fetch('/logout', { method: 'POST' });
    updateAuthUI(false, null);
    location.reload();
        }

    // === Карта ===
    function openMapModal() {
        document.body.classList.add('map-open');
    document.getElementById('map-modal').style.display = 'flex';
        }
    function closeMapModal() {
        document.body.classList.remove('map-open');
    document.getElementById('map-modal').style.display = 'none';
        }

        // === Инициализация после загрузки DOM ===
        document.addEventListener('DOMContentLoaded', () => {
        // Слайдер цен
        updatePriceSlider();
            minPriceInput?.addEventListener('input', () => {updatePriceSlider(); });
            maxPriceInput?.addEventListener('input', () => {updatePriceSlider(); });

    async function checkAuthOnLoad() {
                try {
                    const res = await fetch('/check-auth', {
        method: 'GET',
    headers: {
        'Content-Type': 'application/json'
                        },
    credentials: 'include' // 🔥 Важно: отправляем cookies
                    });

    const data = await res.json();

    console.log('📡 Ответ сервера:', data);

    if (data.authenticated && data.user) {
        updateAuthUI(true, data.user);
                    } else {
        updateAuthUI(false, null);
                    }
                } catch (err) {
        console.error('❌ Ошибка проверки авторизации:', err);
    updateAuthUI(false, null);
                }
            }

            // Чекбоксы фильтров


            document.querySelectorAll('.filter-group input[type="checkbox"]').forEach(cb => {
        cb.addEventListener('change', sendFiltersToServer);
            });

    // Поиск отеля (с задержкой)
    document.getElementById('hotel-search')?.addEventListener('input', 
                (e) => {clearTimeout(e.target._timer); e.target._timer = setTimeout(sendFiltersToServer, 300); }
    );

    // Кнопка "Найти" 
    document.querySelector('.search-btn')?.addEventListener('click', sendFiltersToServer);

            // Закрытие модальных окон по клику вне контента
            document.getElementById('auth-modal')?.addEventListener('click', (e) => {
                if (e.target.id === 'auth-modal') hideAuthModal();
            });
            document.getElementById('map-modal')?.addEventListener('click', (e) => {
                if (e.target.id === 'map-modal') closeMapModal();
            });

    document.addEventListener('DOMContentLoaded', checkAuthOnLoad);

            // Закрытие по Esc
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') {
        hideAuthModal();
    closeMapModal();
                }
            });
        });
</script>