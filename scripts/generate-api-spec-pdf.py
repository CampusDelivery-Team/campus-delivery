from __future__ import annotations

import re
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    PageBreak,
    Paragraph,
    Preformatted,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs" / "api-frontend-backend-spec.md"
OUTPUT_DIR = ROOT / "output" / "pdf"
OUTPUT = OUTPUT_DIR / "api-frontend-backend-spec.pdf"


def register_fonts() -> tuple[str, str]:
    font_candidates = [
        Path("C:/Windows/Fonts/simsun.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/msyh.ttc"),
    ]
    bold_candidates = [
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/msyhbd.ttc"),
        Path("C:/Windows/Fonts/simsunb.ttf"),
    ]

    normal = next((p for p in font_candidates if p.exists()), None)
    bold = next((p for p in bold_candidates if p.exists()), normal)

    if normal is None:
        return "Helvetica", "Helvetica-Bold"

    pdfmetrics.registerFont(TTFont("ChineseNormal", str(normal)))
    pdfmetrics.registerFont(TTFont("ChineseBold", str(bold)))
    return "ChineseNormal", "ChineseBold"


def make_styles() -> dict[str, ParagraphStyle]:
    normal_font, bold_font = register_fonts()
    base = getSampleStyleSheet()

    return {
        "title": ParagraphStyle(
            "Title",
            parent=base["Title"],
            fontName=bold_font,
            fontSize=20,
            leading=28,
            alignment=TA_CENTER,
            spaceAfter=14,
            textColor=colors.HexColor("#111827"),
        ),
        "h1": ParagraphStyle(
            "Heading1",
            parent=base["Heading1"],
            fontName=bold_font,
            fontSize=15,
            leading=22,
            spaceBefore=10,
            spaceAfter=8,
            textColor=colors.HexColor("#1f2937"),
        ),
        "h2": ParagraphStyle(
            "Heading2",
            parent=base["Heading2"],
            fontName=bold_font,
            fontSize=12.5,
            leading=18,
            spaceBefore=8,
            spaceAfter=6,
            textColor=colors.HexColor("#374151"),
        ),
        "body": ParagraphStyle(
            "Body",
            parent=base["BodyText"],
            fontName=normal_font,
            fontSize=9.5,
            leading=15,
            alignment=TA_LEFT,
            spaceAfter=5,
        ),
        "small": ParagraphStyle(
            "Small",
            parent=base["BodyText"],
            fontName=normal_font,
            fontSize=8,
            leading=12,
        ),
        "code": ParagraphStyle(
            "Code",
            parent=base["Code"],
            fontName=normal_font,
            fontSize=8.5,
            leading=12,
            leftIndent=4,
            rightIndent=4,
            backColor=colors.HexColor("#f3f4f6"),
            borderPadding=5,
        ),
    }


def clean_inline(text: str) -> str:
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    text = re.sub(r"`([^`]+)`", r"<font color='#0f766e'>\1</font>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    return text


def split_table_row(line: str) -> list[str]:
    return [cell.strip() for cell in line.strip().strip("|").split("|")]


def markdown_to_story(markdown: str, styles: dict[str, ParagraphStyle]):
    story = []
    lines = markdown.splitlines()
    i = 0
    in_code = False
    code_lines: list[str] = []

    while i < len(lines):
        line = lines[i]

        if line.startswith("```"):
            if in_code:
                story.append(Preformatted("\n".join(code_lines), styles["code"]))
                story.append(Spacer(1, 5))
                code_lines = []
                in_code = False
            else:
                in_code = True
            i += 1
            continue

        if in_code:
            code_lines.append(line)
            i += 1
            continue

        if not line.strip():
            i += 1
            continue

        if line.startswith("# "):
            story.append(Paragraph(clean_inline(line[2:].strip()), styles["title"]))
            story.append(Spacer(1, 8))
            i += 1
            continue

        if line.startswith("## "):
            story.append(Paragraph(clean_inline(line[3:].strip()), styles["h1"]))
            i += 1
            continue

        if line.startswith("### "):
            story.append(Paragraph(clean_inline(line[4:].strip()), styles["h2"]))
            i += 1
            continue

        if line.startswith("|") and i + 1 < len(lines) and lines[i + 1].startswith("|"):
            table_lines = []
            while i < len(lines) and lines[i].startswith("|"):
                table_lines.append(lines[i])
                i += 1

            rows = [split_table_row(row) for row in table_lines]
            if len(rows) >= 2 and all(re.match(r"^:?-{3,}:?$", cell) for cell in rows[1]):
                rows = [rows[0]] + rows[2:]

            data = [
                [Paragraph(clean_inline(cell), styles["small"]) for cell in row]
                for row in rows
            ]
            col_count = max(len(row) for row in rows)
            table = Table(data, repeatRows=1, hAlign="LEFT")
            table.setStyle(
                TableStyle(
                    [
                        ("FONTNAME", (0, 0), (-1, -1), styles["small"].fontName),
                        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#e5e7eb")),
                        ("TEXTCOLOR", (0, 0), (-1, 0), colors.HexColor("#111827")),
                        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#d1d5db")),
                        ("VALIGN", (0, 0), (-1, -1), "TOP"),
                        ("LEFTPADDING", (0, 0), (-1, -1), 4),
                        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
                        ("TOPPADDING", (0, 0), (-1, -1), 4),
                        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                    ]
                )
            )
            if col_count >= 5:
                table._argW = [22 * mm, 48 * mm, 32 * mm, 42 * mm, 25 * mm][:col_count]
            story.append(table)
            story.append(Spacer(1, 6))
            continue

        if re.match(r"^\d+\.\s+", line):
            item = re.sub(r"^\d+\.\s+", "", line)
            story.append(Paragraph("• " + clean_inline(item), styles["body"]))
            i += 1
            continue

        if line.startswith("- "):
            story.append(Paragraph("• " + clean_inline(line[2:].strip()), styles["body"]))
            i += 1
            continue

        story.append(Paragraph(clean_inline(line.strip()), styles["body"]))
        i += 1

    story.append(PageBreak())
    return story


def draw_footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("ChineseNormal" if "ChineseNormal" in pdfmetrics.getRegisteredFontNames() else "Helvetica", 8)
    canvas.setFillColor(colors.HexColor("#6b7280"))
    canvas.drawString(20 * mm, 12 * mm, "校园跑腿系统接口与前后端分离规范文档")
    canvas.drawRightString(190 * mm, 12 * mm, f"第 {doc.page} 页")
    canvas.restoreState()


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    styles = make_styles()
    markdown = SOURCE.read_text(encoding="utf-8")
    story = markdown_to_story(markdown, styles)
    if story and isinstance(story[-1], PageBreak):
        story = story[:-1]

    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=A4,
        rightMargin=16 * mm,
        leftMargin=16 * mm,
        topMargin=16 * mm,
        bottomMargin=18 * mm,
        title="校园跑腿系统接口与前后端分离规范文档",
        author="组员1",
    )
    doc.build(story, onFirstPage=draw_footer, onLaterPages=draw_footer)
    print(OUTPUT)


if __name__ == "__main__":
    main()
