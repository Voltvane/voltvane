# ⚡ Voltvane

**Free, open-source Windows PC optimizer that actually explains itself.**

Most optimization tools apply registry tweaks silently with zero explanation. Voltvane shows you exactly what each tweak does, why it helps, and lets you revert everything with one click.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue?style=flat-square&logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)
![Free](https://img.shields.io/badge/download-free-brightgreen?style=flat-square)

---

## What it does

- **Registry tweaks** — Game DVR, fullscreen optimizations, mouse acceleration, MMCSS scheduling, timer resolution
- **Power plan tuning** — Custom Voltvane power plans for laptops and desktops, minimum processor state fixes
- **Latency tweaks** — Disable Nagle's algorithm, network throttling, GPU/SFIO priority
- **Background service cleanup** — SysMain, Windows Search indexing, telemetry reduction
- **CPU & GPU guides** — ThrottleStop, MSI Afterburner walkthroughs built into the app
- **Everything is reversible** — One click reverts any tweak to Windows defaults
- **Creates a restore point** before the first change

---

## Why open source?

Because you should be able to see exactly what a PC optimizer does to your system before running it. Every tweak in `TweakCatalog.cs` is documented with what it changes, why it helps, and what the revert does.

---

## Download

**[voltvane.com](https://voltvane.com)** — Free installer, includes ThrottleStop and MSI Afterburner.

Pro features (advanced CPU/GPU tuning, more tweaks) available via subscription.

---

## Building from source

**Requirements:**
- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- WebView2 Runtime (usually already installed on Windows 11)

**Build:**
```bash
git clone https://github.com/Voltvane/voltvane.git
cd voltvane
dotnet build
```

**Publish (self-contained):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Output will be in `bin/Release/net8.0-windows10.0.17763.0/win-x64/publish/`

---

## Project structure

```
Voltvane/
├── MainWindow.cs          # Main app window, auth, message handling
├── Program.cs             # Entry point
├── Models/
│   └── Tweak.cs           # Tweak model, enums (RiskLevel, FormFactor, TweakKind)
├── Services/
│   ├── TweakCatalog.cs    # All tweaks defined here — start here to understand what the app does
│   ├── TweakEngine.cs     # Applies/reverts tweaks (registry, powercfg, commands)
│   ├── LicenseService.cs  # Supabase auth and Pro status
│   ├── HardwareDetector.cs# Detects laptop vs desktop
│   ├── StateStore.cs      # Persists which tweaks are enabled
│   └── UpdateService.cs   # Auto-update checker
└── UI/
    └── app.html           # Full frontend (WebView2)
```

---

## Adding a tweak

All tweaks live in `Services/TweakCatalog.cs`. Adding one is straightforward:

```csharp
list.Add(new Tweak
{
    Id = "your_tweak_id",
    Title = "Your tweak title",
    Category = "Latency & Scheduling",
    ShortDescription = "One line explaining what it does.",
    Explanation = "Full explanation shown in the app.",
    Target = FormFactor.Both,        // Both, PC, or Laptop
    Kind = TweakKind.Registry,       // Registry, PowerCfg, Command, or Guide
    Risk = RiskLevel.Safe,           // Safe, Caution, or Advanced
    IsPro = false,
    RecommendedForPC = true,
    RecommendedForLaptop = true,
    RegHive = "HKCU",
    RegPath = @"Your\Registry\Path",
    RegName = "ValueName",
    RegType = "DWORD",
    RegValueApply = "1",
    RegValueRevert = "0"
});
```

---

## License

MIT — do whatever you want with it.

---

## Contributing

PRs welcome. If you know a tweak that's safe, reversible, and genuinely helpful — open a PR with it added to `TweakCatalog.cs` with a proper explanation.

---

*Built by [@Voltvane](https://voltvane.com)*
