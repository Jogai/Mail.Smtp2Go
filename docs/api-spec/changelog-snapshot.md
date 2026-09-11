<!-- Snapshot of https://developers.smtp2go.com/reference/changelog taken by the spec harvester; page updatedAt 2026-09-07T04:16:10.000Z. Do not edit. -->

# Changelog

**Sep 7th 2026**

* Added support for authenticating API requests with an `Authorization: Bearer <api-key>` header, alongside the existing
  `X-Smtp2go-Api-Key` header, so MCP clients that cannot send custom headers can connect.
* Updated the MCP documentation with Claude Desktop custom connector setup and Claude Code server scopes.

**July 30th 2026**

* Added a `type` parameter to the existing ip\_allow\_list endpoints to allow editing the SMTP or API allow lists.

**June 29th 2026**

* Allow `subaccount_id` to be passed to the `suppression/*` endpoints so Master account API keys can operate on subaccount suppressions.

**June 22nd 2026**

* Masked the `api_key` return property on the `api_keys/edit` and `api_keys/view` endpoints.

**June 19th 2026**

* Allow 'subaccount\_id' the be passed to the sms reporting endpoints.

**June 11th 2026**

* Enforced strict request body sizes, 1Mb for non-email calls and 50Mb for /v3/email\* endpoints.

**May 29th 2026**

* Allowed domains delegated to a subaccount to be visible in the /domain/view endpoint.
* Added a `from_master` boolean flag to indicate if said domains are owned or delegated.
* Added stricter rules around permissions when adding/editing API Keys, addition or removal of endpoints without appropriate
  permissions from the authenticating key or the existing permission set of a target key will no longer succeed.

**May 25th 2026**

* Added AI/MCP section.

**Sep 15th 2025**

* Added `email_password` to the `PATCH /users/smtp/edit` endpoint.

**Sep 14th 2025**

* Added `/ip_auth/view`, `/ip_auth/patch` and `/ip_auth/remove` to allow API requests to manipulate existing IP Auth entries.

**Sep 13th 2025**

* Added an option to the `/email/*` endpoints to allow a `schedule` timestamp to be passed allowing emails to be scheduled for sending
  up to 3 days from the request time. A maximum of 10,000 queued emails at a time are currently allowed and emails will be sent as close
  to the scheduled time as possible.

**Aug 15th 2025**

* Added a `PATCH` `/api_keys/edit` endpoint that allows for editing of API keys ignoring missing properties instead of defaulting them.

**Aug 1st 2025**

* We enabled Keep-Alives by default to speed up TLS handshakes for subsequent requests, If you want to explicitly request the connection to close please pass the `Connection: close` header with your requests.

**July 1st 2025**

* All webhook endpoints can now operate on subaccounts by passing the `subaccount_id`when making requests.

**June 12th 2025**

* Marked `spam_rejects` and `bounce_rejects` as deprecated (these fields will continue to be returned however)
* Added a `rejects` property as the above 2 shared the same information which was a little confusing.

**May 28th 2025**

* Reverted a change to the `/email/send` validation layer that was invalidating payloads with a `text_body` value of only spaces.

**April 7th 2025**

* Added `default_ratelimit_value` and `default_ratelimit_period` to `/api_keys` and `/users/smtp` responses.

**April 3rd 2025**

* Added `description` to the `/stats/email_history` endpoint and removed it from the `username` field data.
* Removed `feedback_domain` from the `/users/smtp*` endpoints as it's no longer required.
* Fixed an issue with `/api_keys/edit` where omission of the `endpoints`property would result in the list being overwritten with the default of `["/email/send"]`.
* Modified the return value of the `custom_ratelimit_period` returned from the `/users/smtp/(add|edit|view)` endpoints to be the same as the `/api_keys/(add|edit|view)`endpoints to keep them the same.
* Updated the `email_password` validation to accept passwords with a given entropy of 64 bits or more, generated passwords will have an entropy of 128 bits or more to improve SMTP security.

**January 23rd 2024**

* Modified `url` description on `/archive/search` endpoint from `A url that can be used to download the attachment` to `A url that can be used to download the original email`
* Added a `custom_headers` property to the `/activity/search` endpoint that allows for specifying headers to parse out of the raw email headers into a more usable structure.
* Added a `sender_full` property to the `/activity/search` endpoint that allows for the full sender information (ie. `Joe Blob <joeblog@example.com>`) to be accessed if present.
