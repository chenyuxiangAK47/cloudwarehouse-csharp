#!/usr/bin/env python3
"""Build a static QA report page for GitHub Pages from CI test + coverage outputs."""

from __future__ import annotations

import html
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path


def parse_test_lines(text: str) -> list[dict[str, int | str]]:
    rows: list[dict[str, int | str]] = []

    # VSTest one-liner (Linux CI / English): Passed! - Failed: 0, Passed: 83, ...
    en_pattern = re.compile(
        r"(?:Passed!|Failed!)\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)"
    )
    for line in text.splitlines():
        m = en_pattern.search(line)
        if not m:
            continue
        failed, passed, skipped, total = map(int, m.groups())
        name = "Tests"
        if ".dll" in line:
            dll = re.search(r"-\s+(\S+\.dll)", line)
            if dll:
                name = dll.group(1).replace(".dll", "")
        rows.append(
            {
                "name": name,
                "failed": failed,
                "passed": passed,
                "skipped": skipped,
                "total": total,
            }
        )

    if rows:
        return rows

    # VSTest multi-line block (common on ubuntu-latest with normal verbosity):
    #   Total tests: 83
    #        Passed: 83
    #        Failed: 0
    #       Skipped: 0
    names = [
        "CloudWarehouse.Tests",
        "CloudWarehouse.IntegrationTests",
        "CloudWarehouse.E2ETests",
    ]
    block_re = re.compile(
        r"Total tests:\s*(\d+)\s*\n\s*Passed:\s*(\d+)\s*\n\s*Failed:\s*(\d+)\s*\n\s*Skipped:\s*(\d+)",
        re.MULTILINE,
    )
    for i, m in enumerate(block_re.finditer(text)):
        total, passed, failed, skipped = map(int, m.groups())
        rows.append(
            {
                "name": names[i] if i < len(names) else f"TestProject{i + 1}",
                "failed": failed,
                "passed": passed,
                "skipped": skipped,
                "total": total,
            }
        )
    if rows:
        return rows

    # Chinese `dotnet test` console output (Windows)
    # Variants: 通过数 / 已通过 ; 失败数 / 失败
    zh_total = re.findall(r"测试总数:\s*(\d+)", text)
    zh_passed = re.findall(r"(?:通过数|已通过):\s*(\d+)", text)
    zh_failed = re.findall(r"(?:失败数|失败):\s*(\d+)", text)
    zh_skipped = re.findall(r"(?:跳过数|已跳过):\s*(\d+)", text)
    for i, total_s in enumerate(zh_total):
        total = int(total_s)
        passed = int(zh_passed[i]) if i < len(zh_passed) else total
        failed = int(zh_failed[i]) if i < len(zh_failed) else max(total - passed, 0)
        skipped = int(zh_skipped[i]) if i < len(zh_skipped) else 0
        name = names[i] if i < len(names) else f"TestProject{i + 1}"
        rows.append(
            {
                "name": name,
                "failed": failed,
                "passed": passed,
                "skipped": skipped,
                "total": total,
            }
        )

    # Single-line summary fallback
    if not rows:
        m = re.search(
            r"测试摘要:.*?总计:\s*(\d+).*?失败:\s*(\d+).*?成功:\s*(\d+).*?已跳过:\s*(\d+)",
            text,
        )
        if m:
            total, failed, passed, skipped = map(int, m.groups())
            rows.append(
                {
                    "name": "CloudWarehouse.sln",
                    "failed": failed,
                    "passed": passed,
                    "skipped": skipped,
                    "total": total,
                }
            )

    return rows


def extract_perf_lines(text: str) -> list[str]:
    return [ln.strip() for ln in text.splitlines() if "[PERF]" in ln]


def read_optional(path: Path) -> str:
    if not path.is_file():
        return "(not available)"
    for enc in ("utf-8", "utf-8-sig", "utf-16", "utf-16-le", "gbk"):
        try:
            return path.read_text(encoding=enc)
        except UnicodeError:
            continue
    return path.read_text(encoding="utf-8", errors="replace")


