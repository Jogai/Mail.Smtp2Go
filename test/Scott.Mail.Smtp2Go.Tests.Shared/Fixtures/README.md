# Fixtures

JSON request/response samples used by the unit and contract tests.

- `Transport/`: the documented error envelope shapes (nested `data.error`, flat, field validation as object/array/map, garbage) and a success envelope with unknown top-level fields.
- `Json/timestamps.json`: every timestamp format observed in the API docs, with the expected UTC instant.
- `Webhooks/`: the docs examples for `webhook/add`, `webhook/edit`, `webhook/view` and `webhook/remove` (plus the live array shape of `webhook/view`), and under `Webhooks/Docs/` and `Webhooks/Live/` the callback payloads in JSON and form encoding (see the README there).
- `Stats/`: the docs' 200 examples of the six `stats/*` endpoints, a full `email_history` response built from its schema, and request goldens.
- `Activity/`: the docs' `activity/search` 200 example, a full response built from its schema (every event property, delivery attempts, an unknown event name) and the request golden.
- `Templates/`: the docs' 200 examples of the five `template/*` endpoints and the request goldens.
- `Suppressions/`: the docs' 200 examples of the three `suppression/*` endpoints and the request goldens.
