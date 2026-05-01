# Percentage Agent Instructions

Battery Percentage Icon (Percentage) is a public-facing Windows tray application built on WPF and Windows Forms. It is distributed through the Microsoft Store as an MSIX package and through GitHub Releases as a portable, self-contained single-exe build, and has a large existing user base. Behavior, identifiers, settings, packaging surface, and minimum supported OS are stable contracts that must not change without an explicit migration plan. These instructions apply to all AI agents performing code reviews, code changes, tests, documentation updates, dependency updates, or repository maintenance in this repository.

The application also references the sibling Codify projects (`..\..\Codify\System.Windows\System.Windows.csproj` and, transitively, `..\..\Codify\System\System.csproj`) as project references. Codify must be checked out as a sibling directory of this repository. Codify's own `AGENTS.md` governs changes made inside those projects; this file governs the Percentage application code only.

## Core Priorities

1. Preserve correctness and expected runtime behavior. Tray crashes and silently broken notifications are invisible to users and erode trust; never trade reliability for cleverness.
2. Maximize performance and minimize memory consumption. The app runs continuously from sign-in, so steady-state memory, GDI/HICON usage, startup cost, and tray refresh throughput are first-class design constraints.
3. Always use the latest stable C# language version, .NET runtime, WPF platform, SDK, tooling, and library features. When the latest stable release of a technology is on a non-LTS track and an LTS track exists, target the latest LTS release of that track instead.
4. Adopt newly stabilized language features, BCL APIs, and framework capabilities as soon as they ship; do not retain older patterns once a cleaner modern equivalent is available.
5. Do not use preview, experimental, deprecated, unsupported, or unstable APIs unless explicitly requested and documented. The Microsoft Store certification process and the public user base raise the bar for stability.
6. Apply best practices for the relevant .NET, WPF, MSIX packaging, and application context whenever possible.
7. Keep code testable by preferring small units, explicit dependencies, deterministic behavior, and clear contracts. When extracting logic into a service-style class, prefer designs that admit unit testing even if no tests exist yet.
8. Prefer manual smoke testing on Windows 11 for user-visible changes (tray icon rendering, notifications, settings persistence, auto-start, single-instance behavior). Add unit tests where they meaningfully reduce risk.

## Backward Compatibility Contract

Unlike a personal library, Percentage has a real public contract. The following surfaces must not change without an explicit, opted-in migration plan:

- **User settings**: any property in `App/Properties/Settings.settings` and the related `Default*` constants on the `App` class. Renaming, removing, or changing the type or format of an existing setting requires extending `MigrateUserSettings` and `TryMigratePreviousUserConfig` in `App/App.xaml.cs` so that users upgrading from older versions retain their preferences. The `RequiresUpgrade` flow and the existing hex-string color migrations are precedents.
- **Microsoft Store package identity**: `Identity/@Name`, `Identity/@Publisher`, and the resulting Package Family Name in `Pack/Package.appxmanifest`. Changing any of these breaks Store updates for every installed user.
- **Stable identifiers**: the `App.Id` GUID (`f05f920a-c997-4817-84bd-c54d87e40625`) is shared by the single-instance Mutex name in `App/App.xaml.cs` and by `StartupTask/@TaskId` in the appxmanifest. The toast `ToastActivatorCLSID` (`549f9494-b2ef-4e7a-89a4-f36ad71025a0`) and its matching `com:Class/@Id` must also remain stable. Never regenerate these identifiers.
- **Minimum supported OS**: `<SupportedOSPlatformVersion>` in `App/App.csproj` and the `TargetDeviceFamily/@MinVersion` values in `Pack/Package.appxmanifest`. Tightening these cuts off existing users; never raise without explicit instruction.
- **Distribution flavors**: both the MSIX (Microsoft Store) flavor and the portable single-exe (GitHub Releases) flavor are supported. Auto-start configuration uses MSIX `StartupTask` only; the portable flavor relies on a manual shortcut in the user's Startup folder. Changes that affect startup behavior must consider both paths.
- **Crash-handling UX**: the three unhandled-exception handlers in `App/App.xaml.cs` (`DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`), the `HandleException` message format, and the issue-reporting URL `https://github.com/soleon/Percentage/issues` are part of the support workflow. Do not remove the handlers or change the reporting URL without coordinating with project maintenance.

