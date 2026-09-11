# Security policy

## Supported versions

Only the latest release of the `Scott.Mail.Smtp2Go` packages receives security fixes.

| Version | Supported |
| :-- | :-- |
| 1.x (latest) | yes |
| earlier | no |

## Reporting a vulnerability

Please do not open a public issue for a security problem. Use GitHub private vulnerability reporting for this repository: [Report a vulnerability](https://github.com/Jogai/Mail.Smtp2Go/security/advisories/new). The report reaches the maintainer only, and a fix can be prepared and released before anything is disclosed.

Include the package and version, what the issue is, and how to reproduce it. You should get an acknowledgement within a week; fixes are released as a patch version with a `CHANGELOG.md` entry, and the advisory is published once the fix is available.

## Scope

The packages are clients for the SMTP2GO HTTP API. Issues in the SMTP2GO service itself should be reported to SMTP2GO through the channels on [smtp2go.com](https://www.smtp2go.com/). Handling of API keys in your own application (configuration, secrets storage) is outside this policy, but a library defect that leaks a key (for example in a log line or exception message) is very much in scope.
