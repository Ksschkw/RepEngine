// Wallet Management — RepEngine
let currentWallet = null;

let _phantomProvider = null;
let _solflareProvider = null;

function isMobile() {
    return /Android|iPhone|iPad|iPod|webOS|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
}

function isInAppBrowser() {
    return !!(window.phantom?.solana?.isPhantom || window.solflare?.isSolflare);
}

// Poll until wallet providers are injected (wallets inject async)
async function detectProviders(maxMs = 4000) {
    const step = 100;
    let elapsed = 0;
    while (elapsed < maxMs) {
        if (!_phantomProvider) {
            const p = window.phantom?.solana || window.solana;
            if (p?.isPhantom) _phantomProvider = p;
        }
        if (!_solflareProvider && window.solflare?.isSolflare) {
            _solflareProvider = window.solflare;
        }
        if (_phantomProvider && _solflareProvider) break;
        await new Promise(r => setTimeout(r, step));
        elapsed += step;
    }
}

// ── Initialization ─────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {

    // Restore saved wallet first (instant UI update)
    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        setWalletConnectedState(savedWallet);
    }

    // Wait for provider injection BEFORE checking isInAppBrowser
    await detectProviders(3000);

    // Auto-connect if we're inside a wallet's in-app browser
    if (!savedWallet && isInAppBrowser()) {
        autoConnectInAppBrowser();
        return;
    }

    // Mobile top-bar button
    document.getElementById('walletConnectBtn')?.addEventListener('click', () => {
        if (currentWallet) showMobileWalletSheet();
        else promptWalletConnection();
    });

    // Desktop navbar button
    document.getElementById('walletConnectBtnDesktop')?.addEventListener('click', (e) => {
        if (currentWallet) showWalletMenu(e.currentTarget);
        else promptWalletConnection();
    });

    // Close desktop dropdown on outside click
    document.addEventListener('click', (e) => {
        const menu = document.getElementById('walletDropdownMenu');
        if (menu && !menu.contains(e.target) && !e.target.closest('#walletConnectBtnDesktop')) {
            menu.remove();
        }
    });
});

// ── Auto-connect inside wallet in-app browser ──────────
async function autoConnectInAppBrowser() {
    let providerName = null;
    if (window.solflare?.isSolflare) providerName = 'solflare';
    else if (window.phantom?.solana?.isPhantom || window.solana?.isPhantom) providerName = 'phantom';
    if (!providerName) return;

    showInAppOverlay(providerName);
    try {
        await connectWeb3Wallet(providerName);
    } finally {
        removeInAppOverlay();
    }
}

function showInAppOverlay(providerName) {
    if (document.getElementById('inAppOverlay')) return;
    const el = document.createElement('div');
    el.id = 'inAppOverlay';
    el.style.cssText = `position:fixed;inset:0;z-index:99999;background:rgba(10,14,26,0.97);
        display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1.5rem;`;
    el.innerHTML = `
        <div style="font-size:3rem">${providerName === 'solflare' ? '☀️' : '👻'}</div>
        <div style="color:#fff;font-size:1.1rem;font-weight:700;">Connecting to ${providerName === 'solflare' ? 'Solflare' : 'Phantom'}</div>
        <div style="color:rgba(255,255,255,0.6);font-size:0.85rem;text-align:center;max-width:260px;line-height:1.5">
            Check your wallet app for a connection and signature request
        </div>
        <div class="spinner" style="width:32px;height:32px;border-width:3px;"></div>
    `;
    document.body.appendChild(el);
}

function removeInAppOverlay() {
    document.getElementById('inAppOverlay')?.remove();
}

