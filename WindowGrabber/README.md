# Window Grabber

**Window Grabber** est un utilitaire Windows desktop (WPF / .NET 8) qui affiche toutes les fenêtres ouvertes sur votre ordinateur — tous moniteurs confondus — et vous permet d'un clic de **ramener n'importe quelle fenêtre sur le moniteur où Window Grabber est lancé**.

100 % local · Hors ligne · Pas de télémétrie · Pas de dépendance internet.

---

## Fonctionnalités V1

- **Détection multi-écrans** : tous les moniteurs connectés, résolution, coordonnées virtuelles, moniteur principal, moniteur cible (celui où Window Grabber est affiché).
- **Type de connexion** (best-effort, via WMI `WmiMonitorConnectionParams`) : HDMI / DisplayPort / USB-C / DVI / VGA / Inconnu.
- **Détection des fenêtres** : toutes les fenêtres de niveau supérieur "utiles", filtrage des fenêtres cloakées / outils / sans titre.
- **Infos par fenêtre** : titre, nom du processus, icône, état (normal / minimisée / maximisée), moniteur actuel.
- **Miniature live** des fenêtres via `DwmRegisterThumbnail` (fallback élégant sur l'icône si échec).
- **UI sombre moderne** : grille de cartes, badges moniteur, recherche/filtre, actualisation, tri.
- **Action principale** : clic → la fenêtre est activée, restaurée si minimisée, recentrée proprement sur le moniteur cible (zone de travail, sans dépasser).
- **Paramètres** persistés dans `%APPDATA%\WindowGrabber\settings.json` :
  - Activer/désactiver les miniatures live
  - Afficher/masquer le type de connexion
  - Masquer les fenêtres système
  - Thème sombre (activé par défaut)
- **Logs** rotatifs dans `%APPDATA%\WindowGrabber\logs\`.
- **Gestion d'erreurs** : une fenêtre qui refuse d'être déplacée n'entraîne aucun crash — un message discret est affiché.

---

## Prérequis

- **Windows 10 (1809+) ou Windows 11**
- **.NET 8 SDK** pour builder depuis les sources : https://dotnet.microsoft.com/download/dotnet/8.0
- (Optionnel) **Visual Studio 2022 17.8+** ou **JetBrains Rider** pour ouvrir `WindowGrabber.sln`

> Aucun prérequis pour l'utilisateur final si le `.exe` est publié en **single-file self-contained** (option par défaut) : tout est inclus.

---

## Lancer en local (développement)

```powershell
git clone <repo>
cd WindowGrabber
dotnet restore
dotnet run --project src/WindowGrabber/WindowGrabber.csproj
```

---

## Générer le `.exe` (build release)

### Option 1 — Single-file self-contained (recommandé, aucun prérequis côté utilisateur)

```powershell
cd WindowGrabber
dotnet publish src/WindowGrabber/WindowGrabber.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o publish
```

L'exécutable final se trouve dans :

```
WindowGrabber\publish\WindowGrabber.exe
```

Double-cliquez dessus pour lancer l'application.

### Option 2 — Framework-dependent (nécessite .NET 8 runtime installé)

```powershell
dotnet publish src/WindowGrabber/WindowGrabber.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -o publish
```

---

## Utilisation

1. Lancez `WindowGrabber.exe` sur le moniteur où vous voulez rapatrier les fenêtres.
2. La barre supérieure affiche le **Moniteur cible actuel**.
3. La grille liste toutes les fenêtres ouvertes avec leur miniature.
4. Cliquez sur une carte → la fenêtre est ramenée sur votre écran.
5. Utilisez la barre de recherche pour filtrer par titre ou nom d'application.
6. Bouton **Actualiser** pour recharger la liste.
7. Bouton **Paramètres** pour ajuster les options.

> **Astuce** : déplacez la fenêtre Window Grabber sur un autre écran puis actualisez — l'écran cible change automatiquement.

---

## Structure du projet

```
WindowGrabber/
├── WindowGrabber.sln
├── README.md
└── src/WindowGrabber/
    ├── WindowGrabber.csproj
    ├── app.manifest                     # DPI PerMonitorV2
    ├── App.xaml / App.xaml.cs
    ├── Resources/                       # logo.png, app.ico
    │
    ├── Interop/                         # P/Invoke Win32 (isolé)
    │   ├── NativeMethods.cs
    │   ├── NativeStructs.cs
    │   ├── NativeConstants.cs
    │   └── IconExtractor.cs
    │
    ├── Models/                          # POCO métier
    │   ├── MonitorInfo.cs
    │   ├── WindowInfo.cs
    │   ├── Enums.cs
    │   └── AppSettings.cs
    │
    ├── Services/                        # Logique métier (pas de WPF ici)
    │   ├── MonitorService.cs            # EnumDisplayMonitors
    │   ├── WindowService.cs             # EnumWindows + filtrage
    │   ├── WindowMover.cs               # Logique de déplacement
    │   ├── ConnectionTypeService.cs     # WMI WmiMonitorConnectionParams
    │   └── SettingsService.cs           # Persistence JSON
    │
    ├── ViewModels/                      # MVVM
    │   ├── ViewModelBase.cs
    │   ├── MainViewModel.cs
    │   ├── WindowItemViewModel.cs
    │   └── SettingsViewModel.cs
    │
    ├── Views/                           # UI WPF
    │   ├── MainWindow.xaml(.cs)
    │   ├── SettingsWindow.xaml(.cs)
    │   └── WindowCard.xaml(.cs)
    │
    ├── Controls/
    │   └── DwmThumbnailHost.cs          # HwndHost wrappant DwmRegisterThumbnail
    │
    ├── Converters/
    ├── Themes/DarkTheme.xaml
    └── Helpers/                         # Logger, RelayCommand
```

Séparation nette :
- **Interop/** → uniquement du P/Invoke, aucun code métier.
- **Services/** → logique pure, aucun code WPF, testable.
- **ViewModels/** + **Views/** → UI WPF.

---

## Logs

Emplacement : `%APPDATA%\WindowGrabber\logs\windowgrabber.log`

Niveau par défaut : INFO. Les erreurs de déplacement (fenêtres système protégées, etc.) y sont tracées sans bloquer l'UI.

---

## Limitations connues

- Certaines fenêtres **UWP / Store** peuvent refuser d'être déplacées par `SetWindowPos` (sécurité OS). Window Grabber détecte ce cas et log l'échec sans planter.
- Les fenêtres **exécutées en tant qu'administrateur** ne peuvent pas être déplacées par une app non-élevée (UIPI). Relancer Window Grabber "en tant qu'administrateur" si nécessaire.
- Le **type de connexion** (HDMI / DP / USB-C) dépend du driver GPU : certains PC retournent "Inconnu".
- Les miniatures **DWM** ne fonctionnent que si la composition DWM est active (cas par défaut sur Windows 10/11).

---

## Licence

MIT — voir header des fichiers source.
