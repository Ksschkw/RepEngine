// ═══════════════════════════════════════════════════════
// Wallet Management — RepEngine
// Supports:
//   Desktop: browser extension connect (Phantom/Solflare)
//   Mobile:  encrypted deep link API (Phantom/Solflare)
// ═══════════════════════════════════════════════════════

let currentWallet = null;
let _phantomProvider = null;
let _solflareProvider = null;

// ── Helpers ────────────────────────────────────────────
function isMobile() {
    return /Android|iPhone|iPad|iPod|webOS|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
}

function isInAppBrowser() {
    return !!(window.phantom?.solana?.isPhantom || window.solflare?.isSolflare);
}

// ── Base58 encode/decode (Bitcoin alphabet) ────────────
const BS58_ALPHA = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';

function bs58encode(bytes) {
    const digits = [0];
    for (let i = 0; i < bytes.length; i++) {
        let carry = bytes[i];
        for (let j = 0; j < digits.length; j++) {
            carry += digits[j] << 8;
            digits[j] = carry % 58;
            carry = (carry / 58) | 0;
        }
        while (carry) { digits.push(carry % 58); carry = (carry / 58) | 0; }
    }
    let str = '';
    for (let i = 0; i < bytes.length && bytes[i] === 0; i++) str += BS58_ALPHA[0];
    for (let i = digits.length - 1; i >= 0; i--) str += BS58_ALPHA[digits[i]];
    return str;
}

function bs58decode(str) {
    const bytes = [0];
    for (let i = 0; i < str.length; i++) {
        const c = BS58_ALPHA.indexOf(str[i]);
        if (c < 0) throw new Error('Invalid base58 character');
        let carry = c;
        for (let j = 0; j < bytes.length; j++) {
            carry += bytes[j] * 58;
            bytes[j] = carry & 0xff;
            carry >>= 8;
        }
        while (carry) { bytes.push(carry & 0xff); carry >>= 8; }
    }
    for (let i = 0; i < str.length && str[i] === BS58_ALPHA[0]; i++) bytes.push(0);
    return new Uint8Array(bytes.reverse());
}

// ── Provider detection ─────────────────────────────────
async function detectProviders(maxMs = 3000) {
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

// ═══════════════════════════════════════════════════════
// INITIALIZATION
// ═══════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', async () => {

    // 1. Check if returning from a deep link redirect FIRST
    const deepLinkResult = handleDeepLinkReturn();
    if (deepLinkResult) return; // Deep link handled, don't do anything else

    // 2. Restore saved wallet
    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        setWalletConnectedState(savedWallet);
    }

    // 3. If inside wallet's in-app browser, auto-connect
    await detectProviders(2000);
    if (!savedWallet && isInAppBrowser()) {
        autoConnectInAppBrowser();
    }

    // 4. Set up click handlers
    document.getElementById('walletConnectBtn')?.addEventListener('click', () => {
        if (currentWallet) showMobileWalletSheet();
        else promptWalletConnection();
    });

    document.getElementById('walletConnectBtnDesktop')?.addEventListener('click', (e) => {
        if (currentWallet) showWalletMenu(e.currentTarget);
        else promptWalletConnection();
    });

    document.addEventListener('click', (e) => {
        const menu = document.getElementById('walletDropdownMenu');
        if (menu && !menu.contains(e.target) && !e.target.closest('#walletConnectBtnDesktop')) {
            menu.remove();
        }
    });
});

// ═══════════════════════════════════════════════════════
// DEEP LINK CONNECT API (Phantom & Solflare)
// ═══════════════════════════════════════════════════════
// This is the encrypted app-to-app communication protocol.
// 1. We generate a NaCl box keypair
// 2. Send our public key + redirect URL to the wallet via deep link
// 3. User approves in their wallet app
// 4. Wallet redirects back to our URL with encrypted response
// 5. We decrypt using our secret key + their public key -> get wallet address

