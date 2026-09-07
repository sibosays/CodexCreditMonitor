# Codex Credit Monitor Roadmap

## Stable baseline

v2.2 is the current stable baseline. Its implementation, product documentation, screenshot, MSI, PortableApps package, and checksums are centrally archived in this project.

## Release policy

- GitHub exposes only the latest supported release and its matching tag.
- GitHub-facing documentation is written in English.
- Every release provides a tested installable package, a portable package, checksums, and a current dashboard screenshot.
- The bundled product README is the single source displayed dynamically in the Info window.
- Once a version is closed, fixes and features are planned under the next major version unless the user explicitly authorizes a same-version corrective release.

## Next version

Future product work belongs to the next major version and starts only after v2.2 has been formally closed.

## Changeset document convention

All centrally stored changesets use the filename format `YYYY-MM-DD_CHANGESET_NNN.md`.

- `YYYY-MM-DD` is the date on which the changeset is first centrally registered.
- `NNN` is one project-wide, chronological, zero-padded sequence number.
- Multiple changesets on the same date receive successive sequence numbers.
- An assigned filename and sequence number remain stable; later edits do not change them.
- Each changeset records at least: status, baseline, target release, objective, scope, acceptance criteria, test matrix, regression checks, exclusions, and delivery conditions.
- Closed releases are not silently modified. Changeset 003 is the explicitly authorized v2.2 corrective-release exception and was approved for full GitHub publication on 7 September 2026.

Current sequence:

- `2026-09-06_CHANGESET_001.md` — completed v2.1.1 bundle.
- `2026-09-06_CHANGESET_002.md` — completed v2.2 stable-release bundle.
- `2026-09-06_CHANGESET_003.md` — completed seven-bug v2.2 quality and stability bundle, including the final historical usage-pace correction.

