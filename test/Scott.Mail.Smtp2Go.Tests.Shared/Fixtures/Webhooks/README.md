# Webhook callback fixtures

Two sets, both of which the parser must handle; every fixture exists as `<name>.json` (`output_format: json`) and `<name>.form` (the raw `application/x-www-form-urlencoded` body of `output_format: form`).

- `Docs/`: built from the "Webhook Parameters - Email" and "- SMS" tables on developers.smtp2go.com/docs/webhooks-overview (2026-02-02 revision). Uses the documented event names (`open`, `click`, `spam`, `unsubscribe`, `resubscribe`, `reject`) and hyphenated keys (`message-id`, `user-agent`, `read-secs`, `client-device`, `geoip-*`). `unknown_future` is a made-up event to pin the Unknown fallback.
- `Live/`: what the API actually sent. `processed`, `delivered`, `bounce-hard`, `bounce-soft` and `clicked` are re-created from the payloads the community library captured in its unit tests (JSON captured 2026-02-07, forms captured 2026-03-24, with the mixed-case `Message-Id`/`Subject` duplicates). `opened`, `spam_complaint` and `unsubscribed` carry the live event names from that library's enum; their bodies are reconstructions, not captures. Replace any of these with a real capture from the tunnel-based live test when one is available.

The parent folder holds the `webhook/*` management fixtures (docs examples; `view-response-array.json` is the live array shape).
