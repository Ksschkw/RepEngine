// Wallet Management — RepEngine
let currentWallet = null;

// Initialize wallet connection
document.addEventListener('DOMContentLoaded', () => {
    // Check if wallet was previously connected
    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        connectWallet(savedWallet);
    }

    // Mobile top-bar button
    const walletBtn = document.getElementById('walletConnectBtn');
    walletBtn?.addEventListener('click', async () => {
        if (currentWallet) disconnectWallet();
        else await promptWalletConnection();
    });

    // Desktop navbar button — was NEVER wired up before (bug fix)
    const walletBtnDesktop = document.getElementById('walletConnectBtnDesktop');
    walletBtnDesktop?.addEventListener('click', async () => {
        if (currentWallet) disconnectWallet();
        else await promptWalletConnection();
    });
});

async function promptWalletConnection() {
    // For demo: simple prompt. In production → Phantom/Solflare wallet adapters.
    const wallet = prompt('Enter your Solana wallet address (or leave empty for demo):');
    if (wallet === null) return; // User cancelled
    const walletAddress = wallet.trim() || generateDemoWallet();
    connectWallet(walletAddress);
}

function connectWallet(walletAddress) {
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

function generateDemoWallet() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz123456789';
    let wallet = '';
    for (let i = 0; i < 44; i++) {
        wallet += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return wallet;
}

function getCurrentWallet() { return currentWallet; }

// Export for use in other scripts
window.walletManager = { getCurrentWallet, connectWallet, disconnectWallet };
