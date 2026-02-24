# ⚡ RepEngine: Trustless Web3 Freelance Marketplace & DAO

**Built for the FairScale Superteam Earn Hackathon**

RepEngine is an enterprise-grade, reputation-powered gig marketplace and DAO governance platform. It solves the massive trust deficit in Web3 freelancing by deeply integrating the **FairScale API** to weight DAO voting and gate job market access based on cryptographic, on-chain reputation.

> **Live Demo:** [Insert Northflank URL Here]
> 
> **Video Pitch:** [Insert YouTube/Loom Link Here]

![RepEngine App Preview](image.png)

---

## 🏆 Hackathon Submission Criteria

### 1. Meaningful FairScore Integration (30%)
FairScore is not a decorative element in RepEngine; it is the core constraint engine of the entire platform:
* **DAO Governance:** Proposals require a minimum FairScore to be created. Voting power is not just 1-wallet-1-vote; it is mathematically strictly weighted by the voter's FairScore.
* **Gated Job Marketplace:** Clients can create "Premium Gigs" demanding exact minimum FairScore thresholds (e.g., minimum score of 80 required to even view the 'Apply' button).
* **Reputation Tiers:** The backend dynamically calculates ranks (`Unranked`, `Bronze`, `Silver`, `Gold`, `Diamond`, `Legend`) based on real-time API polling from FairScale.

### 2. Technical Excellence (25%)
RepEngine was not built as a minimum viable prototype; it is an enterprise-ready C# Web App:
* **Backend:** ASP.NET Core 10.0 + Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`).
* **Schema:** 13 deeply relational Postgres models handling Contracts, Escrow Milestones, Disputes, and Governance logic.
* **Native Mobile Cryptography:** Instead of trapping users in wallet in-app browsers, mobile wallet authentication is handled via `tweetnacl` cryptographic keypairs. It uses the `phantom://` and `solflare://` deep link API specifications to generate ephemeral `NaCl` box encryption, ensuring secure, native, cross-app signatures on iOS and Android.
* **PWA & Offline:** Fully installable mobile PWA with Service Worker `stale-while-revalidate` asset caching and offline network handling.

### 3. Business Viability (15%)
**The Problem:** The Web3 gig economy is plagued by anonymous scammers, unverified portfolios, and rug pulls.
**The Solution:** An escrow-backed contract system where only cryptographically verified, high-reputation (FairScore) actors can apply for high-value bounties.
**Revenue Model:** RepEngine charges a flat 2.5% protocol fee on successfully completed milestones and escrow payouts. Access to post "Legend-tier" jobs requires premium platform credits.

### 4. Traction & Users (20%)
* **Legends.fun:** Check out our FAIRathon leaderboard listing: [Insert Legends.fun URL Here]
* **Engagement:** [Insert Twitter thread link here]

---

## 🚀 Local Setup & Development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL Server 

### 1. Database & Environment Setup
Clone the repo and configure your `appsettings.json` with a PostgreSQL connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=repengine;Username=postgres;Password=local_dev_password"
}
```

Create a `.env` file in the root for secret keys:
```env
FairScale__ApiKey=YOUR_FAIRSCALE_API_KEY
```

### 2. Run Database Migrations
```bash
dotnet ef database update
```
*(Development mode ensures EF Core auto-migrates missing schemas on startup).*

### 3. Run the Application
```bash
dotnet run
```
Access the mobile-first UI at `http://localhost:5000`.
Zero-dependency OpenAPI Swagger Docs are available at `http://localhost:5000/docs`.

---

## 🌩️ Cloud Deployment Architecture
RepEngine is configured for Nixpack/Docker containerization on Northflank paired with Supabase connection pooling:

1. **Supabase Setup:** Ensure you use the **Session Pooler (IPv4)** connection string (`port 6543/5432`) to support EF Core's persistence model on cloud platforms.
2. **Northflank Config:** Provide the connection string and FairScale API key securely as environment variables overriding the `appsettings.json` placeholders.
3. **IPv6 Mitigation:** The provided `Dockerfile` explicitly includes `ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1` to ensure fault-tolerant Supabase resolution.

---
*Built by [Your Name] for the FairScale Superteam Earn Hackathon, February 2026.*
