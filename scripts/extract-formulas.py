import re
import sys
import xml.etree.ElementTree as ET
import zipfile

path = sys.argv[1]
cells = sys.argv[2:] if len(sys.argv) > 2 else ["K3", "L3", "X3", "X6"]

with zipfile.ZipFile(path) as z:
    ns = {
        "m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
        "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    }
    wb = ET.fromstring(z.read("xl/workbook.xml"))
    rels = ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
    relmap = {r.get("Id"): r.get("Target") for r in rels}
    idmap = {}
    for s in wb.find("m:sheets", ns):
        name = s.get("name")
        rid = s.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
        target = relmap.get(rid, "")
        if target.startswith("/"):
            target = target.lstrip("/")
        if not target.startswith("xl/"):
            target = "xl/" + target.lstrip("/")
        idmap[name] = target

    for sheet_name in ["账单模版 - 账单明细", "2026-01 - 账单明细"]:
        target = idmap.get(sheet_name)
        if not target:
            print("missing", sheet_name)
            continue
        xml = z.read(target).decode("utf-8", "replace")
        print("\n===", sheet_name, "===")
        for cell in cells:
            pat = rf'<c r="{cell}"[^>]*>.*?</c>'
            m = re.search(pat, xml, re.DOTALL)
            if m:
                snippet = m.group(0).replace("\n", " ")[:800]
                print(cell, ":", snippet)
            else:
                print(cell, ": not found")