Breaking changes outside this contract (internal types, private helpers, refactors of XAML-only views, code organization) are fine and do not require deprecation cycles.

## Dependency and Platform Policy

- Always use the latest stable version of every NuGet package, .NET runtime, WPF platform, SDK, and tool.
- When a technology has an LTS release track and the latest stable release is not on that track, target the latest LTS release instead. The current target is `net10.0-windows10.0.26100.0` (latest LTS .NET).
- Single-target the chosen runtime. Do not multi-target older versions, add compatibility target frameworks, or add package-validation baselines.
- When a newer stable (or newer LTS, where applicable) release of any dependency or platform is available, updating to it is the default expectation, not an opt-in. Confirm builds for all four configured platforms (`AnyCPU`, `x64`, `ARM64`, `x86`) and that the MSIX package still produces.
- Avoid adding dependencies unless they clearly improve correctness, performance, maintainability, testability, or platform support.
- Do not introduce large dependencies for small utilities. App download size is user-visible.
- When a dependency cannot be updated to the latest stable (or latest LTS) version, document the technical reason blocking the update.
- The Codify project references are load-bearing. Codify must be checked out as a sibling directory; Codify's `AGENTS.md` governs changes inside those projects.

## Performance and Memory Policy

- Treat allocation rate, steady-state memory use, startup cost, UI responsiveness, and tray refresh throughput as first-class design constraints. The app refreshes the tray icon on a configurable interval (default 60 seconds) and runs for the entire user session.
- Be specifically vigilant about GDI and HICON resource leaks. WPF-UI's tray icon rendering has had recurring HICON leak issues; any code that produces `Icon`, `HICON`, or backing bitmaps must dispose them deterministically.
- Prefer allocation-conscious APIs, efficient data structures, and predictable algorithms.
- Avoid unnecessary LINQ, reflection, boxing, string allocations, repeated enumeration, closure captures, and async overhead on the tray refresh path and in startup code.
- Use spans, memory pooling, caching, compiled expressions, source generators, or specialized collections only when they are appropriate, measurable, and maintainable.
- For performance-sensitive changes, validate with diagnostic measurement (allocation profiler, ETW, or scoped repro) or explain why measurement is not practical.
- Do not trade away correctness or maintainability for speculative micro-optimizations.

## WPF and Application Policy