def build_html(
    *,
    test_rows: list[dict[str, int | str]],
    perf_lines: list[str],
    coverage_summary: str,
    vuln_scan: str,
    e2e_log: str,
    sha: str,
    ref: str,
    run_id: str,
    run_url: str,
) -> str:
    total_passed = sum(int(r["passed"]) for r in test_rows)
    total_failed = sum(int(r["failed"]) for r in test_rows)
    total_skipped = sum(int(r["skipped"]) for r in test_rows)
    total_tests = sum(int(r["total"]) for r in test_rows)
    built_at = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")

    project_rows = "\n".join(
        f"<tr><td>{html.escape(str(r['name']))}</td>"
        f"<td>{r['passed']}</td><td>{r['failed']}</td>"
        f"<td>{r['skipped']}</td><td>{r['total']}</td></tr>"
        for r in test_rows
    )
    perf_block = (
        "<ul>"
        + "".join(f"<li><code>{html.escape(ln)}</code></li>" for ln in perf_lines)
        + "</ul>"
        if perf_lines
        else "<p>No [PERF] lines in this run.</p>"
    )

    status_class = "ok" if total_failed == 0 else "fail"
    status_text = "ALL PASSED" if total_failed == 0 else f"{total_failed} FAILED"

    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>CloudWarehouse QA Report</title>
  <style>
    :root {{ font-family: system-ui, Segoe UI, sans-serif; color: #1a1a1a; }}
    body {{ max-width: 960px; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }}
    h1 {{ margin-bottom: 0.25rem; }}
    .meta {{ color: #555; font-size: 0.95rem; margin-bottom: 1.5rem; }}
    .banner {{ padding: 1rem 1.25rem; border-radius: 8px; margin: 1rem 0 1.5rem; font-weight: 600; }}
    .banner.ok {{ background: #e8f5e9; border: 1px solid #a5d6a7; }}
    .banner.fail {{ background: #ffebee; border: 1px solid #ef9a9a; }}
    table {{ border-collapse: collapse; width: 100%; margin: 1rem 0; }}
    th, td {{ border: 1px solid #ddd; padding: 0.5rem 0.75rem; text-align: left; }}
    th {{ background: #f5f5f5; }}
    pre {{ background: #f8f8f8; border: 1px solid #e0e0e0; padding: 1rem; overflow-x: auto; font-size: 0.85rem; }}
    a {{ color: #0969da; }}
    code {{ font-size: 0.9em; }}
  </style>
</head>
<body>
  <h1>CloudWarehouse QA Report</h1>
  <p class="meta">
    Built: {html.escape(built_at)} · Branch: <code>{html.escape(ref)}</code> ·
    Commit: <code>{html.escape(sha[:12] if sha else "local")}</code> ·
    <a href="{html.escape(run_url)}">GitHub Actions run #{html.escape(run_id)}</a>
  </p>

  <div class="banner {status_class}">
    {status_text} — {total_passed} passed, {total_failed} failed,
    {total_skipped} skipped, {total_tests} total
  </div>

  <h2>Test results by project</h2>
  <table>
    <thead><tr><th>Project</th><th>Passed</th><th>Failed</th><th>Skipped</th><th>Total</th></tr></thead>
    <tbody>
      {project_rows}
      <tr><th>Sum</th><th>{total_passed}</th><th>{total_failed}</th><th>{total_skipped}</th><th>{total_tests}</th></tr>
    </tbody>
  </table>

  <h2>Performance smoke</h2>
  {perf_block}

  <h2>Playwright UI E2E (§8.3.2)</h2>
  <p>
    Formal CI artefact:
    <a href="e2e/e2e-playwright-test.txt">e2e/e2e-playwright-test.txt</a>
    · Actions artefact name <code>e2e-playwright-results</code>
  </p>
  <pre>{html.escape(e2e_log.strip()[:6000])}</pre>

  <h2>Coverage summary</h2>
  <p><a href="coverage/index.html">Open full HTML coverage report</a></p>
  <pre>{html.escape(coverage_summary.strip())}</pre>

  <h2>NuGet vulnerability scan (visibility)</h2>
  <pre>{html.escape(vuln_scan.strip()[:8000])}</pre>

  <p class="meta">Auto-generated by <code>tools/generate_qa_report.py</code> on each CI run.
  Modular Monolith — not microservices. Includes Playwright UI smoke E2E (§8.3.2).</p>
</body>
</html>
"""


def main() -> int:
    test_log = Path(sys.argv[1] if len(sys.argv) > 1 else "test-output.txt")
    out_dir = Path(sys.argv[2] if len(sys.argv) > 2 else "site")
    out_dir.mkdir(parents=True, exist_ok=True)

    text = read_optional(test_log)
    rows = parse_test_lines(text)
    perf = extract_perf_lines(text)
    coverage = read_optional(Path("coveragereport/Summary.txt"))
    vuln = read_optional(Path("vulnerable-packages.txt"))
    e2e = read_optional(Path("e2e-playwright-test.txt"))

    sha = os.environ.get("GITHUB_SHA", "local")
    ref = os.environ.get("GITHUB_REF_NAME", "local")
    run_id = os.environ.get("GITHUB_RUN_ID", "0")
    server = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    repo = os.environ.get("GITHUB_REPOSITORY", "chenyuxiangAK47/cloudwarehouse-csharp")
    run_url = f"{server}/{repo}/actions/runs/{run_id}"

    page = build_html(
        test_rows=rows,
        perf_lines=perf,
        coverage_summary=coverage,
        vuln_scan=vuln,
        e2e_log=e2e,
        sha=sha,
        ref=ref,
        run_id=run_id,
        run_url=run_url,
    )
    (out_dir / "index.html").write_text(page, encoding="utf-8")
    print(f"Wrote {out_dir / 'index.html'} ({len(rows)} test project rows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
