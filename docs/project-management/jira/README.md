# Sprint / Jira Tracking Pack

This folder is the **project tracking artefact set** used for Solo Sprint governance (Jira-compatible).

## Files

| File | Purpose |
| --- | --- |
| `product-backlog.csv` | Full product backlog in **Jira CSV import** columns (Summary, Issue Type, Status, Sprint, Story Points, Epic Link…) |
| `sprint-burndown-points.csv` | Per-sprint remaining story points for burndown charts |
| `burndown-board.html` | Open in browser → screenshot for report appendix (Jira-style board + burndown) |

## How to import into Jira Cloud (optional)

1. Create a free/standard Jira Software project.
2. **Issues → Import issues from CSV** → upload `product-backlog.csv`.
3. Map columns: Summary, Issue Type, Status, Priority, Sprint, Story Points, Epic Link, Description, Assignee, Labels.
4. Create Sprints named `Sprint 1` … `Sprint 5` and complete them to match Status=Done rows.

## Solo practice (what we actually ran)

- Cadence: Phase 1 weekly Sprints 1–4; Phase 2 milestone Sprint 5.
- Backlog + SP burndown maintained in this folder and committed to Git (audit trail).
- Linked engineering evidence: GitHub Actions CI, Playwright E2E, coverage artefacts.

## Report references

- Final report §4.1.1 / §4.2.1 / §4.8.1
- Appendix: screenshot of `burndown-board.html`
