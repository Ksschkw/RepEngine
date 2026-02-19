class UiManager {
    constructor() {
        this.initStyles();
    }

    // Creates the necessary CSS dynamically so we don't clutter site.css
    initStyles() {
        if (document.getElementById('ui-manager-styles')) return;

        const style = document.createElement('style');
        style.id = 'ui-manager-styles';
        style.textContent = `
            .ui-modal-overlay {
                position: fixed;
                inset: 0;
                background: rgba(0, 0, 0, 0.7);
                backdrop-filter: blur(8px);
                z-index: 10000;
                display: flex;
                align-items: center;
                justify-content: center;
                padding: 1rem;
                animation: uiFadeIn 0.2s ease-out;
            }

            .ui-modal {
                background: var(--bg-secondary);
                border: 1px solid var(--border);
                border-radius: var(--radius-lg);
                width: 100%;
                max-width: 400px;
                box-shadow: var(--shadow-lg);
                animation: uiScaleUp 0.2s cubic-bezier(0.16, 1, 0.3, 1);
                overflow: hidden;
            }

            .ui-modal-header {
                padding: 1rem;
                border-bottom: 1px solid var(--border);
                font-weight: 700;
                display: flex;
                justify-content: space-between;
                align-items: center;
            }

            .ui-modal-body {
                padding: 1.5rem 1rem;
                color: var(--text-secondary);
                font-size: 0.95rem;
                line-height: 1.5;
            }

            .ui-modal-footer {
                padding: 1rem;
                background: rgba(0, 0, 0, 0.2);
                display: flex;
                justify-content: flex-end;
                gap: 0.75rem;
            }

            .ui-input {
                width: 100%;
                background: var(--bg-tertiary);
                border: 1px solid var(--border);
                border-radius: var(--radius-md);
                padding: 0.75rem;
                color: var(--text-primary);
                font-size: 1rem;
                margin-top: 0.5rem;
            }

            .ui-input:focus {
                outline: none;
                border-color: var(--accent-primary);
            }

            @keyframes uiFadeIn { from { opacity: 0; } to { opacity: 1; } }
            @keyframes uiScaleUp { from { transform: scale(0.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }
        `;
        document.head.appendChild(style);
    }

    /**
     * Shows a custom Alert modal.
     * @param {string} message 
     * @param {string} [title='Alert'] 
     * @returns {Promise<void>}
     */
    alert(message, title = 'Alert') {
        return new Promise((resolve) => {
            this._createModal({
                title,
                content: message,
                buttons: [
                    { text: 'OK', class: 'btn btn-primary', onClick: () => resolve() }
                ]
            });
        });
    }

    /**
     * Shows a custom Confirm modal.
     * @param {string} message 
     * @param {string} [title='Confirm'] 
     * @returns {Promise<boolean>}
     */
    confirm(message, title = 'Confirm') {
        return new Promise((resolve) => {
            this._createModal({
                title,
                content: message,
                buttons: [
                    { text: 'Cancel', class: 'btn btn-secondary', onClick: () => resolve(false) },
                    { text: 'Confirm', class: 'btn btn-primary', onClick: () => resolve(true) }
                ]
            });
        });
    }

    /**
     * Shows a custom Prompt modal.
     * @param {string} message 
     * @param {string} [defaultValue=''] 
     * @param {string} [title='Prompt'] 
     * @returns {Promise<string|null>}
     */
    prompt(message, defaultValue = '', title = 'Input Required') {
        return new Promise((resolve) => {
            const inputId = 'ui-prompt-input-' + Date.now();

            const modal = this._createModal({
                title,
                content: `
                    <p class="mb-2">${message}</p>
                    <input type="text" id="${inputId}" class="ui-input" value="${defaultValue}" placeholder="Type here...">
                `,
                buttons: [
                    { text: 'Cancel', class: 'btn btn-secondary', onClick: () => resolve(null) },
                    {
                        text: 'Submit',
                        class: 'btn btn-primary',
                        onClick: () => {
                            const val = document.getElementById(inputId).value;
                            resolve(val);
                        }
                    }
                ],
                onOpen: () => {
                    const input = document.getElementById(inputId);
                    if (input) {
                        input.focus();
                        input.select();
                        input.addEventListener('keydown', (e) => {
                            if (e.key === 'Enter') {
                                const val = input.value;
                                this._closeModal(modal); // Manually close since we're bypassing button click
                                resolve(val);
                            }
                        });
                    }
                }
            });
        });
    }

    _createModal({ title, content, buttons, onOpen }) {
        const overlay = document.createElement('div');
        overlay.className = 'ui-modal-overlay';

        const modal = document.createElement('div');
        modal.className = 'ui-modal';

        // Header
        const header = document.createElement('div');
        header.className = 'ui-modal-header';
        header.innerHTML = `<span>${title}</span>`;
        // Close icon (x)
        const closeBtn = document.createElement('div');
        closeBtn.innerHTML = '&times;';
        closeBtn.style.cursor = 'pointer';
        closeBtn.style.fontSize = '1.5rem';
        closeBtn.onclick = () => {
            this._closeModal(overlay);
            // If it's a promise, this might leave it hanging if not handled, 
            // but for simple UI logic, clicking X is usually Cancel/Null.
            // For now, buttons handle the resolve. 
        };
        header.appendChild(closeBtn);

        // Body
        const body = document.createElement('div');
        body.className = 'ui-modal-body';
        body.innerHTML = typeof content === 'string' ? content : '';
        if (typeof content !== 'string') body.appendChild(content);

        // Footer
        const footer = document.createElement('div');
        footer.className = 'ui-modal-footer';

        buttons.forEach(btnConfig => {
            const btn = document.createElement('button');
            btn.className = btnConfig.class;
            btn.textContent = btnConfig.text;
            btn.onclick = () => {
                this._closeModal(overlay);
                if (btnConfig.onClick) btnConfig.onClick();
            };
            footer.appendChild(btn);
        });

        modal.appendChild(header);
        modal.appendChild(body);
        modal.appendChild(footer);
        overlay.appendChild(modal);

        document.body.appendChild(overlay);

        if (onOpen) onOpen();

        return overlay;
    }

    _closeModal(overlay) {
        if (!overlay) return;
        overlay.style.animation = 'uiFadeIn 0.2s reverse forwards';
        setTimeout(() => {
            if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
        }, 200);
    }
}

// Global instance
window.uiManager = new UiManager();

// Optional: Override defaults (with warning)
window.alert = (msg) => window.uiManager.alert(msg);
// Note: confirm/prompt are async now, so we can't fully override them 1:1 without breaking sync code.
// Ideally, we just use uiManager directly.