// ── Mobile Bottom Sheet (replaces dropdown on mobile) ──
function showMobileWalletSheet() {
    if (document.getElementById('walletSheet')) return;

    const sheet = document.createElement('div');
    sheet.id = 'walletSheet';

    // Backdrop
    const backdrop = document.createElement('div');
    backdrop.style.cssText = `position:fixed;inset:0;z-index:10000;background:rgba(0,0,0,0.6);backdrop-filter:blur(4px);`;
    backdrop.onclick = () => sheet.remove();

    // Sheet panel sliding up from bottom
    const panel = document.createElement('div');
    panel.style.cssText = `
        position:fixed;bottom:0;left:0;right:0;z-index:10001;
        background:var(--bg-secondary,#1a1f2e);
        border-radius:20px 20px 0 0;
        padding:0.75rem 1.5rem 2.5rem;
        box-shadow:0 -8px 40px rgba(0,0,0,0.5);
        animation:slideUp 0.25s cubic-bezier(0.32,0.72,0,1);
    `;

    // Inject slide-up animation if not already present
    if (!document.getElementById('walletSheetStyle')) {
        const style = document.createElement('style');
        style.id = 'walletSheetStyle';
        style.textContent = `
            @keyframes slideUp { from { transform: translateY(100%); } to { transform: translateY(0); } }
        `;
        document.head.appendChild(style);
    }

    const shortAddr = truncateAddress(currentWallet);
    panel.innerHTML = `
        <!-- Drag handle -->
        <div style="width:40px;height:4px;background:rgba(255,255,255,0.2);border-radius:2px;margin:0 auto 1.25rem;"></div>
        <!-- Address -->
        <div style="background:rgba(255,255,255,0.05);border:1px solid rgba(255,255,255,0.08);border-radius:12px;padding:1rem;margin-bottom:1rem;display:flex;align-items:center;gap:12px;">
            <div style="width:36px;height:36px;border-radius:50%;background:var(--grad-primary,linear-gradient(135deg,#7c3aed,#4f46e5));display:flex;align-items:center;justify-content:center;font-size:1rem;">🔗</div>
            <div>
                <div style="color:#fff;font-weight:700;font-size:0.9rem;">${shortAddr}</div>
                <div style="color:rgba(255,255,255,0.5);font-size:0.75rem;">Connected Wallet</div>
            </div>
        </div>
        <!-- Actions -->
        <div style="display:flex;flex-direction:column;gap:0.5rem;">
            <a href="/Dashboard" style="display:flex;align-items:center;gap:12px;padding:0.875rem 1rem;border-radius:12px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06);text-decoration:none;color:#fff;font-size:0.9rem;">
                <span style="font-size:1.25rem">📊</span> Dashboard
            </a>
            <a href="/Dashboard" style="display:flex;align-items:center;gap:12px;padding:0.875rem 1rem;border-radius:12px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06);text-decoration:none;color:#fff;font-size:0.9rem;">
                <span style="font-size:1.25rem">🏅</span> My FairScore
            </a>
            <button id="sheetDisconnectBtn" style="display:flex;align-items:center;gap:12px;padding:0.875rem 1rem;border-radius:12px;background:rgba(239,68,68,0.08);border:1px solid rgba(239,68,68,0.2);width:100%;color:#ef4444;font-size:0.9rem;cursor:pointer;">
                <span style="font-size:1.25rem">🔌</span> Disconnect Wallet
            </button>
        </div>
    `;

    sheet.appendChild(backdrop);
    sheet.appendChild(panel);
    document.body.appendChild(sheet);

    document.getElementById('sheetDisconnectBtn').addEventListener('click', () => {
        sheet.remove();
        if (window.uiManager) {
            window.uiManager.confirm('Disconnect your wallet?', 'Disconnect').then(ok => { if (ok) disconnectWallet(); });
        } else if (confirm('Disconnect your wallet?')) {
            disconnectWallet();
        }
    });
}

// ── Desktop dropdown (unchanged for non-mobile) ────────
function showWalletMenu(anchor) {
    const existing = document.getElementById('walletDropdownMenu');
    if (existing) { existing.remove(); return; }

    const menu = document.createElement('div');
    menu.id = 'walletDropdownMenu';
    menu.style.cssText = `
        position:fixed; z-index:10001;
        background:var(--bg-secondary, #1a1f2e); border:1px solid rgba(255,255,255,0.1);
        border-radius:12px; padding:0.5rem; min-width:200px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.4); backdrop-filter: blur(12px);
        animation: uiScaleUp 0.15s ease-out;
    `;

    const rect = anchor.getBoundingClientRect();
    menu.style.top = (rect.bottom + 8) + 'px';
    menu.style.right = Math.max(8, window.innerWidth - rect.right) + 'px';

    const shortAddr = truncateAddress(currentWallet);
    menu.innerHTML = `
        <div style="padding:0.75rem 1rem; border-bottom:1px solid rgba(255,255,255,0.08); margin-bottom:0.25rem;">
            <div style="font-weight:700; font-size:0.9rem; color:var(--text-primary,#fff);">🔗 ${shortAddr}</div>
            <div style="font-size:0.75rem; color:var(--text-muted,#888); margin-top:2px;">Connected Wallet</div>
        </div>
        <a href="/Dashboard" style="display:flex;align-items:center;gap:8px;padding:0.6rem 1rem;border-radius:8px;text-decoration:none;color:var(--text-primary,#fff);font-size:0.875rem;" onmouseover="this.style.background='rgba(255,255,255,0.06)'" onmouseout="this.style.background='transparent'">📊 Dashboard</a>
        <a href="/Dashboard" style="display:flex;align-items:center;gap:8px;padding:0.6rem 1rem;border-radius:8px;text-decoration:none;color:var(--text-primary,#fff);font-size:0.875rem;" onmouseover="this.style.background='rgba(255,255,255,0.06)'" onmouseout="this.style.background='transparent'">🏅 My FairScore</a>
        <div style="border-top:1px solid rgba(255,255,255,0.08);margin-top:0.25rem;padding-top:0.25rem;">
            <button onclick="confirmDisconnect()" style="display:flex;align-items:center;gap:8px;padding:0.6rem 1rem;border-radius:8px;width:100%;border:none;background:transparent;color:#ef4444;cursor:pointer;font-size:0.875rem;" onmouseover="this.style.background='rgba(239,68,68,0.1)'" onmouseout="this.style.background='transparent'">🔌 Disconnect Wallet</button>
        </div>
    `;
    document.body.appendChild(menu);
}

