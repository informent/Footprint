# Footprint

Footprint is a read-only, local-first Windows storage analyzer. It scans a chosen folder, ranks entries by size, supports cancellation, and never deletes or modifies files.

Version 1.0 uses a fault-tolerant directory walker: inaccessible entries and reparse points are reported and skipped instead of aborting the entire scan. Directory totals use boundary-safe path checks, and detailed results include scanned-file counts, total bytes, progress events, and skipped-item evidence.

Version 1.1 brings that evidence into the desktop interface. Each completed scan now distinguishes complete coverage from scans with inaccessible or reparse-point paths, shows exact file and folder counts, and exposes the first skipped-path details in the warning tooltip.

Version 1.2 also checks the selected root itself for reparse points before traversal, so choosing a junction reports a skipped root instead of scanning outside the selected location.

MIT licensed.

## Name change

Footprint replaces OpenSpace. New builds use Footprint.exe and Footprint.csproj. Existing releases remain available under their original names.
