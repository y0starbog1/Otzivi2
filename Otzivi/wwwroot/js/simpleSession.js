// 📁 simpleSession.js - МИНИМАЛЬНАЯ ВЕРСИЯ
document.addEventListener('DOMContentLoaded', function () {
    console.log('⏰ Session timer started (1 minute)');

    let lastActivity = Date.now();
    const TIMEOUT = 60000; // 1 минута в миллисекундах
    const WARNING_TIME = 30000; // Предупреждение за 30 секунд
    let warningShown = false;

    // Отслеживаем активность
    const trackActivity = () => {
        lastActivity = Date.now();
        warningShown = false;
    };

    // События активности
    ['click', 'mousemove', 'keypress', 'scroll'].forEach(event => {
        document.addEventListener(event, trackActivity, { passive: true });
    });

    // Проверка каждые 10 секунд
    setInterval(() => {
        const idleTime = Date.now() - lastActivity;

        // Предупреждение
        if (idleTime >= WARNING_TIME && !warningShown) {
            warningShown = true;
            showWarning();
        }

        // Таймаут
        if (idleTime >= TIMEOUT) {
            console.log('🔒 Session timeout - redirecting to login');
            window.location.href = '/Account/Login?timeout=true';
        }
    }, 10000); // Проверка каждые 10 секунд
});

function showWarning() {
    // Создаем простое уведомление
    const div = document.createElement('div');
    div.innerHTML = `
        <div style="
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: #ffc107;
            color: #856404;
            padding: 15px;
            border-radius: 5px;
            z-index: 9999;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2);
            max-width: 300px;
            animation: slideIn 0.3s ease;
        ">
            <strong>⚠️ Сессия скоро завершится!</strong>
            <p>Вы будете автоматически выйдены через 30 секунд.</p>
            <small>Выполните любое действие для продления сессии.</small>
        </div>
    `;

    document.body.appendChild(div);

    // Удаляем через 5 секунд
    setTimeout(() => {
        if (div.parentNode) {
            div.parentNode.removeChild(div);
        }
    }, 5000);
}