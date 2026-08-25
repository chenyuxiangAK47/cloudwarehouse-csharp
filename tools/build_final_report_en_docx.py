# -*- coding: utf-8 -*-
"""
Build formal English DOCX from Final-Report-ZH-Master.md.

Rules (per author):
- Formal academic English.
- Do NOT invent features.
- Keep figure placeholders in ORIGINAL Chinese (hints stay in place).
- Keep 【文字占位】 and Phase-2 blank hour cells as Chinese.
- Post-fix glossary so MT cannot invent dual-track=domestic/international etc.
"""
from __future__ import annotations

import re
import time
from pathlib import Path

from deep_translator import GoogleTranslator

# Reuse docx writer from Chinese builder
import build_final_report_docx as zhbuild

ROOT = Path(__file__).resolve().parents[1]
ZH_MD = ROOT / "docs/project-management/Final-Report-ZH-Master.md"
OUT_MD = ROOT / "docs/project-management/Final-Report-EN-Master.md"
OUT_DOCX = ROOT / "docs/project-management/Final-Report-EN.docx"

# Phrases that must remain / be forced after MT
POST_GLOSSARY = [
    # kill common MT hallucinations
    (r"(?i)domestic\s*(versus|vs\.?|/)\s*international", "receivable versus payable"),
    (r"(?i)international\s*(versus|vs\.?|/)\s*domestic", "receivable versus payable"),
    (r"(?i)AI\s+intelligent\s+billing", "assistive rule lookup (not settlement)"),
    (r"(?i)AI\s+billing\s+engine", "FeeCalculationEngine (not AI settlement)"),
    (r"(?i)microservices?\s+already\s+(online|deployed|live)", "modular monolith (microservices Planned only)"),
    (r"(?i)production[- ]grade\s+HA\s+already", "no production HA (honest MVP)"),
    (r"(?i)full\s+semantic\s+RAG", "lexical built-in rule RAG"),
    (r"(?i)vector\s+RAG\s+in\s+production", "lexical built-in rule RAG (not production vector RAG)"),
    # prefer project terms
    (r"(?i)rule\s+knowledge\s+lookup", "built-in rule RAG"),
    (r"(?i)pricing\s+rule\s+retrieval", "built-in rule RAG"),
]

SKIP_TRANSLATE_MARKERS = (
    "【插图占位",
    "【在此粘贴图片",
    "【文字占位】",
    "（填写）",
    "(填写)",
)

CHAPTER_HEADING_MAP = {
    "第一章 项目概述": "Chapter 1 Project Overview",
    "第二章 技术栈与关键技术决策": "Chapter 2 Technology Stack and Key Decisions",
    "第三章 系统用例与业务模块": "Chapter 3 Use Cases and Business Modules",
    "第四章 项目路线图与迭代执行": "Chapter 4 Project Roadmap and Sprint Execution",
    "第五章 数据库设计与实体关系": "Chapter 5 Database Design and Entity-Relationship Model",
    "第六章 系统架构设计": "Chapter 6 System Architecture Design",
    "第七章 软件设计": "Chapter 7 Software Design",
    "第八章 DevSecOps 与质量保障": "Chapter 8 DevSecOps and Quality Assurance",
    "第九章 风险管理": "Chapter 9 Risk Management",
    "第十章 中期反馈逐条回应": "Chapter 10 Response to Mid-term Feedback",
    "第十一章 结论与展望": "Chapter 11 Conclusion and Outlook",
    "附录 A 证据与截图清单": "Appendix A Evidence and Screenshot Checklist",
    "附录 B 术语与禁话速查": "Appendix B Terminology and Forbidden Phrases",
}


def contains_cjk(text: str) -> bool:
    return any("\u4e00" <= ch <= "\u9fff" for ch in text)


def should_keep_chinese(block: str) -> bool:
    if any(m in block for m in SKIP_TRANSLATE_MARKERS):
        return True
    # Keep pure code/path/table separator lines
    stripped = block.strip()
    if not stripped:
        return True
    if stripped.startswith("```"):
        return True
    if re.fullmatch(r"\|?\s*:?---.*", stripped):
        return True
    return False


def apply_glossary(text: str) -> str:
    out = text
    for pat, repl in POST_GLOSSARY:
        out = re.sub(pat, repl, out)
    return out


