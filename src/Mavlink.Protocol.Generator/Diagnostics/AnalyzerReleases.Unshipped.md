; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MAVGEN001 | MavlinkGenerator | Error | Invalid MAVLink XML
MAVGEN002 | MavlinkGenerator | Error | Included dialect not found
MAVGEN003 | MavlinkGenerator | Error | Include cycle
MAVGEN009 | MavlinkGenerator | Error | Dialect provided by two referenced assemblies
MAVGEN010 | MavlinkGenerator | Error | Local dialect file differs from the referenced one
MAVGEN011 | MavlinkGenerator | Error | Unsupported dialect manifest
MAVGEN014 | MavlinkGenerator | Error | Two dialect files with the same name
MAVGEN102 | MavlinkGenerator | Warning | Local dialect file ignored
MAVGEN106 | MavlinkGenerator | Warning | Included dialect of a reference not found
