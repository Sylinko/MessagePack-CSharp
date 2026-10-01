# Sylinko package publishing

`release-sylinko-nuget.yml` publishes MessagePack, MessagePack.Annotations and
MessagePackAnalyzer under the original package IDs and one synchronized version.

The release workflow generates a stable package version as
`999.YYYYMMDD.<github.run_number>`, for example `999.20261001.42`. The date
comes from the original workflow run's creation time in UTC. All packages and
platforms from that run share the same version, including when jobs are rerun.
Run numbers belong to this workflow; keep one publishing workflow per package.
Trigger it through workflow_dispatch (no version input), or push a `publish-*`
source tag. The generated version is written to the workflow summary, and the
publishing action creates a release tagged `nuget/<producer>/<version>`.

The workflow refuses to upload over a release that already contains assets,
because the feed action otherwise uses `--clobber`. If publishing stopped after
uploading assets, retain that release and start a new run to publish a new
version. Check any existing feed PR before completing or abandoning it.
The local draft version `999.0.0` is only for local builds and smoke tests; never
publish it. Package release versions do not change assembly or file versions.

Configure `SYLINKO_NUGET_FEED_TOKEN` as in Sylinko/Microsoft.ML and register the
three IDs in Sylinko/nuget-feed first. The publishing action creates a GitHub
Release and a feed PR; merge that PR to make the packages available.

The runtime package depends on annotations and the analyzer package, which
contains both the source generator and code fixes. The package smoke test uses
generated formatters without a reflection fallback, so it detects missing
generator assets. It references only the exact runtime package version, allowing
NuGet to bring in annotations and the generator transitively from the local
package directory. A private package cache prevents an older local build from
satisfying the test. No library test suite or benchmark is run by this step.

Keep assembly names, the existing `1.0.0.0` assembly identity and signing key
stable; package versions evolve independently. A high numeric package version
satisfies common upstream minimum-version constraints, but is outside exact or
upper-bounded upstream ranges and does not establish API compatibility.
NuGet advisory version ranges refer to upstream versions, so the absence of an
audit warning for a date-based fork version does not prove a vulnerability is
fixed. Check advisories against the upstream source revision separately.

For local packing, run `dotnet pack -c Release` separately for
`src/MessagePack/MessagePack.csproj`,
`src/MessagePack.Annotations/MessagePack.Annotations.csproj` and
`src/MessagePack.Analyzers/MessagePack.Analyzers.csproj`.
Pass `-p:PackageVersion=999.20261001.42` to validate a release-shaped version
locally and use it for the consumer smoke test as well.
The analyzer's pack target resolves its project builds, so do not use --no-build.
