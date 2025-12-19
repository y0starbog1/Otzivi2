// 📁 wwwroot/js/activityTracker.js

class ActivityTracker {
    constructor() {
        this.lastActivity = Date.now();
        this.TIMEOUT = 60000; // 1 минута в миллисекундах
        this.WARNING_TIME = 30000; // Предупреждение за 30 секунд
        this.warningShown = false;
        this.init();
    }

    init() {
        console.log('⏰ Activity tracker initialized');

        // Отслеживаем активность пользователя
        this.trackActivity();

        // Проверяем каждые 5 секунд
        setInterval(() => this.checkActivity(), 5000);
    }

    trackActivity() {
        const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];

        events.forEach(event => {
            document.addEventListener(event, () => {
                this.lastActivity = Date.now();
                this.warningShown = false;
            }, { passive: true });
        });
    }

    checkActivity() {
        const idleTime = Date.now() - this.lastActivity;
        const timeLeft = this.TIMEOUT - idleTime;

        // Предупреждение за 30 секунд до конца
        if (timeLeft > 0 && timeLeft <= this.WARNING_TIME && !this.warningShown) {
            this.showWarning(Math.ceil(timeLeft / 1000));
            this.warningShown = true;
        }

        // Таймаут
        if (idleTime >= this.TIMEOUT) {
            this.handleTimeout();
        }
    }

    showWarning(secondsLeft) {
        // Простое уведомление в консоль для теста
        console.warn(`⚠️ Сессия завершится через ${secondsLeft} секунд!`);

        // Можно показать пользователю (раскомментируйте если нужно)
        // this.showNotification(`Сессия завершится через ${secondsLeft} секунд!`);
    }

    showNotification(message) {
        // Удаляем старые уведомления
        const oldNotification = document.getElementById('session-notification');
        if (oldNotification) oldNotification.remove();

        // Создаем новое уведомление
        const notification = document.createElement('div');
        notification.id = 'session-notification';
        notification.innerHTML = `
            <div style="position: fixed; top: 20px; right: 20px; background: #ffc107; 
                        color: #856404; padding: 15px; border-radius: 5px; z-index: 9999;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.2); max-width: 300px;
                        animation: slideIn 0.3s ease;">
                <strong>⚠️ Внимание!</strong>
                <p>${message}</p>
                <button onclick="document.getElementById('session-notification').remove()" 
                        style="background: #856404; color: white; border: none; 
                               padding: 5px 10px; border-radius: 3px; cursor: pointer;">
                    Понятно
                </button>
            </div>
        `;

        document.body.appendChild(notification);

        // Удаляем через 5 секунд
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 5000);
    }

    handleTimeout() {
        console.log('🔒 Session timeout - redirecting to login');

        // Отправляем запрос на выход
        this.sendLogoutRequest();

        // Показываем сообщение
        this.showNotification('Сессия завершена из-за неактивности. Вы будете перенаправлены...');

        // Редирект через 2 секунды
        setTimeout(() => {
            window.location.href = '/Account/Login?timeout=true';
        }, 2000);
    }

    async sendLogoutRequest() {
        try {
            const token = this.getAntiForgeryToken();
            if (token) {
                await fetch('/Account/Logout', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    }
                });
            }
        } catch (error) {
            console.error('Logout error:', error);
        }
    }

    getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }
}

// Инициализация
let activityTracker;
document.addEventListener('DOMContentLoaded', function () {
    // Проверяем, аутентифицирован ли пользователь
    const isAuthenticated = document.body.hasAttribute('data-authenticated') ||
        document.querySelector('input[name="__RequestVerificationToken"]');

    if (isAuthenticated) {
        activityTracker = new ActivityTracker();
        console.log('✅ Activity tracker started for authenticated user');
    }
});