# Codex Credit Monitor Roadmap

## Stable baseline

v2.2 is the current stable baseline, formally closed and paused on 7 September 2026 after successful live publication verification. Its implementation, product documentation, screenshot, MSI, PortableApps package, and checksums are centrally archived in this project.

## Authoritative GitHub publication policy

This section is the sole current instruction for GitHub publication. Older changesets are historical records and do not override it.

- Only Simon may explicitly approve a GitHub publication, release or tag change, asset replacement, or version increase. A version increase may be proposed, but is never performed without Simon's explicit approval.
- Bug fixes retain the current version and tag, including after a release has been closed or paused. The current process-lifecycle correction remains version `2.2.0`; no version increase is authorized.
- GitHub exposes only the current supported release and its matching tag. When a release is replaced, prior public release assets and distribution files are removed; the local source repository retains its complete history.
- GitHub-facing README content and release descriptions are entirely English. The bundled product README remains the single source rendered dynamically in the Info window.
- Each release has exactly four uploaded assets: the PortableApps ZIP, the x64 MSI, `SHA256SUMS.txt`, and one current dashboard screenshot. GitHub-generated source archives are outside this asset count.
- README, release description, screenshot, checksums, download links, file names, release, and tag must all refer to the same current version.

### Required publication verification

- Before Simon's approval: verify the self-contained build, MSI, PortableApps package, checksums, English README and release description, and the dashboard screenshot.
- After publication: download all four uploaded assets from GitHub and compare them byte-for-byte or by SHA-256 with the final local files. Live-check `latest`, the tag, README, release description, download links, and screenshot rendering.

### Public dashboard screenshot

- The screenshot is a reproducible, English simulation of a critical state; it never shows current personal usage data.
- It simultaneously shows low but positive credit balance, auto recharge, a partially used 5-hour window with reset time and remaining percentage, exhausted weekly allowance, visible Add credits and Use credits actions, today's totals, and recent session activity.
- The full professional dashboard layout must be visible without clipping, overlap, or missing captions, and must receive visual review before publication.
- The existing v2.2 critical-state simulation is the public screenshot reference until Simon explicitly approves a different composition.

## Changeset document convention

All centrally stored changesets use the filename format `YYYY-MM-DD_CHANGESET_NNN.md`.

- `YYYY-MM-DD` is the date on which the changeset is first centrally registered.
- `NNN` is one project-wide, chronological, zero-padded sequence number.
- Multiple changesets on the same date receive successive sequence numbers.
- An assigned filename and sequence number remain stable; later edits do not change them.
- Each changeset records at least: status, baseline, target release, objective, scope, acceptance criteria, test matrix, regression checks, exclusions, and delivery conditions.
- Closed releases are not silently modified. Bug fixes may remain under the existing version only under the authoritative policy above. Changeset 003 is the explicitly authorized v2.2 corrective-release exception and was approved for full GitHub publication on 7 September 2026.

Current sequence:

- `2026-09-06_CHANGESET_001.md` — completed v2.1.1 bundle.
- `2026-09-06_CHANGESET_002.md` — completed v2.2 stable-release bundle.
- `2026-09-06_CHANGESET_003.md` — completed historical seven-bug v2.2 quality and stability bundle.
- `2026-09-09_CHANGESET_004.md` — active v2.2.0 lifecycle and usage-pace corrective bundle; no version increase is authorized.
- `2026-09-13_CHANGESET_005.md` — completed v2.2.0 refresh-reliability corrective publication and live verification; no version increase.

