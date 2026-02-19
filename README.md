# RepEngine

**A Mobile-First, Reputation-Based Governance Platform**

RepEngine is a Progressive Web App (PWA) built on **.NET 10** that integrates the **FairScale API** to weight DAO governance and job market access based on on-chain reputation.

---

## 🏗️ Technical Architecture

### Backend: .NET 10 + Minimal APIs
- **Framework:** ASP.NET Core 10.0 (Razor Pages & Minimal APIs)
- **Zero-Dependency Swagger:** Custom implementation of OpenAPI 3.0 specs served via Minimal APIs (`/swagger/v1/swagger.json`), bypassing heavy NuGet packages to ensure lightweight deployment.
- **Environment Management:** Native support for `.env` files for secure API key management.
- **Caching:** `IMemoryCache` implementation to reduce FairScale API calls and improve performance.

### Frontend: Mobile-First Architecture
- **CSS Architecture:** Zero-framework, vanilla CSS implementation.
  - **Single File:** consolidated `site.css` (~22KB) for maximum performance.
  - **Design Tokens:** CSS variables for switching themes and consistent scaling.
  - **Mobile-First Media Queries:** Base styles target mobile devices; standard desktop navigation loads only on screens `≥ 769px`.
- **UI Components:** Custom "Glassmorphism" card system (`.card`), fixed bottom navigation with SVG assets, and a responsive grid layout.
- **Performance:** **Inter** font loaded via Google Fonts CDN; no heavy JS bundles (jQuery removed).

### PWA & Offline Support
- **Installable:** Fully compliant `manifest.json` allows the app to be installed to the home screen on iOS and Android.
- **Service Worker:** Custom `sw.js` implements a **Stale-While-Revalidate** strategy for static assets and a **Network-First** strategy for API calls.
- **Offline Capability:** Caches critical pages (`/Index`, `/Dashboard`, `/Governance`) and assets to function without network connectivity.

### Integration: FairScale API
- **Real-Time Scoring:** Direct integration with `api2.fairscale.xyz`.
- **DTO Mapping:** Strongly-typed C# models map complex JSON responses (including nested `features` and `badges`) to usable domain objects.
- **Proxy Endpoints:** The backend acts as a secure proxy to FairScale, keeping API keys server-side.

---

## 🚀 Setup & Deployment

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- FairScale API Key

### Local Development

1.  **Clone & Restore**
    ```bash
    git clone https://github.com/yourusername/RepEngine.git
    cd RepEngine
    dotnet restore
    ```

2.  **Environment Configuration**
    Create a `.env` file in the root:
    ```env
    FairScale__ApiKey=zpka_YOUR_KEY
    ```

3.  **Run**
    ```bash
    dotnet run
    ```
    Access the app at `http://localhost:5050`
    API Documentation at `http://localhost:5050/docs`

---

## 📱 Mobile Experience

The application is designed primarily for mobile usage:
- **Navigation:** Thumb-friendly fixed bottom bar.
- **Touch Targets:** Minimum 44px touch targets for all interactive elements.
- **Viewport:** Content respects safe-area insets (notch support).
- **Theme:** Deep dark mode (`#0a0e1a`) optimized for OLED screens.

---

## 🛡️ API Documentation

API documentation is auto-generated and served at `/docs`.
- **UI:** Custom-styled Swagger UI matching the application's dark theme.
- **Spec:** OpenAPI 3.0 compliant JSON available at `/swagger/v1/swagger.json`.
