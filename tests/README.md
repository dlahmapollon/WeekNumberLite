# Windows build and icon regression

Requires Windows, Visual Studio Build Tools (MSBuild) and .NET Framework 4.8 Developer Pack.
No test framework or NuGet packages are required. The console test project compiles the
actual production WeekIcon.cs and Week.cs through linked source files.

From a Visual Studio Developer PowerShell at the repository root:

```powershell
.\build.bat
msbuild tests/IconTests.csproj -t:Rebuild -p:Configuration=Release
.\tests\bin\Release\IconTests.exe artifacts/icons
```

Each command must exit with code 0. The Windows Actions workflow runs both Release
and Debug and uploads executables, MSBuild binary logs, test results and PNGs even
when the tests fail. The Release job also exercises build.bat. No installer is run.

## Automated coverage

- All weeks 01 through 53, each at 16, 20, 24, 28, 32, 48 and 64 pixels.
- Explicit light/dark palette inputs; no dependency on runner theme, DPI or date.
- Exact repeatability in one process, dimensions, transparent corners, partially
  transparent antialiased edges, digit ink and height, separation and distinct
  threshold masks for all 53 weeks at each size/theme.
- Solid foreground contrast >= 7:1 and composited digit core contrast >= 4.5:1
  against representative opaque backgrounds #f3f3f3 and #202020. Antialiased
  edge pixels are intentionally excluded from the core contrast threshold.
- Native HICON roundtrip dimensions, background alpha, header and digit opacity;
  returned clone remains usable after original handle destruction.
- 300 create/dispose cycles after warmup, checking both USER and GDI handle counts.
- Invalid arguments, malformed/missing theme data, and ISO week-year boundaries.

The renderer retains Segoe UI Bold. Font and GDI+ updates may alter pixels between
Windows versions. These tests use structural invariants instead of unreviewed golden
hashes. Compare the uploaded sheets at native size and use the 8x nearest-neighbor
samples (11, 18, 38, 42, 53) to inspect edge pixels. The sheets are visual review
artifacts, not an automated proof that humans can read each number.

## Required desktop acceptance

On Windows 10/11, review the actual tray at 100%, 125%, 150%, 175%, 200%, 300%
and 400% scale. Check difficult pairs 11/17, 18/48, 33/38, and 50/53, plus
01/08/10/42/52. PNG sheets contain all weeks so the system clock need not change.

Check both system themes (taskbar theme, not just app theme), theme switching while
running, contrast themes, transparent taskbars/wallpapers, taskbar overflow, Explorer
restart, sleep/resume and moving the taskbar between differently scaled monitors.
The explicit-size tests verify rendering at DPI target sizes; they do not prove
that SystemInformation.SmallIconSize tracks a live per-monitor taskbar DPI change.
If it does not, that integration requires a separate shell/DPI change.

A screenshot or sheet should be inspected at native resolution with no browser
interpolation. Confirm no fringe, clipped stroke, solid background or loss of the
calendar header. Representative contrast checks do not certify all wallpaper colors.
