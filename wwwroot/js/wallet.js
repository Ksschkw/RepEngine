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
    // Detect if we're inside a wallet's in-app browser
    return !!(window.phantom?.solana?.isPhantom || window.solflare?.isSolflare);
}

// ── Eager provider detection on page load ──────────────
async function detectProviders() {
    // Wallet extensions inject globals asynchronously.
    // Poll for up to 3 seconds so providers are ready before user clicks.
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
    // Start eager detection immediately (runs in background)
    detectProviders();

    // Check if wallet was previously connected
    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        setWalletConnectedState(savedWallet);
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
    // Remove existing menu
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

    // Position near the anchor
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
        <a href="/Dashboard" onclick="switchTab && switchTab('reputation')" style="display:flex; align-items:center; gap:8px; padding:0.6rem 1rem; border-radius:8px; text-decoration:none; color:var(--text-primary, #fff); font-size:0.875rem; transition:background 0.15s;"
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

// ── Mobile deep-link URLs ──────────────────────────────
function getMobileDeepLink(providerName) {
    const dappUrl = encodeURIComponent(window.location.href);

    if (providerName === 'phantom') {
        // Phantom universal link — opens app or falls back to web
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

    // Try the universal deep-link first (opens the app's in-app browser).
    // This works on both iOS and Android because both Phantom and Solflare
    // have registered universal links for their domains.
    window.location.href = links.deepLink;

    // If after 2 seconds nothing happened (app not installed),
    // redirect to the appropriate app store.
    setTimeout(() => {
        // Check if we're still on this page (app didn't open)
        const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);
        const storeUrl = isIOS ? links.appStoreIOS : links.appStoreAndroid;
        window.location.href = storeUrl;
    }, 2000);
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
            // Restore button *before* navigating away
            if (clickedBtn) {
                clickedBtn.innerHTML = originalBtnHtml;
                clickedBtn.disabled = false;
            }
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

        if (!provider) {
            // No extension found on desktop — redirect to download page
            if (clickedBtn) {
                clickedBtn.innerHTML = originalBtnHtml;
                clickedBtn.disabled = false;
            }

            if (providerName === 'solflare') {
                window.open('https://solflare.com/download', '_blank');
            } else if (providerName === 'phantom') {
                window.open('https://phantom.app/download', '_blank');
            }

            if (typeof showNotification === 'function') {
                showNotification(`${providerName === 'phantom' ? 'Phantom' : 'Solflare'} extension not detected. Please install it and refresh the page.`, 'warning');
            }
            return;
        }

        // 1. Connect
        const resp = await provider.connect();

        // Safely extract public key
        let pubKeyObj = (resp && resp.publicKey) ? resp.publicKey : provider.publicKey;
        if (!pubKeyObj) {
            throw new Error("Wallet connected but public key could not be retrieved. Ensure it is unlocked.");
        }

        const address = typeof pubKeyObj.toString === 'function' ? pubKeyObj.toString() : String(pubKeyObj);

        // Update button text to indicate we are waiting for signature
        if (clickedBtn) {
            clickedBtn.innerHTML = `<span class="spinner" style="display:inline-block; width:16px; height:16px; border-width:2px; vertical-align:middle; margin-right:8px; border-color:currentColor; border-right-color:transparent;"></span> Please Sign...`;
        }

        // 2. Request cryptographically signed message to verify ownership
        const msg = `Sign this message to authenticate with RepEngine.\n\nTimestamp: ${Date.now()}`;
        const encodedMessage = new TextEncoder().encode(msg);

        const signedMessage = await provider.signMessage(encodedMessage, "utf8");
        if (!signedMessage) {
            throw new Error("Message signature failed or was rejected by user.");
        }

        // 3. Authenticated successfully
        setWalletConnectedState(address);
        closeWalletModal();

    } catch (err) {
        console.error("Wallet error:", err);
        // Don't alert if the user simply cancelled the pop-up
        if (err.message && err.message.toLowerCase().includes("rejected")) {
            console.log("User rejected the request.");
        } else {
            if (typeof showNotification === 'function') {
                showNotification("Authentication failed: " + err.message, "error");
            } else {
                alert("Authentication failed: " + err.message);
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

    // Update mobile button
    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnTxt = document.getElementById('walletBtnText');
    if (walletBtn && walletBtnTxt) {
        walletBtn.classList.add('connected');
        walletBtnTxt.textContent = truncateAddress(walletAddress);
    }

    // Update desktop button
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
}

function truncateAddress(address) {
    if (!address || address.length <= 12) return address;
    return `${address.substring(0, 6)}...${address.substring(address.length - 4)}`;
}

function getCurrentWallet() { return currentWallet; }

// Export globally for the shared context
window.walletManager = { getCurrentWallet, disconnectWallet, promptWalletConnection };