- Keep UI-thread work minimal and avoid blocking the dispatcher. Battery queries, registry reads, and file I/O must not run synchronously on the dispatcher.
- Preserve WPF binding behavior, dependency property semantics, resource lookup behavior, and design-time usability of XAML pages.
- Prefer nullable-safe, analyzer-friendly, idiomatic modern C#. The project uses `<Nullable>enable</Nullable>` with `<WarningsAsErrors>nullable</WarningsAsErrors>`; do not silence nullable warnings without a clear comment explaining why.
- Internal types, helpers, and view code may change freely; reshape, rename, or remove them whenever doing so produces a cleaner surface. Anything covered by the Backward Compatibility Contract above is excluded.
- Document non-trivial public types and members with XML comments where it aids understanding. Full XML coverage is not required for application-internal code, but it is required in the referenced Codify projects (per Codify's `AGENTS.md`).
- Limit global mutable state. Existing static state on the `App` class is acceptable for application-level coordination (single-instance mutex, snackbar service, app-error broadcast); justify any new global state.
- Tray applications must never crash silently. All long-running and event-driven entry points must funnel uncaught exceptions back into the existing handlers or surface them through `SetAppError`.

## Microsoft Store Packaging Policy

- The MSIX package is produced by `Pack/Pack.wapproj` and described by `Pack/Package.appxmanifest`. Changes to either require careful review.
- Do not add new capabilities to the manifest unless required. The current capability set is intentionally minimal (`runFullTrust`); broader capabilities raise Microsoft Store certification risk and the user-trust cost.
- Keep `<PackageVersion>` in `App/App.csproj` and `Identity/@Version` in `Pack/Package.appxmanifest` in sync on every release-bearing commit.
- The `Pack/Package.StoreAssociation.xml` file is generated by Visual Studio when associating the project with a Store listing. Do not hand-edit it.
- Asset images under `Pack/Images/` follow the MSIX scale and target-size naming convention (`scale-100/125/150/200/400`, `targetsize-16/24/32/48/256`, `altform-unplated`, `altform-lightunplated`). Replacements must keep all variants present and valid.
- The portable, self-contained single-exe build path that ships through GitHub Releases is built from `App/App.csproj` directly and does not go through the Pack project. Changes that affect startup, file layout, or external resources must be validated under both flavors.

## Testing Policy

- No automated test projects currently exist in this repository.
- When extracting logic into testable units (services, helpers, value converters), prefer designs that admit deterministic unit tests and add tests where they reduce real risk.
- For all changes that affect user-visible behavior, perform manual smoke testing on Windows 11 covering the relevant subset of: tray icon rendering across light and dark themes and DPI scales; notification flow for critical, low, high, and full battery thresholds; settings persistence and migration from earlier versions of the app; auto-start (MSIX) and shortcut-based start (portable); single-instance behavior; and graceful exit.
- Do not reduce or remove existing tests (if added later) without documenting the reason.

## Code Review Policy

When reviewing changes, prioritize findings in this order:

1. correctness bugs, crashes, and silent failures in tray rendering, notification dispatch, settings persistence, or startup
2. user-visible behavior regressions, especially settings loss, missing notifications, or tray icon failure
3. performance or memory regressions, with extra weight on GDI and HICON leak risk
4. WPF threading, binding, resource, lifecycle, or dispatcher risks
5. Microsoft Store packaging risks: package identity, manifest schema, capabilities, signing, minimum OS version, asset variants
6. backward-compatibility breaks for existing users (settings format, stable identifiers, distribution-flavor differences)
7. failure to use the latest stable (or latest LTS, where an LTS track exists) version of a relevant runtime, SDK, language feature, or dependency
8. dependency, target framework, SDK, or tooling risks
9. maintainability and best-practice issues

Do not flag the absence of API-level deprecation paths or migration shims for purely internal types as a finding; backward compatibility applies to the user-visible surface defined above, not to internal application code.

Do not prefix review findings with bracketed severity labels such as `[P0]`, `[P1]`, `[P2]`, or `[P3]`.

## Tooling Notes

- Do not run `dotnet build`, `dotnet test`, `dotnet pack`, or `dotnet format` concurrently against the same solution or projects; shared `obj/` outputs can lock and cause transient MSBuild/CSC failures. This applies across the App and Pack projects in this repository, and across both Percentage and Codify when their builds may overlap.
- Building `Pack/Pack.wapproj` requires Visual Studio's MSIX/Desktop Bridge tooling. A bare `dotnet build` against the wapproj is not always sufficient; prefer a Visual Studio build or `msbuild` with the full Visual Studio targets when validating packaging changes.
- When intentionally deleting a tracked file that already has staged edits, `git rm` may refuse the deletion; after confirming the deletion is intended, use `git rm -f -- <path>`.

## Required Final Checklist

Before claiming work is complete, agents must verify or explicitly state why they could not verify:

- `App/App.csproj` builds for `AnyCPU`, and additionally for `x64`, `ARM64`, and `x86` when the change touches platform-conditional code or native interop
- `Pack/Pack.wapproj` still produces an MSIX package when the change touches the manifest, packaging, identifiers, capabilities, asset variants, or platform configuration
- analyzers and nullable warnings (treated as errors) pass
- the latest stable (or latest LTS, where an LTS track exists) versions of dependencies, runtimes, SDKs, and language features are in use
- performance and memory impact were considered, including any GDI or HICON allocation introduced by the change
- the Backward Compatibility Contract has not been broken (settings format, package identity, stable identifiers, minimum OS version, crash-handling UX)
- manual smoke testing was performed for any user-visible behavior change, or the limitation was stated explicitly
- when Codify projects were edited as part of the change, Codify's own `AGENTS.md` was honored
