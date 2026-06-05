#!/usr/bin/env python3
"""Build a compact logo badge (hand-drawn mark + name) for the landing page."""
from PIL import Image, ImageDraw, ImageFont
import numpy as np

GREEN_DARK = (74, 103, 65)
BROWN      = (107, 76, 42)
CREAM      = (251, 247, 239)
AVENIR = '/System/Library/Fonts/Avenir Next.ttc'

# --- extract the line-art mark from the rendered business card ---
src = Image.open('bizcard_hi-1.png').convert('RGB')
arr = np.asarray(src).astype(int)
lum = arr.sum(2)
BG = 233
x0, y0, x1, y1 = 230, 448, 1693, 1253
crop = lum[y0:y1, x0:x1]
alpha = np.clip((BG - crop) / 150.0, 0, 1)
ch, cw = alpha.shape
mark = np.zeros((ch, cw, 4), dtype=np.uint8)
mark[..., 0], mark[..., 1], mark[..., 2] = GREEN_DARK
mark[..., 3] = (alpha * 255).astype(np.uint8)
mark_img = Image.fromarray(mark)

# --- badge canvas (transparent, rounded cream panel) ---
S = 4  # supersample
W, Hh = 300, 250
cv = Image.new('RGBA', (W * S, Hh * S), (0, 0, 0, 0))
d = ImageDraw.Draw(cv)
d.rounded_rectangle([2 * S, 2 * S, (W - 2) * S, (Hh - 2) * S], radius=28 * S,
                    fill=CREAM + (242,), outline=GREEN_DARK + (255,), width=3 * S)

# place mark
mw = (W - 44) * S
ms = mw / cw
mark_r = mark_img.resize((int(cw * ms), int(ch * ms)), Image.LANCZOS)
cv.alpha_composite(mark_r, (22 * S, 26 * S))

# name (two lines)
def fnt(sz, idx=2):
    return ImageFont.truetype(AVENIR, sz * S, index=idx)

def ctext(y, txt, f, fill):
    bb = d.textbbox((0, 0), txt, font=f)
    d.text(((W * S - (bb[2] - bb[0])) / 2 - bb[0], y * S), txt, font=f, fill=fill)

ctext(182, 'Foothills Forest School', fnt(23, 2), GREEN_DARK + (255,))
ctext(214, 'Maryville, Tennessee', fnt(15, 5), BROWN + (255,))

cv = cv.resize((W, Hh), Image.LANCZOS)
cv.save('../wwwroot/images/logo-badge.png')
cv.save('logo-badge-preview.png')
print('wrote wwwroot/images/logo-badge.png', cv.size)
