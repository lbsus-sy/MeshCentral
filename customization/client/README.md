# Windows client packaging design notes

The deliverable should be a signed, transparent installer around the supported MeshCentral agent, rather than a replacement remote-control protocol.

## Required behavior
- User/admin explicitly consents before installation and sees the server hostname.
- Verify Authenticode signatures for the installer and payload.
- Install the service with an understandable display name and predictable logs.
- Quiet/no-console behavior applies to normal agent runtime, not to consent, warnings or installation failure prompts.
- Use short-lived/scoped enrollment and never embed reusable administrator credentials.
- Provide a visible uninstaller and clean removal of the service, scheduled tasks and product-owned files.
- Record installer version, server destination and install result in local logs; never log credentials.

## Deferred until a signed build pipeline exists
- No opaque packed EXE, hidden install, security-tool exclusion, log deletion, or covert persistence.
- No client-side exploit or anti-analysis behavior.