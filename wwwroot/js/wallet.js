// Wallet Management
let currentWallet = null;

// Initialize wallet connection
document.addEventListener('DOMContentLoaded', () => {
    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnText = document.getElementById('walletBtnText');
    
    // Check if wallet was previously connected
    const savedWallet = localStorage.getItem('connectedWallet');
    if (savedWallet) {
        connectWallet(savedWallet);
    }
    
    walletBtn?.addEventListener('click', async () => {
        if (currentWallet) {
            disconnectWallet();
        } else {
            await promptWalletConnection();
        }
    });
});

async function promptWalletConnection() {
    // For demo purposes, we'll use a simple prompt
    // In production, this would integrate with Phantom/Solflare wallet adapters
    const wallet = prompt('Enter your Solana wallet address (or leave empty for demo):');
    
    if (wallet === null) return; // User cancelled
    
    const walletAddress = wallet.trim() || generateDemoWallet();
    connectWallet(walletAddress);
}

function connectWallet(walletAddress) {
    currentWallet = walletAddress;
    localStorage.setItem('connectedWallet', walletAddress);
    
    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnText = document.getElementById('walletBtnText');
    
    if (walletBtn && walletBtnText) {
        walletBtn.classList.add('connected');
        walletBtnText.textContent = truncateAddress(walletAddress);
    }
    
    // Dispatch custom event for other components to listen to
    window.dispatchEvent(new CustomEvent('walletConnected', { detail: { wallet: walletAddress } }));
    
    console.log('Wallet connected:', walletAddress);
}

function disconnectWallet() {
    currentWallet = null;
    localStorage.removeItem('connectedWallet');
    
    const walletBtn = document.getElementById('walletConnectBtn');
    const walletBtnText = document.getElementById('walletBtnText');
    
    if (walletBtn && walletBtnText) {
        walletBtn.classList.remove('connected');
        walletBtnText.textContent = 'Connect Wallet';
    }
    
    window.dispatchEvent(new CustomEvent('walletDisconnected'));
    
    console.log('Wallet disconnected');
}

function truncateAddress(address) {
    if (address.length <= 12) return address;
    return `${address.substring(0, 6)}...${address.substring(address.length - 4)}`;
}

function generateDemoWallet() {
    // Generate a demo wallet address for testing
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz123456789';
    let wallet = '';
    for (let i = 0; i < 44; i++) {
        wallet += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return wallet;
}

function getCurrentWallet() {
    return currentWallet;
}

// Export for use in other scripts
window.walletManager = {
    getCurrentWallet,
    connectWallet,
    disconnectWallet
};