function confirmDisconnect() {
    document.getElementById('walletDropdownMenu')?.remove();
    if (window.uiManager) {
        window.uiManager.confirm('Disconnect your wallet?', 'Disconnect Wallet').then(ok => { if (ok) disconnectWallet(); });
    } else if (confirm('Disconnect your wallet?')) {
        disconnectWallet();
    }
}

function promptWalletConnection() {
    const modal = document.getElementById('walletModal');
    if (modal) modal.style.display = 'flex';
}

function closeWalletModal() {
    const modal = document.getElementById('walletModal');
    if (modal) modal.style.display = 'none';
}

// ── Mobile deep-link ───────────────────────────────────
function buildDappUrl() {
    return window.location.origin + window.location.pathname;
}

function getMobileDeepLink(providerName) {
    const dappUrl = encodeURIComponent(buildDappUrl());
    if (providerName === 'phantom') {
        return {
            deepLink: `https://phantom.app/ul/browse/${dappUrl}`,
            appStoreIOS: 'https://apps.apple.com/app/phantom-crypto-wallet/id1598432977',
            appStoreAndroid: 'https://play.google.com/store/apps/details?id=app.phantom'
        };
    } else if (providerName === 'solflare') {
        return {
            deepLink: `https://solflare.com/ul/v1/browse/${dappUrl}`,
            appStoreIOS: 'https://apps.apple.com/app/solflare/id1580902717',
            appStoreAndroid: 'https://play.google.com/store/apps/details?id=com.solflare.mobile'
        };
    }
    return null;
}

function redirectToMobileWallet(providerName) {
    const links = getMobileDeepLink(providerName);
    if (!links) return;

    let didLeave = false;
    const onVisChange = () => { if (document.hidden) { didLeave = true; document.removeEventListener('visibilitychange', onVisChange); } };
    document.addEventListener('visibilitychange', onVisChange);

    window.location.href = links.deepLink;

    setTimeout(() => {
        document.removeEventListener('visibilitychange', onVisChange);
        if (didLeave || document.hidden) return;

        const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);
        const storeUrl = isIOS ? links.appStoreIOS : links.appStoreAndroid;
        if (typeof showNotification === 'function') {
            showNotification(`App not found. Redirecting to install...`, 'info');
        }
        setTimeout(() => { window.location.href = storeUrl; }, 800);
    }, 2500);
}

