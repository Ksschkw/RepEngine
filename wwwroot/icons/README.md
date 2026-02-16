# How to Generate PWA Icons

## Quick Start

1. **Open the Icon Generator**
   - Navigate to: `http://localhost:5000/generate-icons.html` (when running locally)
   - Or open the file directly in your browser: `wwwroot/generate-icons.html`

2. **Generate Icons**
   - Click "🎨 Generate & Download All Icons" button
   - OR wait for auto-prompt on page load

3. **Download**
   - 4 PNG files will automatically download:
     - `icon-192x192.png`
     - `icon-512x512.png`
     - `icon-maskable-512x512.png`
     - `apple-touch-icon.png`

4. **Move Files**
   - Move all downloaded files to: `wwwroot/icons/`
   - Replace any existing placeholder files

## What You Get

Each icon features:
- ⚡ Lightning bolt symbol (RepEngine brand)
- 🎨 Purple-pink gradient background
- ✨ Glowing effects
- 📱 Optimized for mobile home screens

## Icon Sizes

- **192x192px** - Android home screen icon
- **512x512px** - Android splash screen
- **512x512px (maskable)** - Adaptive icon for Android (with safe zone)
- **180x180px** - iOS home screen icon (Apple Touch Icon)

## Verification

After moving the icons:

1. Run your app: `dotnet run`
2. Open Chrome DevTools (F12)
3. Go to **Application** tab
4. Click **Manifest** in left sidebar
5. Verify all icons show correctly

## Troubleshooting

**Icons not showing?**
- Clear browser cache (Ctrl+Shift+Delete)
- Hard refresh (Ctrl+F5)
- Check file paths in `manifest.json`

**Wrong colors?**
- Edit `generate-icons.html` gradient colors
- Regenerate icons

**Need different sizes?**
- Edit the `icons` array in `generate-icons.html`
- Add more sizes as needed

---

**That's it! Your PWA icons are ready! 🎉**