function startDeepLinkConnect(providerName) {
    if (typeof nacl === 'undefined') {
        alert('Crypto library not loaded. Please check your internet connection and refresh.');
        return;
    }

    // Generate ephemeral keypair for this session
    const keypair = nacl.box.keyPair();

    // Store in localStorage (survives redirects between apps)
    localStorage.setItem('_dl_secret', bs58encode(keypair.secretKey));
    localStorage.setItem('_dl_public', bs58encode(keypair.publicKey));
    localStorage.setItem('_dl_provider', providerName);
    localStorage.setItem('_dl_time', Date.now().toString());

    const dappPublicKey = bs58encode(keypair.publicKey);
    const appUrl = encodeURIComponent(window.location.origin);
    const redirectLink = encodeURIComponent(window.location.origin + window.location.pathname);

    let connectUrl;
    if (providerName === 'phantom') {
        connectUrl = `https://phantom.app/ul/v1/connect`
            + `?app_url=${appUrl}`
            + `&dapp_encryption_public_key=${dappPublicKey}`
            + `&redirect_link=${redirectLink}`
            + `&cluster=mainnet-beta`;
    } else {
        // Solflare uses the same deep link API format
        connectUrl = `https://solflare.com/ul/v1/connect`
            + `?app_url=${appUrl}`
            + `&dapp_encryption_public_key=${dappPublicKey}`
            + `&redirect_link=${redirectLink}`
            + `&cluster=mainnet-beta`;
    }

    // Navigate to the wallet — this opens the wallet app and shows the connect prompt
    window.location.href = connectUrl;
}

// Called on page load to check if we're returning from a wallet deep link
function handleDeepLinkReturn() {
    const params = new URLSearchParams(window.location.search);

    // Check for error (user rejected)
    const errorCode = params.get('errorCode');
    if (errorCode) {
        cleanupDeepLinkData();
        cleanUrl();
        setTimeout(() => {
            if (typeof showNotification === 'function') {
                showNotification('Connection was cancelled.', 'info');
            }
        }, 300);
        return true;
    }

    // Check for success params
    // Phantom returns: phantom_encryption_public_key, nonce, data
    // Solflare returns: solflare_encryption_public_key, nonce, data  (or sometimes just the same format)
    const phantomPubKey = params.get('phantom_encryption_public_key');
    const solflarePubKey = params.get('solflare_encryption_public_key');
    const nonce = params.get('nonce');
    const data = params.get('data');

    const walletEncryptionPubKey = phantomPubKey || solflarePubKey;

    if (!walletEncryptionPubKey || !nonce || !data) return false; // Not a deep link return

    // Retrieve our stored secret key
    const secretKeyStr = localStorage.getItem('_dl_secret');
    const providerName = localStorage.getItem('_dl_provider') || 'phantom';

    if (!secretKeyStr) {
        console.error('Deep link return but no stored secret key. Session may have been lost.');
        cleanUrl();
        return true;
    }

    try {
        const ourSecretKey = bs58decode(secretKeyStr);
        const theirPublicKey = bs58decode(walletEncryptionPubKey);
        const nonceBytes = bs58decode(nonce);
        const encryptedData = bs58decode(data);

        // Derive shared secret
        const sharedSecret = nacl.box.before(theirPublicKey, ourSecretKey);

        // Decrypt
        const decrypted = nacl.box.open.after(encryptedData, nonceBytes, sharedSecret);

        if (!decrypted) {
            throw new Error('Failed to decrypt wallet response');
        }

        const json = JSON.parse(new TextDecoder().decode(decrypted));
        // json contains: { public_key: "base58Address", session: "..." }

        if (json.public_key) {
            // Store session for potential future sign requests
            if (json.session) {
                localStorage.setItem('_dl_session', json.session);
                localStorage.setItem('_dl_wallet_pubkey', walletEncryptionPubKey);
            }

            setWalletConnectedState(json.public_key);

            setTimeout(() => {
                if (typeof showNotification === 'function') {
                    showNotification('Wallet connected! 🎉', 'success');
                }
            }, 300);
        }
    } catch (err) {
        console.error('Deep link decryption error:', err);
        setTimeout(() => {
            if (typeof showNotification === 'function') {
                showNotification('Connection failed: ' + err.message, 'error');
            }
        }, 300);
    } finally {
        cleanupDeepLinkData();
        cleanUrl();
    }

    return true;
}

function cleanupDeepLinkData() {
    localStorage.removeItem('_dl_secret');
    localStorage.removeItem('_dl_public');
    localStorage.removeItem('_dl_provider');
    localStorage.removeItem('_dl_time');
}