// ── Main connection flow ───────────────────────────────
async function connectWeb3Wallet(providerName) {
    let clickedBtn = null;
    let originalBtnHtml = '';

    try {
        const buttons = document.querySelectorAll('.btn');
        for (let b of buttons) {
            if (b.getAttribute('onclick')?.includes(`connectWeb3Wallet('${providerName}')`)) {
                clickedBtn = b;
                originalBtnHtml = b.innerHTML;
                b.innerHTML = `<span class="spinner" style="display:inline-block;width:16px;height:16px;border-width:2px;vertical-align:middle;margin-right:8px;border-color:currentColor;border-right-color:transparent;"></span> Connecting...`;
                b.disabled = true;
                break;
            }
        }

        // Mobile: redirect to wallet app (unless we're already in-app browser)
        if (isMobile() && !isInAppBrowser()) {
            if (clickedBtn) { clickedBtn.innerHTML = originalBtnHtml; clickedBtn.disabled = false; }
            closeWalletModal();
            redirectToMobileWallet(providerName);
            return;
        }

        // Get provider
        let provider = providerName === 'phantom' ? _phantomProvider : _solflareProvider;

        if (!provider) {
            if (providerName === 'solflare' && window.solflare?.isSolflare) provider = window.solflare;
            else if (providerName === 'phantom') {
                const p = window.phantom?.solana || window.solana;
                if (p?.isPhantom) provider = p;
            }
        }

        // Extra wait for in-app browser injection
        if (!provider && isInAppBrowser()) {
            for (let i = 0; i < 30; i++) {
                await new Promise(r => setTimeout(r, 200));
                if (providerName === 'solflare' && window.solflare?.isSolflare) { provider = window.solflare; break; }
                if (providerName === 'phantom') {
                    const p = window.phantom?.solana || window.solana;
                    if (p?.isPhantom) { provider = p; break; }
                }
            }
        }

        if (!provider) {
            if (clickedBtn) { clickedBtn.innerHTML = originalBtnHtml; clickedBtn.disabled = false; }
            if (isMobile()) {
                redirectToMobileWallet(providerName);
            } else {
                window.open(providerName === 'phantom' ? 'https://phantom.app/download' : 'https://solflare.com/download', '_blank');
                if (typeof showNotification === 'function') showNotification(`${providerName} extension not detected. Please install it.`, 'warning');
            }
            return;
        }

        // Connect
        const resp = await provider.connect();
        let pubKeyObj = (resp?.publicKey) ? resp.publicKey : provider.publicKey;
        if (!pubKeyObj) throw new Error("Could not retrieve public key. Ensure wallet is unlocked.");
        const address = typeof pubKeyObj.toString === 'function' ? pubKeyObj.toString() : String(pubKeyObj);

        if (clickedBtn) clickedBtn.innerHTML = `<span class="spinner" style="display:inline-block;width:16px;height:16px;border-width:2px;vertical-align:middle;margin-right:8px;border-color:currentColor;border-right-color:transparent;"></span> Sign Message...`;

        // Sign message
        const msg = `Sign to authenticate with RepEngine.\n\nNonce: ${Date.now()}`;
        const signedMessage = await provider.signMessage(new TextEncoder().encode(msg), "utf8");
        if (!signedMessage) throw new Error("Signature rejected.");

        setWalletConnectedState(address);
        closeWalletModal();
        if (typeof showNotification === 'function') showNotification('Wallet connected! 🎉', 'success');

    } catch (err) {
        console.error("Wallet error:", err);
        const rejected = err.message?.toLowerCase().includes("rejected") || err.message?.toLowerCase().includes("cancel") || err.code === 4001;
        if (typeof showNotification === 'function') {
            showNotification(rejected ? 'Connection cancelled.' : 'Connection failed: ' + err.message, rejected ? 'info' : 'error');
        }
    } finally {
        if (clickedBtn) { clickedBtn.innerHTML = originalBtnHtml; clickedBtn.disabled = false; }
    }
}

function setWalletConnectedState(walletAddress) {
    currentWallet = walletAddress;
    localStorage.setItem('connectedWallet', walletAddress);

    const walletBtnTxt = document.getElementById('walletBtnText');
    const walletBtn = document.getElementById('walletConnectBtn');
    if (walletBtn && walletBtnTxt) { walletBtn.classList.add('connected'); walletBtnTxt.textContent = truncateAddress(walletAddress); }

    const desktopBtn = document.getElementById('walletConnectBtnDesktop');
    const desktopBtnTxt = document.getElementById('walletBtnTextDesktop');
    if (desktopBtn && desktopBtnTxt) { desktopBtn.classList.add('connected'); desktopBtnTxt.textContent = truncateAddress(walletAddress); }

    window.dispatchEvent(new CustomEvent('walletConnected', { detail: { wallet: walletAddress } }));
}

function disconnectWallet() {
    currentWallet = null;
    localStorage.removeItem('connectedWallet');

    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnTxt = document.getElementById('walletBtnText');
    if (walletBtn && walletBtnTxt) { walletBtn.classList.remove('connected'); walletBtnTxt.textContent = 'Connect'; }

    const desktopBtn = document.getElementById('walletConnectBtnDesktop');
    const desktopBtnTxt = document.getElementById('walletBtnTextDesktop');
    if (desktopBtn && desktopBtnTxt) { desktopBtn.classList.remove('connected'); desktopBtnTxt.textContent = 'Connect Wallet'; }

    window.dispatchEvent(new CustomEvent('walletDisconnected'));
    if (typeof showNotification === 'function') showNotification('Wallet disconnected.', 'info');
}

function truncateAddress(addr) {
    if (!addr || addr.length <= 12) return addr;
    return `${addr.substring(0, 6)}...${addr.substring(addr.length - 4)}`;
}

function getCurrentWallet() { return currentWallet; }

window.walletManager = { getCurrentWallet, disconnectWallet, promptWalletConnection };
