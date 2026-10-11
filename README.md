# Footprint

Footprint is a read-only, local-first Windows storage analyzer. It scans a chosen folder, ranks entries by size, supports cancellation, and never deletes or modifies files.

Version 1.0 uses a fault-tolerant directory walker: inaccessible entries and reparse points are reported and skipped instead of aborting the entire scan. Directory totals use boundary-safe path checks, and detailed results include scanned-file counts, total bytes, progress events, and skipped-item evidence.

Version 1.1 brings that evidence into the desktop interface. Each completed scan now distinguishes complete coverage from scans with inaccessible or reparse-point paths, shows exact file and folder counts, and exposes the first skipped-path details in the warning tooltip.

Version 1.2 also checks the selected root itself for reparse points before traversal, so choosing a junction reports a skipped root instead of scanning outside the selected location.

MIT licensed.

## Name change

Footprint replaces OpenSpace. New builds use Footprint.exe and Footprint.csproj. Existing releases remain available under their original names.

## Automatic setup

Windows setup packages install Footprint for your current account, bundle the .NET runtime, create a Start menu shortcut and register an uninstaller. A desktop shortcut is optional. No administrator password or manual file copying is required. Existing settings and saved data stay in their current locations. The portable ZIP remains available.

Download `Footprint-v1.3.1-setup.exe` from Releases and run it.

For unattended deployment:

```powershell
Start-Process .\Footprint-v1.3.1-setup.exe -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-' -Wait -PassThru
```

Build from source with the [open-source Inno Setup compiler](https://github.com/jrsoftware/issrc) installed:

```powershell
pwsh -File scripts/Build-Setup.ps1 -Verify
```

The verification option installs into a temporary test folder, checks reinstall and launch, then uninstalls. It runs on the hosted Windows runner during CI. It does not remove existing user settings or backups.
