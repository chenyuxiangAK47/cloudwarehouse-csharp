# -*- coding: utf-8 -*-
"""Strip AI placeholder chrome from Final-Report-ZH-Master.md."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
P = ROOT / "docs/project-management/Final-Report-ZH-Master.md"


def main() -> None:
    text = P.read_text(encoding="utf-8")

    text = re.sub(
        r"^# CloudWarehouse 云仓运费结算 & PDA 无订单报工\n\n"
        r"## 最终实习报告（中文整理稿）\n\n"
        r"本文件由草稿自动整理生成：.*?\n\n---\n\n",
        "# CloudWarehouse 云仓运费结算与 PDA 无订单报工\n\n"
        "**最终实习报告**\n\n",
        text,
        count=1,
        flags=re.S,
    )

    fig_pat = re.compile(
        r"> \*\*【插图占位 ([^】]+)】\*\* [^\n]*\n"
        r"(?:> [^\n]*\n)*"
        r">\n"
        r"> ```\n"
        r"(?:> [^\n]*\n)*"
        r"> ```\n"
        r">\n"
        r"> \*图注：([^*]+)\*\n*",
        re.M,
    )

    def fig_repl(m: re.Match[str]) -> str:
        fid, cap = m.group(1).strip(), m.group(2).strip()
        return f"**图 {fid}** {cap}\n\n"

    text, nfig = fig_pat.subn(fig_repl, text)
    print("figures replaced:", nfig)

    for sym in ("✅ ", "✅", "🔄 ", "🔄", "❌ ", "❌", "⚠️ ", "⚠️"):
        text = text.replace(sym, "")

    broken = (
        "| 交付物名称 | 状态 | 备注 |\n"
        "| --- | --- | --- |\n\n"
        "CloudWarehouse 系统\t已完成\t包含导入、试算、运单双轨对账功能\n"
        "计费策略模式实现\t已完成\t包含类图、时序图及详细设计\n"
        "CI/CD 与质量扫描\t已完成\t包含自动化测试套件与 CodeQL 集成\n"
        "内置规则 RAG\t已完成\t内置规则 RAG：查阅 FAQ，不参与结算\n"
        "PDA 无订单报工 MVP\t已完成\t霍尼韦尔 PDA 端应用\n"
        "最终演示视频/报告\t进行中\t本交付物包含 7 段演示视频"
    )
    fixed = (
        "| 交付物名称 | 状态 | 备注 |\n"
        "| --- | --- | --- |\n"
        "| CloudWarehouse 系统 | 已完成 | 包含导入、试算、运单双轨对账功能 |\n"
        "| 计费策略模式实现 | 已完成 | 包含类图、时序图及详细设计 |\n"
        "| CI/CD 与质量扫描 | 已完成 | 包含自动化测试套件与 CodeQL 集成 |\n"
        "| 内置规则 RAG | 已完成 | 查阅 FAQ，不参与结算 |\n"
        "| PDA 无订单报工 MVP | 已完成 | 霍尼韦尔 PDA 端应用 |\n"
        "| 最终演示视频/报告 | 进行中 | 含 7 段演示视频 |"
    )
    if broken in text:
        text = text.replace(broken, fixed)
        print("delivery table fixed")
    else:
        m = re.search(
            r"\| 交付物名称 \| 状态 \| 备注 \|\n\| --- \| --- \| --- \|\n\n"
            r"(?:[^\n]+\t[^\n]+\n){5}[^\n]+演示[^\n]+",
            text,
        )
        if m:
            text = text[: m.start()] + fixed + text[m.end() :]
            print("delivery table fixed via regex")
        else:
            print("delivery table pattern not found")

    text = text.replace(
        "### 11.4 Client Feedback（占位）\n\n"
        "> **【文字占位】** 请补充企业导师/业务方演示反馈摘要（日期、意见、已闭环项）。"
        "用于 Client Feedback 评分证据。\n",
        "### 11.4 Client Feedback\n\n"
        "（待企业导师补充演示反馈摘要：日期、主要意见、已闭环项。）\n",
    )

    text = text.replace("## Conclusion and Outlook\n\n", "")
    text = text.replace(
        "### 11.5 提交清单\n\n"
        "- [ ] 本报告（中文定稿 + 插图）\n"
        "- [ ] 英文版（由定稿翻译，勿重写事实）\n"
        "- [ ] 7 段评估视频\n"
        "- [ ] 附录截图齐套（见附录 A）\n",
        "### 11.5 提交物\n\n"
        "- 本报告（中文定稿）\n"
        "- 英文版\n"
        "- 评估演示视频\n"
        "- 附录证据截图（见附录 A）\n",
    )
    text = text.replace(
        "请将下列截图按编号贴入附录（每项半页起）：\n\n",
        "附录证据一览：\n\n",
    )
    text = text.replace(
        "一张来自PDA显示成功报工的截图（TODO: 截图）。",
        "一张来自 PDA 显示成功报工的截图。",
    )
    text = text.replace("（TODO: 截图）", "")
    text = text.replace("▼▼▼ 在下方空白处粘贴图片 ▼▼▼", "")
    text = re.sub(r"\n{3,}", "\n\n", text)

    leftovers = [
        "整理稿",
        "Master Draft",
        "插图占位",
        "在此粘贴",
        "导出 PNG 后粘贴",
        "文字占位",
        "红色提示",
        "✅",
        "🔄",
    ]
    for bad in leftovers:
        if bad in text:
            print("still has:", bad)

    P.write_text(text, encoding="utf-8")
    print("wrote", P)


if __name__ == "__main__":
    main()
