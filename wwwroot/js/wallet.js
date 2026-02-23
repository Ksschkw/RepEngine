// Wallet Management — RepEngine
let currentWallet = null;

// ── Eagerly detected providers (filled on page load) ──
let _phantomProvider = null;
let _solflareProvider = null;
let _providersReady = false;

// ── Helpers ────────────────────────────────────────────
function isMobile() {
    return /Android|iPhone|iPad|iPod|webOS|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
}

function isInAppBrowser() {
    return !!(window.phantom?.solana?.isPhantom || window.solflare?.isSolflare);
}

// ── Eager provider detection on page load ──────────────
async function detectProviders() {
    const maxAttempts = 30;
    for (let i = 0; i < maxAttempts; i++) {
        if (!_phantomProvider) {
            const p = window.phantom?.solana || window.solana;
            if (p?.isPhantom) _phantomProvider = p;
        }
        if (!_solflareProvider) {
            if (window.solflare?.isSolflare) _solflareProvider = window.solflare;
        }
        if (_phantomProvider && _solflareProvider) break;
        await new Promise(r => setTimeout(r, 100));
    }
    _providersReady = true;
}

// ── Initialization ─────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    detectProviders();

    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        setWalletConnectedState(savedWallet);
        return; // Already connected, skip auto-connect
    }

    // KEY FIX: If we are ALREADY inside a wallet's in-app browser,
    // auto-connect immediately. This is what triggers the sign prompt.
    // Solflare/Phantom open your URL in their browser, injecting window.solflare
    // or window.phantom. We detect this and auto-connect.
    if (isInAppBrowser()) {
        autoConnectInAppBrowser();
    }

    // Mobile top-bar button
    const walletBtn = document.getElementById('walletConnectBtn');
    walletBtn?.addEventListener('click', async () => {
        if (currentWallet) showWalletMenu(walletBtn);
        else promptWalletConnection();
    });

    // Desktop navbar button
    const walletBtnDesktop = document.getElementById('walletConnectBtnDesktop');
    walletBtnDesktop?.addEventListener('click', async () => {
        if (currentWallet) showWalletMenu(walletBtnDesktop);
        else promptWalletConnection();
    });

    // Close menu on outside click
    document.addEventListener('click', (e) => {
        const menu = document.getElementById('walletDropdownMenu');
        if (menu && !menu.contains(e.target) && !e.target.closest('#walletConnectBtn') && !e.target.closest('#walletConnectBtnDesktop')) {
            menu.remove();
        }
    });
});

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
            <div style="font-weight:700; font-size:0.9rem; color:var(--text-primary, #fff);">🔗 ${shortAddr}</div>
            <div style="font-size:0.75rem; color:var(--text-muted, #888); margin-top:2px;">Connected Wallet</div>
        </div>
        <a href="/Dashboard" style="display:flex; align-items:center; gap:8px; padding:0.6rem 1rem; border-radius:8px; text-decoration:none; color:var(--text-primary, #fff); font-size:0.875rem; transition:background 0.15s;"
           onmouseover="this.style.background='rgba(255,255,255,0.06)'" onmouseout="this.style.background='transparent'">
            📊 Dashboard
        </a>
        <a href="/Dashboard" style="display:flex; align-items:center; gap:8px; padding:0.6rem 1rem; border-radius:8px; text-decoration:none; color:var(--text-primary, #fff); font-size:0.875rem; transition:background 0.15s;"
           onmouseover="this.style.background='rgba(255,255,255,0.06)'" onmouseout="this.style.background='transparent'">
            🏅 My FairScore
        </a>
        <div style="border-top:1px solid rgba(255,255,255,0.08); margin-top:0.25rem; padding-top:0.25rem;">
            <button onclick="confirmDisconnect()" style="display:flex; align-items:center; gap:8px; padding:0.6rem 1rem; border-radius:8px; width:100%; border:none; background:transparent; color:#ef4444; cursor:pointer; font-size:0.875rem; text-align:left; transition:background 0.15s;"
               onmouseover="this.style.background='rgba(239,68,68,0.1)'" onmouseout="this.style.background='transparent'">
                🔌 Disconnect Wallet
            </button>
        </div>
    `;

    document.body.appendChild(menu);
}

function confirmDisconnect() {
    const menu = document.getElementById('walletDropdownMenu');
    if (menu) menu.remove();

    if (window.uiManager) {
        window.uiManager.confirm('Are you sure you want to disconnect your wallet?', 'Disconnect Wallet')
            .then(confirmed => { if (confirmed) disconnectWallet(); });
    } else {
        if (confirm('Are you sure you want to disconnect your wallet?')) {
            disconnectWallet();
        }
    }
}

function promptWalletConnection() {
    const modal = document.getElementById('walletModal');
    if (modal) {
        modal.style.display = 'flex';
    }
}

function closeWalletModal() {
    const modal = document.getElementById('walletModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

// ── Auto-connect when inside a wallet in-app browser ──────
async function autoConnectInAppBrowser() {
    // Wait a moment for the wallet provider to fully initialize
    await new Promise(r => setTimeout(r, 800));

    // Determine which wallet we're inside
    let providerName = null;
    if (window.solflare?.isSolflare) providerName = 'solflare';
    else if (window.phantom?.solana?.isPhantom || window.solana?.isPhantom) providerName = 'phantom';

    if (!providerName) return;

    // Show a connecting overlay so the user sees something is happening
    showInAppConnectingOverlay(providerName);

    try {
        await connectWeb3Wallet(providerName);
    } finally {
        removeInAppConnectingOverlay();
    }
}

function showInAppConnectingOverlay(providerName) {
    const existing = document.getElementById('inAppConnectingOverlay');
    if (existing) return;

    const overlay = document.createElement('div');
    overlay.id = 'inAppConnectingOverlay';
    overlay.style.cssText = `
        position: fixed; inset: 0; z-index: 99999;
        background: rgba(10,14,26,0.97);
        display: flex; flex-direction: column;
        align-items: center; justify-content: center;
        gap: 1.5rem; backdrop-filter: blur(8px);
    `;
    overlay.innerHTML = `
        <div style="font-size: 3rem;">${providerName === 'solflare' ? '☀️' : '👻'}</div>
        <div style="color:white; font-size:1.1rem; font-weight:700;">Connecting to ${providerName === 'solflare' ? 'Solflare' : 'Phantom'}</div>
        <div style="color:rgba(255,255,255,0.6); font-size:0.875rem; text-align:center; max-width:280px;">
            Please approve the connection request within your wallet
        </div>
        <div class="spinner" style="width:32px; height:32px; border-width:3px;"></div>
    `;
    document.body.appendChild(overlay);
}

function removeInAppConnectingOverlay() {
    document.getElementById('inAppConnectingOverlay')?.remove();
}

// ── Mobile deep-link URLs ──────────────────────────────
function buildDappUrl() {
    const base = window.location.origin + window.location.pathname;
    return base;
}

function getMobileDeepLink(providerName) {
    const dappUrl = encodeURIComponent(buildDappUrl());

    if (providerName === 'phantom') {
        return {
            // Phantom v2 universal link for in-app browsing
            deepLink: `https://phantom.app/ul/browse/${dappUrl}`,
            fallbackScheme: `phantom://browse/${dappUrl}`,
            appStoreIOS: 'https://apps.apple.com/app/phantom-crypto-wallet/id1598432977',
            appStoreAndroid: 'https://play.google.com/store/apps/details?id=app.phantom'
        };
    } else if (providerName === 'solflare') {
        return {
            // Solflare universal link for in-app browsing
            deepLink: `https://solflare.com/ul/v1/browse/${dappUrl}`,
            fallbackScheme: `solflare://ul/v1/browse/${dappUrl}`,
            appStoreIOS: 'https://apps.apple.com/app/solflare/id1580902717',
            appStoreAndroid: 'https://play.google.com/store/apps/details?id=com.solflare.mobile'
        };
    }
    return null;
}