function cleanUrl() {
    // Remove deep link params from URL bar
    const clean = window.location.origin + window.location.pathname;
    window.history.replaceState({}, document.title, clean);
}

// ═══════════════════════════════════════════════════════
// IN-APP BROWSER AUTO-CONNECT
// ═══════════════════════════════════════════════════════
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
            Please approve the connection and sign the message
        </div>
        <div class="spinner" style="width:32px;height:32px;border-width:3px;"></div>
    `;
    document.body.appendChild(el);
}

function removeInAppOverlay() {
    document.getElementById('inAppOverlay')?.remove();
}

// ═══════════════════════════════════════════════════════
// MOBILE BOTTOM SHEET (wallet menu)
// ═══════════════════════════════════════════════════════
function showMobileWalletSheet() {
    if (document.getElementById('walletSheet')) return;

    const sheet = document.createElement('div');
    sheet.id = 'walletSheet';

    const backdrop = document.createElement('div');
    backdrop.style.cssText = `position:fixed;inset:0;z-index:10000;background:rgba(0,0,0,0.6);backdrop-filter:blur(4px);`;
    backdrop.onclick = () => sheet.remove();

    const panel = document.createElement('div');
    panel.style.cssText = `
        position:fixed;bottom:0;left:0;right:0;z-index:10001;
        background:var(--bg-secondary,#1a1f2e);
        border-radius:20px 20px 0 0;
        padding:0.75rem 1.5rem calc(env(safe-area-inset-bottom, 0px) + 2rem);
        box-shadow:0 -8px 40px rgba(0,0,0,0.5);
        animation:sheetSlideUp 0.25s cubic-bezier(0.32,0.72,0,1);
    `;

    if (!document.getElementById('walletSheetStyle')) {
        const style = document.createElement('style');
        style.id = 'walletSheetStyle';
        style.textContent = `@keyframes sheetSlideUp{from{transform:translateY(100%)}to{transform:translateY(0)}}`;
        document.head.appendChild(style);
    }

    const shortAddr = truncateAddress(currentWallet);
    panel.innerHTML = `
        <div style="width:40px;height:4px;background:rgba(255,255,255,0.2);border-radius:2px;margin:0 auto 1.25rem;"></div>
        <div style="background:rgba(255,255,255,0.05);border:1px solid rgba(255,255,255,0.08);border-radius:12px;padding:1rem;margin-bottom:1rem;display:flex;align-items:center;gap:12px;">
            <div style="width:36px;height:36px;border-radius:50%;background:linear-gradient(135deg,#7c3aed,#4f46e5);display:flex;align-items:center;justify-content:center;font-size:1rem;">🔗</div>
            <div>
                <div style="color:#fff;font-weight:700;font-size:0.9rem;">${shortAddr}</div>
                <div style="color:rgba(255,255,255,0.5);font-size:0.75rem;">Connected Wallet</div>
            </div>
        </div>
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

// ═══════════════════════════════════════════════════════
// DESKTOP DROPDOWN
// ═══════════════════════════════════════════════════════
function showWalletMenu(anchor) {
    const existing = document.getElementById('walletDropdownMenu');
    if (existing) { existing.remove(); return; }

    const menu = document.createElement('div');
    menu.id = 'walletDropdownMenu';
    menu.style.cssText = `
        position:fixed;z-index:10001;
        background:var(--bg-secondary,#1a1f2e);border:1px solid rgba(255,255,255,0.1);
        border-radius:12px;padding:0.5rem;min-width:200px;
        box-shadow:0 8px 32px rgba(0,0,0,0.4);backdrop-filter:blur(12px);
        animation:uiScaleUp 0.15s ease-out;
    `;

    const rect = anchor.getBoundingClientRect();
    menu.style.top = (rect.bottom + 8) + 'px';
    menu.style.right = Math.max(8, window.innerWidth - rect.right) + 'px';

    const shortAddr = truncateAddress(currentWallet);
    menu.innerHTML = `
        <div style="padding:0.75rem 1rem;border-bottom:1px solid rgba(255,255,255,0.08);margin-bottom:0.25rem;">
            <div style="font-weight:700;font-size:0.9rem;color:var(--text-primary,#fff);">🔗 ${shortAddr}</div>
            <div style="font-size:0.75rem;color:var(--text-muted,#888);margin-top:2px;">Connected Wallet</div>
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

// ═══════════════════════════════════════════════════════
// WALLET MODAL & MAIN CONNECT FLOW
// ═══════════════════════════════════════════════════════
function promptWalletConnection() {
    const modal = document.getElementById('walletModal');
    if (modal) modal.style.display = 'flex';
}

function closeWalletModal() {
    const modal = document.getElementById('walletModal');
    if (modal) modal.style.display = 'none';
}

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

        // ── MOBILE PATH: use deep link API ──
        if (isMobile() && !isInAppBrowser()) {
            if (clickedBtn) { clickedBtn.innerHTML = originalBtnHtml; clickedBtn.disabled = false; }
            closeWalletModal();
            startDeepLinkConnect(providerName);
            return;
        }

        // ── DESKTOP / IN-APP BROWSER: use provider.connect() ──
        let provider = providerName === 'phantom' ? _phantomProvider : _solflareProvider;

        if (!provider) {
            if (providerName === 'solflare' && window.solflare?.isSolflare) provider = window.solflare;
            else if (providerName === 'phantom') {
                const p = window.phantom?.solana || window.solana;
                if (p?.isPhantom) provider = p;
            }
        }

        // Extra wait for in-app browser
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
                startDeepLinkConnect(providerName);
            } else {
                window.open(providerName === 'phantom' ? 'https://phantom.app/download' : 'https://solflare.com/download', '_blank');
                if (typeof showNotification === 'function') showNotification(`Please install ${providerName} and refresh.`, 'warning');
            }
            return;
        }

        // Connect
        const resp = await provider.connect();
        let pubKeyObj = resp?.publicKey || provider.publicKey;
        if (!pubKeyObj) throw new Error("Public key not available. Unlock your wallet.");
        const address = typeof pubKeyObj.toString === 'function' ? pubKeyObj.toString() : String(pubKeyObj);

        if (clickedBtn) clickedBtn.innerHTML = `<span class="spinner" style="display:inline-block;width:16px;height:16px;border-width:2px;vertical-align:middle;margin-right:8px;border-color:currentColor;border-right-color:transparent;"></span> Sign Message...`;

        // Sign message to verify ownership
        const msg = `Sign to authenticate with RepEngine.\n\nNonce: ${Date.now()}`;
        const signedMessage = await provider.signMessage(new TextEncoder().encode(msg), "utf8");
        if (!signedMessage) throw new Error("Signature rejected.");

        setWalletConnectedState(address);
        closeWalletModal();
        if (typeof showNotification === 'function') showNotification('Wallet connected! 🎉', 'success');

    } catch (err) {
        console.error("Wallet error:", err);
        const rejected = err.message?.toLowerCase().includes("reject") || err.message?.toLowerCase().includes("cancel") || err.code === 4001;
        if (typeof showNotification === 'function') {
            showNotification(rejected ? 'Connection cancelled.' : 'Error: ' + err.message, rejected ? 'info' : 'error');
        }
    } finally {
        if (clickedBtn) { clickedBtn.innerHTML = originalBtnHtml; clickedBtn.disabled = false; }
    }
}

// ═══════════════════════════════════════════════════════
// STATE MANAGEMENT
// ═══════════════════════════════════════════════════════
function setWalletConnectedState(walletAddress) {
    currentWallet = walletAddress;
    localStorage.setItem('connectedWallet', walletAddress);

    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnTxt = document.getElementById('walletBtnText');
    if (walletBtn && walletBtnTxt) { walletBtn.classList.add('connected'); walletBtnTxt.textContent = truncateAddress(walletAddress); }

    const desktopBtn = document.getElementById('walletConnectBtnDesktop');
    const desktopBtnTxt = document.getElementById('walletBtnTextDesktop');
    if (desktopBtn && desktopBtnTxt) { desktopBtn.classList.add('connected'); desktopBtnTxt.textContent = truncateAddress(walletAddress); }

    window.dispatchEvent(new CustomEvent('walletConnected', { detail: { wallet: walletAddress } }));
}

function disconnectWallet() {
    currentWallet = null;
    localStorage.removeItem('connectedWallet');
    localStorage.removeItem('_dl_session');
    localStorage.removeItem('_dl_wallet_pubkey');

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
