// PWA Utilities for RepEngine
class PWAManager {
    constructor() {
        this.deferredPrompt = null;
        this.isInstalled = false;
        this.isOnline = navigator.onLine;
        this.init();
    }

    init() {
        // Register service worker
        if ('serviceWorker' in navigator) {
            this.registerServiceWorker();
        }

        // Listen for install prompt
        window.addEventListener('beforeinstallprompt', (e) => {
            e.preventDefault();
            this.deferredPrompt = e;
            this.showInstallButton();
        });

        // Check if already installed
        window.addEventListener('appinstalled', () => {
            console.log('[PWA] App installed successfully');
            this.isInstalled = true;
            this.hideInstallButton();
            this.deferredPrompt = null;
        });

        // Monitor online/offline status
        window.addEventListener('online', () => this.handleOnline());
        window.addEventListener('offline', () => this.handleOffline());

        // Check for updates
        this.checkForUpdates();
    }

    async registerServiceWorker() {
        try {
            const registration = await navigator.serviceWorker.register('/sw.js');
            console.log('[PWA] Service Worker registered:', registration.scope);

            // Listen for updates
            registration.addEventListener('updatefound', () => {
                const newWorker = registration.installing;
                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        this.showUpdateNotification();
                    }
                });
            });
        } catch (error) {
            console.error('[PWA] Service Worker registration failed:', error);
        }
    }

    showInstallButton() {
        // Create install button if it doesn't exist
        let installBtn = document.getElementById('pwa-install-btn');
        if (!installBtn) {
            installBtn = document.createElement('button');
            installBtn.id = 'pwa-install-btn';
            installBtn.className = 'btn btn-primary pwa-install-btn';
            installBtn.innerHTML = '📱 Install App';
            installBtn.onclick = () => this.promptInstall();

            // Add to navbar or create floating button
            const navbar = document.querySelector('.navbar .container');
            if (navbar) {
                navbar.appendChild(installBtn);
            }
        }
        installBtn.style.display = 'inline-flex';
    }

    hideInstallButton() {
        const installBtn = document.getElementById('pwa-install-btn');
        if (installBtn) {
            installBtn.style.display = 'none';
        }
    }

    async promptInstall() {
        if (!this.deferredPrompt) {
            console.log('[PWA] Install prompt not available');
            return;
        }

        this.deferredPrompt.prompt();
        const { outcome } = await this.deferredPrompt.userChoice;
        console.log(`[PWA] User response: ${outcome}`);

        this.deferredPrompt = null;
        this.hideInstallButton();
    }

    showUpdateNotification() {
        const notification = document.createElement('div');
        notification.className = 'pwa-update-notification';
        notification.innerHTML = `
      <div class="glass-card" style="position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 300px; padding: 1rem;">
        <p style="margin: 0 0 0.5rem 0;"><strong>Update Available</strong></p>
        <p style="margin: 0 0 1rem 0; font-size: 0.875rem; color: var(--text-muted);">
          A new version of RepEngine is available.
        </p>
        <button class="btn btn-primary btn-sm" onclick="pwaManager.applyUpdate()">
          Update Now
        </button>
        <button class="btn btn-secondary btn-sm" onclick="this.closest('.pwa-update-notification').remove()">
          Later
        </button>
      </div>
    `;
        document.body.appendChild(notification);
    }

    applyUpdate() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.getRegistration().then((registration) => {
                if (registration && registration.waiting) {
                    registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                    window.location.reload();
                }
            });
        }
    }

    handleOnline() {
        this.isOnline = true;
        console.log('[PWA] Back online');
        this.showNetworkStatus('online');

        // Retry failed requests if any
        if ('serviceWorker' in navigator && 'sync' in ServiceWorkerRegistration.prototype) {
            navigator.serviceWorker.ready.then((registration) => {
                return registration.sync.register('sync-data');
            });
        }
    }

    handleOffline() {
        this.isOnline = false;
        console.log('[PWA] Offline');
        this.showNetworkStatus('offline');
    }

    showNetworkStatus(status) {
        // Remove existing status
        const existing = document.querySelector('.network-status');
        if (existing) existing.remove();

        const statusBar = document.createElement('div');
        statusBar.className = `network-status network-status-${status}`;
        statusBar.innerHTML = status === 'online'
            ? '✓ Back online'
            : '⚠ You are offline';

        statusBar.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      padding: 0.5rem;
      text-align: center;
      font-size: 0.875rem;
      font-weight: 600;
      z-index: 10000;
      background: ${status === 'online' ? 'var(--accent-success)' : 'var(--accent-warning)'};
      color: white;
      animation: slideDown 0.3s ease-out;
    `;

        document.body.appendChild(statusBar);

        // Auto-remove after 3 seconds
        setTimeout(() => {
            statusBar.style.animation = 'slideUp 0.3s ease-out';
            setTimeout(() => statusBar.remove(), 300);
        }, 3000);
    }

    checkForUpdates() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.getRegistration().then((registration) => {
                if (registration) {
                    registration.update();
                }
            });
        }
    }

    // Request persistent storage (optional)
    async requestPersistentStorage() {
        if (navigator.storage && navigator.storage.persist) {
            const isPersisted = await navigator.storage.persist();
            console.log(`[PWA] Persistent storage: ${isPersisted}`);
            return isPersisted;
        }
        return false;
    }
}

// Initialize PWA Manager
const pwaManager = new PWAManager();

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
  @keyframes slideDown {
    from { transform: translateY(-100%); }
    to { transform: translateY(0); }
  }
  
  @keyframes slideUp {
    from { transform: translateY(0); }
    to { transform: translateY(-100%); }
  }

  .pwa-install-btn {
    margin-left: 1rem;
    font-size: 0.875rem !important;
    padding: 0.5rem 1rem !important;
  }

  @media (max-width: 768px) {
    .pwa-install-btn {
      position: fixed;
      bottom: 20px;
      right: 20px;
      z-index: 1000;
      box-shadow: var(--shadow-xl);
    }
  }
`;
document.head.appendChild(style);

// Export for global use
window.pwaManager = pwaManager;
