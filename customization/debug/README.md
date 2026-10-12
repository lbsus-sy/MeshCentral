# Debug and diagnostics feature boundary

The default tools should be read-only and opt-in:

- Process list, PID, image path, user, CPU/memory summary.
- Export selected Windows Event Log entries and service state.
- Collect existing crash dumps with user/admin authorization and explicit size and retention limits.
- Show a WinDbg-inspired summary for an uploaded dump: exception code, faulting module, stack frames and symbol status. Use Microsoft's supported debugging components and comply with their licensing terms.
- Terminate only an explicitly selected process after authorization and clear confirmation; protect critical/system processes and audit every action.
- Test application crashes only against a dedicated test process in an isolated test VM. A kernel BSOD test must not be a remote production feature; use a disposable VM/checkpoint and a local confirmation path outside the remote management channel.

Every privileged operation needs server-side authorization, per-device audit records, bounded execution time/output, and a visible result. Client-side UI hiding is not a security control.