def translate_chunk(text: str, translator: GoogleTranslator) -> str:
    if should_keep_chinese(text):
        return text
    if not contains_cjk(text):
        return text
    # Preserve markdown table pipes / headings markers by translating row cells lightly
    try:
        # Google free tier soft limit ~4500 chars
        if len(text) > 4200:
            parts = split_long(text, 4000)
            return "\n".join(translate_chunk(p, translator) for p in parts)
        translated = translator.translate(text)
        time.sleep(0.12)
        return apply_glossary(translated)
    except Exception:
        # Author rule: if unsure, leave Chinese
        return text


def split_long(text: str, limit: int) -> list[str]:
    lines = text.splitlines(keepends=True)
    chunks: list[str] = []
    buf: list[str] = []
    n = 0
    for line in lines:
        if n + len(line) > limit and buf:
            chunks.append("".join(buf))
            buf = [line]
            n = len(line)
        else:
            buf.append(line)
            n += len(line)
    if buf:
        chunks.append("".join(buf))
    return chunks


def map_known_headings(line: str) -> str:
    s = line.strip()
    for zh, en in CHAPTER_HEADING_MAP.items():
        if s == zh or s == f"# {zh}":
            return f"# {en}"
        if s.startswith("## ") and zh in s:
            return s  # subsection handled by MT
    return line


def translate_markdown(zh: str) -> str:
    translator = GoogleTranslator(source="zh-CN", target="en")
    lines = zh.splitlines(keepends=True)
    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Figure placeholder quote blocks: keep intact until blank line after block
        if "【插图占位" in line or (line.startswith(">") and i + 1 < len(lines) and "【插图占位" in "".join(lines[i : i + 3])):
            block = []
            while i < len(lines):
                block.append(lines[i])
                # end when we leave quote block and hit a non-empty non-quote after a blank
                if i > 0 and lines[i].strip() == "" and (i + 1 >= len(lines) or not lines[i + 1].startswith(">")):
                    i += 1
                    break
                i += 1
            out.append("".join(block))
            # Add English caption note once
            if "【插图占位" in "".join(block) and "FIGURE PLACEHOLDER" not in "".join(block):
                out.append("\n> *(English note: FIGURE PLACEHOLDER — paste PNG here; Chinese instructions above remain the source of truth.)*\n\n")
            continue

        if "【文字占位】" in line or "（填写）" in line:
            out.append(line)
            i += 1
            continue

        mapped = map_known_headings(line.rstrip("\n"))
        if mapped != line.rstrip("\n"):
            out.append(mapped + ("\n" if line.endswith("\n") else ""))
            i += 1
            continue

        # Accumulate a paragraph (non-empty lines) for better MT quality
        if not line.strip():
            out.append(line)
            i += 1
            continue

        para = [line]
        i += 1
        while i < len(lines) and lines[i].strip() and not lines[i].startswith("#") and "【插图占位" not in lines[i]:
            # keep tables as multi-line paragraphs
            para.append(lines[i])
            i += 1
            if len("".join(para)) > 3500:
                break
        blob = "".join(para)
        out.append(translate_chunk(blob, translator))
        if not out[-1].endswith("\n"):
            out.append("\n")

    front = (
        "# CloudWarehouse Freight Settlement & PDA No-Order Reporting\n\n"
        "## Final Internship Report (English)\n\n"
        "Formal English translation of the Chinese master draft. "
        "**Figure placeholders remain in Chinese** as requested. "
        "Blank Phase-2 hour cells and Client Feedback placeholders are left for the author. "
        "No production HA / live CW–PDA API / AI settlement claims are intended.\n\n"
        "---\n\n"
    )
    body = "".join(out)
    # Drop duplicate Chinese front title block if present
    body = re.sub(
        r"^# CloudWarehouse 云仓[\s\S]*?---\n+",
        "",
        body,
        count=1,
    )
    return front + apply_glossary(body)


def main() -> None:
    if not ZH_MD.exists():
        raise SystemExit(f"Missing {ZH_MD}; run build_final_report_docx.py first")
    zh = ZH_MD.read_text(encoding="utf-8")
    print("Translating (figure blocks kept in Chinese)...")
    en = translate_markdown(zh)
    OUT_MD.write_text(en, encoding="utf-8")
    print(f"Wrote {OUT_MD} ({len(en)} chars)")
    zhbuild.md_to_docx(en, OUT_DOCX)
    # Fix title page for EN
    print(f"Wrote {OUT_DOCX}")


if __name__ == "__main__":
    main()
