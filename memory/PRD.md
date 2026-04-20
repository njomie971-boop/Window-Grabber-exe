# Window Grabber — PRD

## Original Problem Statement
Application Windows desktop native (WPF / .NET 8 / C#), 100 % locale, qui :
- détecte tous les moniteurs connectés (résolution, coordonnées virtuelles, principal, type de connexion HDMI/DP/USB-C/DVI/VGA)
- détecte toutes les fenêtres top-level ouvertes (titre, process, moniteur, état, icône, miniature live)
- permet d'un clic de ramener n'importe quelle fenêtre sur le moniteur où Window Grabber est ouvert
- offre une interface sombre moderne (cartes + badges + recherche + tri)
- persiste des paramètres simples
- se build en `.exe` single-file self-contained

## Architecture & Tech Stack
- **C# · .NET 8 · WPF** (`net8.0-windows`, `UseWPF=true`)
- **MVVM** : `ViewModels/` + `Views/` + `INotifyPropertyChanged` custom (pas de framework MVVM externe)
- **Win32 interop** isolé strict dans `Interop/` (seul fichier avec `DllImport`)
- **Logique métier** dans `Services/` (testable, zéro code WPF)
- **Packages** : `System.Management` 8.0.0 (WMI), `System.Drawing.Common` 8.0.7
- **Persistance** : JSON via `System.Text.Json` dans `%APPDATA%\WindowGrabber\`
- **Logging** : logger fichier maison (rotation basique 2 MB)
- **DPI** : `PerMonitorV2` via app.manifest (critique pour multi-écrans)
- **Packaging** : `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`

## User Personas
- **Utilisateur pro multi-écrans** (3+ moniteurs) qui perd régulièrement des fenêtres sur des écrans distants ou fraîchement débranchés et veut les ramener en 1 clic.
- **Développeur / créatif** avec environnement de travail multi-moniteurs complexe.

## Core Requirements (static)
1. Détection fiable des moniteurs (EnumDisplayMonitors)
2. Détection pertinente des fenêtres (EnumWindows + filtrage cloaking/tool/système)
3. Identification du moniteur cible = moniteur qui héberge Window Grabber
4. Déplacement propre des fenêtres vers le moniteur cible (restore si min/max, recentrage, clamp workarea, activation au premier plan)
5. UI sombre, grille de cartes, recherche, tri, badges
6. Paramètres persistés, logs, gestion d'erreur sans crash
7. Build en `.exe` single-file reproductible

## What's Implemented (V1 — 2026-02)
- [x] Architecture modulaire Interop / Models / Services / ViewModels / Views
- [x] `MonitorService` + `ConnectionTypeService` (WMI WmiMonitorConnectionParams, fallback "Inconnu")
- [x] `WindowService` : EnumWindows + DWM cloaking + blacklist classes/process
- [x] `WindowMover` : restore-move-remax + ForceForeground via AttachThreadInput
- [x] `DwmThumbnailHost` : HwndHost pour miniatures live DWM avec fallback icône
- [x] `IconExtractor` : WM_GETICON + GCL + ExtractIconEx
- [x] `SettingsService` : JSON dans `%APPDATA%\WindowGrabber\settings.json`
- [x] `Logger` : rotation 2 MB dans `%APPDATA%\WindowGrabber\logs\`
- [x] MVVM complet : MainViewModel, WindowItemViewModel, SettingsViewModel
- [x] UI sombre : DarkTheme.xaml, MainWindow, SettingsWindow, WindowCard
- [x] Recherche + tri (moniteur / titre / application)
- [x] Actualisation manuelle, recalcul target monitor au drag
- [x] app.manifest avec PerMonitorV2 DPI awareness
- [x] csproj avec PublishSingleFile + SelfContained = true par défaut
- [x] Icône .ico multi-résolutions générée depuis le PNG fourni
- [x] README détaillé avec étapes de build
- [x] Global exception handler (dispatcher + appdomain)

## Not Applicable in This Environment
Testing agent, supervisor, frontend/backend preview URL — Window Grabber est une app Windows native. La compilation et le test doivent être faits par l'utilisateur sur Windows avec `dotnet publish`.

## Backlog / Future

### P1 (features utiles)
- [ ] Placeholder "Rechercher…" dans le TextBox (style trigger avec VisualBrush)
- [ ] Virtualisation de l'ItemsControl pour > 50 fenêtres (VirtualizingWrapPanel)
- [ ] Raccourci global (hotkey) pour invoquer Window Grabber (RegisterHotKey)
- [ ] Tri "dernière utilisée" (via GetLastInputInfo ou Z-order)
- [ ] Preview grande taille en hover d'une carte (panneau latéral)

### P2 (polish)
- [ ] Animations d'entrée staggered sur les cartes
- [ ] Thème clair (actuellement désactivé dans l'UI)
- [ ] Localisation (anglais/français)
- [ ] Glisser-déposer une carte sur un pill de moniteur pour choisir la cible
- [ ] Mémorisation position/taille de la fenêtre de Window Grabber

### P3 (explicite hors scope V1)
- [ ] Remplacement Alt+Tab
- [ ] Hooks clavier complexes
- [ ] Synchronisation cloud

## Limitations Connues
- Fenêtres élevées (admin) non-déplaçables par app non-élevée (UIPI)
- Certaines UWP refusent SetWindowPos (protection OS)
- Type connexion inconnu si driver GPU ne renseigne pas WMI

## Next Tasks
1. L'utilisateur build le projet sur Windows avec `dotnet publish` (cf README)
2. Retour d'expérience sur les cas réels (fenêtres spécifiques qui posent problème)
3. Itérations sur P1 selon retours