function redirectToMobileWallet(providerName) {
    const links = getMobileDeepLink(providerName);
    if (!links) return;

    // Mark that we are attempting a deep link so we can detect return
    sessionStorage.setItem('walletDeepLinkAttempt', providerName);
    sessionStorage.setItem('walletDeepLinkTime', Date.now().toString());

    // Use visibility API to detect if the wallet app actually opened.
    // If the page becomes hidden (app opened), do NOT redirect to store.
    let didLeave = false;

    const onVisibilityChange = () => {
        if (document.hidden) {
            didLeave = true;
            document.removeEventListener('visibilitychange', onVisibilityChange);
        }
    };
    document.addEventListener('visibilitychange', onVisibilityChange);

    // Try the custom scheme first (more reliable on Android for installed apps).
    // Then fall back to universal link.
    const isAndroid = /Android/i.test(navigator.userAgent);

    if (isAndroid && links.fallbackScheme) {
        // On Android, use an intent that can gracefully fall back
        window.location.href = links.fallbackScheme;
    } else {
        // On iOS or as primary attempt, use universal link
        window.location.href = links.deepLink;
    }

    // Only redirect to store if the app didn't open (page stayed visible)
    setTimeout(() => {
        document.removeEventListener('visibilitychange', onVisibilityChange);

        // If the user left the page (app opened), don't go to store
        if (didLeave) return;

        // If page is currently hidden (user switched to app), don't go to store
        if (document.hidden) return;

        // App didn't open — offer to install
        const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);
        const storeUrl = isIOS ? links.appStoreIOS : links.appStoreAndroid;

        if (typeof showNotification === 'function') {
            showNotification(`${providerName === 'phantom' ? 'Phantom' : 'Solflare'} app not found. Redirecting to install...`, 'info');
        }

        // Small delay so notification is visible
        setTimeout(() => {
            window.location.href = storeUrl;
        }, 800);
    }, 2500);
}

