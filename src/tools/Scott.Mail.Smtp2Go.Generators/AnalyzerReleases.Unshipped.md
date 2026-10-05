; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SMTP2GO001 | Smtp2Go.Generation | Error | The endpoint snapshot (endpoints.json) is not among the additional files.
SMTP2GO002 | Smtp2Go.Generation | Error | The endpoint snapshot could not be read.
SMTP2GO003 | Smtp2Go.Generation | Error | An EndpointTraits pattern matches no documented operation.
SMTP2GO004 | Smtp2Go.Generation | Error | A type marked for generation is not partial.
SMTP2GO005 | Smtp2Go.Generation | Error | A generated client has no Smtp2GoConnection constructor parameter.
