import sys
from pathlib import Path
from PIL import Image, ImageDraw

src = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r"D:\delivery-backend\.codex-doc-work\ppt-name-update\before")
out = Path(sys.argv[2]) if len(sys.argv) > 2 else src.parent / f"{src.name}-contact"
out.mkdir(parents=True, exist_ok=True)
files = sorted(src.glob("slide-*.png"))
thumb_w, thumb_h = 320, 180
cols, rows = 4, 3
label_h = 24
per_sheet = cols * rows

for sheet_idx in range((len(files) + per_sheet - 1) // per_sheet):
    canvas = Image.new("RGB", (cols * thumb_w, rows * (thumb_h + label_h)), "white")
    draw = ImageDraw.Draw(canvas)
    for offset, file in enumerate(files[sheet_idx * per_sheet : (sheet_idx + 1) * per_sheet]):
        x = (offset % cols) * thumb_w
        y = (offset // cols) * (thumb_h + label_h)
        with Image.open(file) as im:
            canvas.paste(im.convert("RGB").resize((thumb_w, thumb_h)), (x, y))
        draw.text((x + 8, y + thumb_h + 3), file.stem, fill="black")
    canvas.save(out / f"contact-{sheet_idx + 1}.png")
