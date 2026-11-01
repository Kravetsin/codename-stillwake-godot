# CI runs the tests in Release

- Status: accepted
- Date: 2026-10-31

## Context

`dotnet test` took 80% of CI's `build, tests` job: 150 to 400 s of a job of 3.5 to 8 minutes, the same tests on whatever
machine the run drew. It ran in Debug, where the JIT optimizes nothing. The repository is public, so its runners are already
GitHub's largest free ones, 4 vCPU and 16 GB, and jobs wait 2 s for one.

Measured on GitHub's runners with every setting on one machine, so the machines' differences do not blur it: the whole
suite in seconds, on `main` at `11d5614c`, in run 41240105506 of a throwaway branch.

| Runner, CPU | Machines | Debug | Release | Release, `DOTNET_TieredPGO=0` | Release, `DOTNET_TieredCompilation=0` | Release, `DOTNET_gcServer=1` |
| --- | --- | --- | --- | --- | --- | --- |
| `ubuntu-latest`, EPYC 9V45 | 3 | 166–172 | 81–88 | 77–81 | 82–83 | 82–89 |
| `ubuntu-latest`, EPYC 9V74 | 1 | 249 | 116–118 | 105 | 112 | 102 |
| `ubuntu-24.04-arm`, Neoverse-N2 | 2 | 234–257 | 91–104 | 86–94 | 84–98 | 100–101 |

Every run gave the same results, in Debug and Release, on x64 and arm64. A desk misleads here: at 4 threads on 16,
`TieredCompilation=0` saved 28% and Release 59%, which a runner's 4 vCPU do not repeat.

## Decision

- CI builds and tests with `-c Release`. The solution's Release builds the game in Debug, demos and all, and `ExportRelease`
  stays its own step, so CI still compiles what it did.
- The test step sets `DOTNET_TieredPGO=0`: 6–10% less on x64 for one line.
- `STILLWAKE_BUDGET_SLACK` goes from 5 to 2. On a desk a tick in Release is 2.5 to 13 times cheaper than in Debug, so at 5 CI
  would stop noticing a budget going. All six machines passed the budgets at 1 with room left; the tightest, forty creatures'
  share of a tick, took 0.15–0.24 ms of its 0.4 ms.
- CI logs `lscpu`, to tie a slow run to its machine. Past runs fall into three speeds, the slowest more than twice the
  fastest; the slowest did not turn up among these four x64 machines. Later runs named it: AMD EPYC 7763, 131–145 s of tests in
  Release against the EPYC 9V45's 75–80 s, in 7 of 16 runs.

## Consequences

- A failure seen only on CI reproduces with `dotnet test Stillwake.sln -c Release`. A desk stays in Debug, where the budgets
  are strict and a test can be debugged.
- Code under `#if DEBUG` and `Debug.Assert` would never run on CI. There is none in `sim/`, `net/` or `game/` today, and a rule
  that matters belongs in a test anyway.
- The budget tests on CI now time the optimized code players run, with PGO off, so a little dearer than a player's.

## Not done, on purpose

- arm64: 2.8 minutes whatever the run draws, against 2.3 to about 3.8 on x64, but it no longer tests the architecture
  players use. Not worth that yet; the CPU lines in the logs will say how often x64 draws its slowest machines.
- `TieredCompilation=0` and `gcServer`: no better than `TieredPGO=0` on the runners, and `gcServer` holds more memory.
- Caching NuGet or the build: `restore` takes 10 s, and a build cache would trade a few seconds for stale outputs.
- Splitting the tests over jobs: each would build first, and the steps in [`docs/plans/dev-workflow.md`](../plans/dev-workflow.md) are cheaper.
