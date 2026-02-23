# RepEngine

**Enterprise-Grade, Reputation-Powered Web3 Gig Marketplace & DAO Governance**

RepEngine is a Progressive Web App (PWA) built on **.NET 10** and **PostgreSQL** that integrates the **FairScale API** to weight DAO governance and job market access based on on-chain reputation.

![Mobile Preview](image.png)

## 🏗️ Technical Architecture

### Backend: .NET 10 + Entity Framework Core
- **Framework:** ASP.NET Core 10.0 (Razor Pages & Minimal APIs)
- **Database:** PostgreSQL via Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Schema:** 13+ enterprise models including Jobs, Contracts, Milestones, Reviews, Disputes, and Governance Proposals.
- **Zero-Dependency Swagger:** Custom OpenAPI 3.0 implementation served via Minimal APIs (`/docs`), bypassing heavy NuGet packages.
- **Environment Management:** Native support for `.env` files for secure API key and database connection management.

### Frontend: Mobile-First Architecture
- **CSS Architecture:** Zero-framework, vanilla CSS implementation (`site.css` ~22KB).
- **Design Tokens:** CSS variables for switching themes and robust scaling.
- **Mobile-First:** Target mobile devices primarily, standard desktop navigation scaling up for screens `≥ 769px`.
- **UI System:** Custom Glassmorphism cards, responsive grids, built-in modal managers, and PWA install native prompts.

### PWA & Offline Support
- **Installable:** Fully compliant `manifest.json` for iOS and Android home screen installation.
- **Service Worker:** Custom `sw.js` with **Stale-While-Revalidate** strategy for assets and **Network-First** for APIs.
- **Offline Capability:** Caches critical pages and displays offline banners natively.

### Integration: FairScale API
- **Real-Time Scoring:** Direct integration with `api2.fairscale.xyz`.
- **Tier Gating:** Access to specific jobs and DAO voting is locked behind minimum FairScore and Reputation Tiers.

---

## 🚀 Setup & Local Development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL Server (Local or Cloud like Supabase/Neon)
- FairScale API Key

### Local Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/RepEngine.git
   cd RepEngine
   ```

2. **Configure Database & Environment**
   Update your `appsettings.json` with your PostgreSQL Connection String:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=repengine;Username=postgres;Password=yourpassword"
   }
   ```
   *Note: If using Neon or Supabase locally, paste the pooled connection string here.*

   Create a `.env` file in the root for secret keys:
   ```env
   FairScale__ApiKey=zpka_YOUR_KEY
   ```

3. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```
   *Note: The app is configured to auto-migrate on startup in Development mode as well.*

4. **Run the Application**
   ```bash
   dotnet run
   ```
   Access the app at `http://localhost:5000`  
   API Documentation at `http://localhost:5000/docs`

---

## 🌩️ Deployment (Northflank & Neon/Supabase)

RepEngine is designed to be easily deployed to modern cloud platforms like Northflank, paired with a serverless Postgres database.

### 1. Database Setup (Neon or Supabase)
1. Create a new project in **Neon.tech** or **Supabase**.
2. Copy the **Connection String** (URI format, usually starts with `postgresql://`).
3. For Neon, ensure you use the pooled connection string if available.

### 2. Northflank Deployment
1. Log in to [Northflank](https://northflank.com/) and create a new **Service**.
2. Select **Repository** and connect your GitHub repo containing RepEngine.
3. **Build Configuration**:
   - Build Type: **Dockerfile** (Add a standard ASP.NET Core 10.0 Dockerfile to your root, or use Northflank's Nixpacks which automatically detects .NET).
   - If using Nixpacks, Northflank will automatically run `dotnet publish`.
4. **Environment Variables**:
   Add the following variables in the Northflank dashboard:
   - `ConnectionStrings__DefaultConnection` : `[Your Neon/Supabase PostgreSQL URL]`
   - `FairScale__ApiKey` : `[Your FairScale API Key]`
   - `ASPNETCORE_ENVIRONMENT` : `Production`
5. **Ports & Networking**:
   - Expose HTTP Port **8080** (Default for .NET 8+ containers).
6. Click **Deploy**. Northflank will build and host the application seamlessly.

---

## 🛡️ API Documentation

API documentation is auto-generated and served at `/docs`.
- **UI:** Custom-styled Swagger UI matching the application's dark theme.
- **Spec:** OpenAPI 3.0 compliant JSON available at `/swagger/v1/swagger.json`.