// Handle return from a wallet deep link.
// When the user opens Solflare/Phantom and then comes back to the browser,
// we should try to detect their wallet provider if present.
function handleDeepLinkReturn() {
    const attempt = sessionStorage.getItem('walletDeepLinkAttempt');
    const time = sessionStorage.getItem('walletDeepLinkTime');

    if (!attempt || !time) return;

    // Only act on recent attempts (within last 5 minutes)
    const elapsed = Date.now() - parseInt(time);
    if (elapsed > 5 * 60 * 1000) {
        sessionStorage.removeItem('walletDeepLinkAttempt');
        sessionStorage.removeItem('walletDeepLinkTime');
        return;
    }

    // Clean up
    sessionStorage.removeItem('walletDeepLinkAttempt');
    sessionStorage.removeItem('walletDeepLinkTime');

    // If we're now inside an in-app browser, auto-connect
    if (isInAppBrowser()) {
        setTimeout(() => {
            connectWeb3Wallet(attempt);
        }, 500);
    }
}

// ── Main connection flow ───────────────────────────────
async function connectWeb3Wallet(providerName) {
    let clickedBtn = null;
    let originalBtnHtml = '';

    try {
        // Find the button that was clicked to apply a loading state
        const buttons = document.querySelectorAll('.btn');
        for (let b of buttons) {
            const onclickAttr = b.getAttribute('onclick');
            if (onclickAttr && onclickAttr.includes(`connectWeb3Wallet('${providerName}')`)) {
                clickedBtn = b;
                originalBtnHtml = b.innerHTML;
                b.innerHTML = `<span class="spinner" style="display:inline-block; width:16px; height:16px; border-width:2px; vertical-align:middle; margin-right:8px; border-color:currentColor; border-right-color:transparent;"></span> Connecting...`;
                b.disabled = true;
                break;
            }
        }

        // ── Mobile path: deep-link to native wallet app ──
        if (isMobile() && !isInAppBrowser()) {
            if (clickedBtn) {
                clickedBtn.innerHTML = originalBtnHtml;
                clickedBtn.disabled = false;
            }
            closeWalletModal();
            redirectToMobileWallet(providerName);
            return;
        }

        // ── Desktop / in-app browser path ──

        // Use eagerly detected provider (no polling needed here)
        let provider = null;
        if (providerName === 'phantom') {
            provider = _phantomProvider;
        } else if (providerName === 'solflare') {
            provider = _solflareProvider;
        }

        // If eager detection hasn't finished yet, do a quick final check
        if (!provider) {
            if (providerName === 'solflare' && window.solflare?.isSolflare) {
                provider = window.solflare;
            } else if (providerName === 'phantom') {
                const p = window.phantom?.solana || window.solana;
                if (p?.isPhantom) provider = p;
            }
        }

        // If still no provider and we're in an in-app browser, wait a bit longer
        if (!provider && isInAppBrowser()) {
            for (let i = 0; i < 20; i++) {
                await new Promise(r => setTimeout(r, 200));
                if (providerName === 'solflare' && window.solflare?.isSolflare) {
                    provider = window.solflare; break;
                } else if (providerName === 'phantom') {
                    const p = window.phantom?.solana || window.solana;
                    if (p?.isPhantom) { provider = p; break; }
                }
            }
        }

        if (!provider) {
            if (clickedBtn) {
                clickedBtn.innerHTML = originalBtnHtml;
                clickedBtn.disabled = false;
            }

            if (isMobile()) {
                // On mobile without provider = app not installed
                redirectToMobileWallet(providerName);
            } else {
                // Desktop — open download page
                if (providerName === 'solflare') {
                    window.open('https://solflare.com/download', '_blank');
                } else if (providerName === 'phantom') {
                    window.open('https://phantom.app/download', '_blank');
                }
                if (typeof showNotification === 'function') {
                    showNotification(`${providerName === 'phantom' ? 'Phantom' : 'Solflare'} extension not detected. Please install it and refresh the page.`, 'warning');
                }
            }
            return;
        }

        // 1. Connect
        const resp = await provider.connect();

        let pubKeyObj = (resp && resp.publicKey) ? resp.publicKey : provider.publicKey;
        if (!pubKeyObj) {
            throw new Error("Wallet connected but public key could not be retrieved. Ensure it is unlocked.");
        }

        const address = typeof pubKeyObj.toString === 'function' ? pubKeyObj.toString() : String(pubKeyObj);

        // Update button text
        if (clickedBtn) {
            clickedBtn.innerHTML = `<span class="spinner" style="display:inline-block; width:16px; height:16px; border-width:2px; vertical-align:middle; margin-right:8px; border-color:currentColor; border-right-color:transparent;"></span> Please Sign...`;
        }

        // 2. Request message signature to verify ownership
        const msg = `Sign this message to authenticate with RepEngine.\n\nTimestamp: ${Date.now()}`;
        const encodedMessage = new TextEncoder().encode(msg);

        const signedMessage = await provider.signMessage(encodedMessage, "utf8");
        if (!signedMessage) {
            throw new Error("Message signature failed or was rejected by user.");
        }

        // 3. Authenticated successfully
        setWalletConnectedState(address);
        closeWalletModal();

        if (typeof showNotification === 'function') {
            showNotification('Wallet connected successfully! 🎉', 'success');
        }

    } catch (err) {
        console.error("Wallet error:", err);
        if (err.message && err.message.toLowerCase().includes("rejected")) {
            console.log("User rejected the request.");
            if (typeof showNotification === 'function') {
                showNotification('Connection cancelled.', 'info');
            }
        } else {
            if (typeof showNotification === 'function') {
                showNotification("Connection failed: " + err.message, "error");
            } else {
                alert("Connection failed: " + err.message);
            }
        }
    } finally {
        if (clickedBtn) {
            clickedBtn.innerHTML = originalBtnHtml;
            clickedBtn.disabled = false;
        }
    }
}

