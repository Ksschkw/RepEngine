# RepEngine ⚡

**Reputation-Powered DAO Governance & Freelance Marketplace**

<!-- Built for the [FairScale Hackathon](https://superteam.fun) - Where reputation is the new currency. -->

---

## 🎯 Overview

RepEngine is a production-ready platform that leverages **FairScale's reputation infrastructure** to power two core features:

1. **🗳️ Weighted DAO Governance** - Vote on platform decisions with power scaled by your on-chain reputation
2. **💼 Reputation-Gated Jobs** - Access freelance opportunities based on your FairScore tier

## ✨ Key Features

### Reputation Tiers (6 Levels)
- **Unranked** (0-19): Basic access, 0.5x voting power
- **Bronze** (20-39): Entry-level jobs, 1x voting, 5% fee discount
- **Silver** (40-59): Create proposals, 1.5x voting, 10% discount
- **Gold** (60-79): Premium jobs, 2x voting, 15% discount
- **Platinum** (80-94): Exclusive access, 3x voting, 20% discount
- **Diamond** (95-100): VIP access, 5x voting, 25% discount + revenue share

### DAO Governance
- **Reputation-weighted voting** - Higher FairScore = more influence
- **Tier-based proposal creation** - Silver+ can create proposals
- **Live proposal tracking** - Real-time vote counts and status
- **Sample proposals** - Pre-loaded demo proposals to showcase functionality

### Freelance Marketplace
- **Tier-gated job access** - Premium jobs require higher reputation
- **Dynamic fee discounts** - Better reputation = lower platform fees
- **Job application system** - Apply with cover letter and proposed rate
- **Sample jobs** - 5 pre-loaded jobs across different tiers

### FairScore Integration
- **Deterministic scoring** - Consistent scores based on wallet address (sandbox mode)
- **Score breakdown** - Transaction volume, account age, DeFi activity, governance, social
- **Historical tracking** - 6-month score history visualization
- **Improvement suggestions** - Personalized recommendations to boost score
- **Caching** - 5-minute cache for optimal performance

## 🏗️ Technical Stack

- **Backend**: ASP.NET Core 10.0 (Razor Pages + Minimal APIs)
- **Frontend**: Vanilla HTML/CSS/JavaScript
- **Design**: Custom dark mode with glassmorphism, Inter font, gradient animations
- **API**: FairScale (sandbox/mock mode - free, no API key required)
- **Storage**: In-memory (demo purposes)

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Any modern browser

### Installation

```bash
# Clone the repository
git clone <your-repo-url>
cd RepEngine

# Run the application
dotnet run

# Or use watch mode for development
dotnet watch run
```

The app will be available at `https://localhost:5001`

### First Steps

1. **Connect Wallet** - Click "Connect Wallet" in the navbar
   - Enter a Solana wallet address, or leave empty for a demo wallet
   - Your wallet is saved in localStorage for convenience

2. **Check Your Score** - Visit the homepage and click "Try Live Demo"
   - Enter your wallet or use the connected one
   - View your FairScore, tier, and breakdown

3. **Explore Features**:
   - **Dashboard** - View your reputation stats, history, and improvement suggestions
   - **Governance** - Vote on proposals or create your own (Silver+ tier)
   - **Jobs** - Browse and apply to jobs based on your tier

### PWA Installation (Mobile & Desktop)

RepEngine is a **Progressive Web App** - install it for the best experience!

#### On Mobile (Android/iOS)

1. **Android (Chrome)**:
   - Visit the deployed URL
   - Tap the "Install App" button in the navbar, OR
   - Tap the menu (⋮) → "Add to Home screen"
   - The app will appear on your home screen like a native app

2. **iOS (Safari)**:
   - Visit the deployed URL
   - Tap the Share button (□↑)
   - Scroll down and tap "Add to Home Screen"
   - Tap "Add" in the top right

#### On Desktop

1. **Chrome/Edge**:
   - Visit the deployed URL
   - Click the install icon (⊕) in the address bar, OR
   - Click the "Install App" button in the navbar
   - The app will open in its own window

2. **Benefits of Installing**:
   - ⚡ Faster loading with offline support
   - 📱 Full-screen experience (no browser UI)
   - 🔔 Push notifications (future feature)
   - 💾 Works offline for cached pages

### Mobile Features

- **Responsive Design** - Optimized for all screen sizes (320px - 1920px)
- **Touch-Friendly** - All buttons meet 44px minimum touch target
- **Hamburger Menu** - Clean mobile navigation
- **Offline Support** - Service worker caches pages for offline access
- **Safe Area Support** - Works perfectly on notched devices (iPhone X+)
- **Fast Performance** - Optimized for 3G networks

## 📁 Project Structure

```
RepEngine/
├── Models/                    # Data models
│   ├── FairScoreResponse.cs   # FairScale API response
│   ├── ReputationTier.cs      # Tier system (6 tiers)
│   ├── UserProfile.cs         # User data
│   ├── Proposal.cs            # DAO proposals & votes
│   └── Job.cs                 # Freelance jobs & applications
├── Services/                  # Business logic
│   ├── FairScoreService.cs    # FairScale API integration (mock)
│   ├── ReputationService.cs   # Tier calculations & feature access
│   ├── GovernanceService.cs   # Proposal & voting logic
│   └── JobService.cs          # Job marketplace logic
├── Pages/                     # Razor Pages
│   ├── Index.cshtml           # Homepage with live demo
│   ├── Dashboard.cshtml       # User dashboard
│   ├── Governance.cshtml      # DAO governance
│   └── Jobs.cshtml            # Job marketplace
├── wwwroot/                   # Static assets
│   ├── css/
│   │   ├── site.css           # Design system
│   │   └── components.css     # Component styles
│   └── js/
│       ├── wallet.js          # Wallet management
│       └── site.js            # Global scripts
└── Program.cs                 # App configuration & API endpoints
```

## 🔌 API Endpoints

### FairScore
- `GET /api/fairscore?wallet={address}` - Get FairScore data
- `GET /api/dashboard?wallet={address}` - Get dashboard data

### Governance
- `GET /api/governance/proposals` - List all proposals
- `GET /api/governance/proposals/active` - List active proposals
- `POST /api/governance/proposals` - Create proposal
- `POST /api/governance/vote` - Cast vote

### Jobs
- `GET /api/jobs?wallet={address}` - Get accessible jobs
- `GET /api/jobs/all` - Get all jobs
- `POST /api/jobs/apply` - Apply to job

### Reputation
- `GET /api/tiers` - Get all reputation tiers

## 🎨 Design Philosophy

- **Dark Mode First** - Modern, premium aesthetic
- **Glassmorphism** - Frosted glass cards with backdrop blur
- **Gradient Accents** - Purple/pink gradients for visual interest
- **Micro-animations** - Smooth transitions and hover effects
- **Responsive** - Mobile-friendly grid layouts

## 🔐 FairScale Integration

Currently using **sandbox mode** (free, no API key required):
- Deterministic scoring based on wallet address hash
- Realistic score breakdown across 5 categories
- 6-month historical data generation
- Improvement suggestions

**To use real FairScale API**:
1. Sign up at https://sales.fairscale.xyz/
2. Get your API key
3. Update `appsettings.json`:
   ```json
   "FairScale": {
     "ApiKey": "your-api-key-here",
     "UseMockData": false
   }
   ```

## 💡 Business Model

### Problem
Web3 lacks a unified reputation system. Users can't leverage their on-chain history for governance influence or job opportunities.

### Solution
RepEngine uses FairScale's privacy-first reputation infrastructure to:
- Weight DAO voting by credibility (prevents Sybil attacks)
- Gate job access by proven track record (reduces client risk)
- Reward high-reputation users with lower fees

### Revenue Streams
1. **Platform Fees** - 5% base fee on job transactions (discounted by tier)
2. **Premium Listings** - Featured job postings
3. **Tier Upgrades** - Optional reputation staking (future)

### Go-to-Market
1. **Phase 1** - Launch on Solana ecosystem DAOs
2. **Phase 2** - Partner with freelance platforms (Braintrust, Layer3)
3. **Phase 3** - Expand to multi-chain reputation aggregation

## 📊 Hackathon Submission Checklist

- ✅ **Production-ready app** - Live, functional, deployable
- ✅ **FairScale integration** - Core to product logic (voting power, job access)
- ✅ **Technical excellence** - Clean code, well-documented, scalable architecture
- ✅ **Business viability** - Clear problem/solution, revenue model, GTM strategy
- ✅ **Meaningful use case** - Reputation gates features, not decorative
- ✅ **Demo-ready** - Sample data, interactive features, visual polish

## 🚢 Deployment

### Live Demo

🌐 **[Live App URL - Add your deployment URL here]**

📱 **Install as PWA** - Visit the URL on mobile and add to home screen!

<!-- ### Free Deployment Options

RepEngine can be deployed to these **100% free** platforms:

1. **Railway** (Recommended) - 500 hours/month free
2. **Render** - Free tier with auto-sleep
3. **Fly.io** - Free tier with 3 shared CPUs
4. **Azure App Service** - Free F1 tier -->

<!-- See [DEPLOYMENT_GUIDE.md](.hackathon/DEPLOYMENT_GUIDE.md) for detailed instructions. -->

### Local Production Build
```bash
dotnet publish -c Release
cd bin/Release/net10.0/publish
dotnet RepEngine.dll
```

### Docker Deployment
```bash
# Build image
docker build -t repengine .

# Run container
docker run -p 8080:8080 repengine
```

## 🤝 Contributing

This is a hackathon project, but contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

<!-- ## 📄 License

MIT License - feel free to use this code for your own projects!

## 🙏 Acknowledgments

- **FairScale** - For the reputation infrastructure and hackathon
- **Superteam** - For hosting the bounty
- **Solana** - For the ecosystem -->

---

**Built for lols for the FairScale Hackathon - Excuse to up my ASP.NET game**

*Reputation is the new currency. Build yours on RepEngine.*
