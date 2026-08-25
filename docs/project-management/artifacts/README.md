# Test & QA artifacts (for Final Report §8)

**Primary evidence (preferred for examiners):** GitHub Actions  
https://github.com/chenyuxiangAK47/cloudwarehouse-csharp/actions  
→ workflow **CI** → latest **green** run → step **Test with coverage**

**Public QA summary page (after Pages enabled + one push):**  
https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/  
(Auto-updated after each successful CI on `main`; includes pass counts, coverage summary link, NuGet scan excerpt.)

## Summary (local reproduction, 25 Aug 2026)

| Metric | Value |
| --- | --- |
| Unit tests (`CloudWarehouse.Tests`) | **83 passed**, 0 failed |
| Integration tests (`CloudWarehouse.IntegrationTests`) | **27 passed**, 0 failed |
| **Total** | **110 passed** |
| 1000-row Excel parse (`[PERF]`) | **114 ms** (threshold &lt; 30 s) |
| 15 concurrent price-table preview | **~203 ms** (that run) |

## Files in this folder

| File | Contents |
| --- | --- |
| `dotnet-test-full.txt` | Full `dotnet test CloudWarehouse.sln` log (backup; use Actions screenshots in Word) |
| `load-smoke.txt` | Filtered perf unit tests |
| `load-stress.txt` | `StressLoadTests` only |

## Reproduce locally

```powershell
cd d:\tools\cloudwarehouse-csharp
dotnet test CloudWarehouse.sln
```

## Report cross-references

- §8.3.1 — unit/integration tables (110 passed)
- §8.7 — load/smoke numbers
- Appendix A-12 — test pass screenshot
- Appendix A-13 — `[PERF]` / StressLoadTests screenshot