function setWalletConnectedState(walletAddress) {
    currentWallet = walletAddress;
    localStorage.setItem('connectedWallet', walletAddress);

    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnTxt = document.getElementById('walletBtnText');
    if (walletBtn && walletBtnTxt) {
        walletBtn.classList.add('connected');
        walletBtnTxt.textContent = truncateAddress(walletAddress);
    }

    const desktopBtn = document.getElementById('walletConnectBtnDesktop');
    const desktopBtnTxt = document.getElementById('walletBtnTextDesktop');
    if (desktopBtn && desktopBtnTxt) {
        desktopBtn.classList.add('connected');
        desktopBtnTxt.textContent = truncateAddress(walletAddress);
    }

    window.dispatchEvent(new CustomEvent('walletConnected', { detail: { wallet: walletAddress } }));
    console.log('Wallet connected:', walletAddress);
}

function disconnectWallet() {
    currentWallet = null;
    localStorage.removeItem('connectedWallet');

    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnTxt = document.getElementById('walletBtnText');
    if (walletBtn && walletBtnTxt) {
        walletBtn.classList.remove('connected');
        walletBtnTxt.textContent = 'Connect';
    }

    const desktopBtn = document.getElementById('walletConnectBtnDesktop');
    const desktopBtnTxt = document.getElementById('walletBtnTextDesktop');
    if (desktopBtn && desktopBtnTxt) {
        desktopBtn.classList.remove('connected');
        desktopBtnTxt.textContent = 'Connect Wallet';
    }

    window.dispatchEvent(new CustomEvent('walletDisconnected'));
    console.log('Wallet disconnected');

    if (typeof showNotification === 'function') {
        showNotification('Wallet disconnected.', 'info');
    }
}

function truncateAddress(address) {
    if (!address || address.length <= 12) return address;
    return `${address.substring(0, 6)}...${address.substring(address.length - 4)}`;
}

function getCurrentWallet() { return currentWallet; }

// Export globally
window.walletManager = { getCurrentWallet, disconnectWallet, promptWalletConnection };
