#!/usr/bin/env python3
"""Build the Foothills Forest School OG card (1200x630) using the real
hand-drawn mountains+pines logo extracted from the business card PDF."""
from PIL import Image, ImageDraw, ImageFont
import numpy as np

W, H = 1200, 630

# --- Brand palette (from wwwroot/css/style.css) ---
GREEN_DARK = (74, 103, 65)
GREEN_MID  = (93, 107, 71)
BROWN      = (107, 76, 42)
CREAM_TOP  = (251, 247, 239)
CREAM_BOT  = (232, 238, 216)   # --green-pale

AVENIR = '/System/Library/Fonts/Avenir Next.ttc'
CHALK  = '/System/Library/Fonts/Supplemental/ChalkboardSE.ttc'

# --- 1. Extract the line-art logo from the rendered business card ---
src = Image.open('bizcard_hi-1.png').convert('RGB')
arr = np.asarray(src).astype(int)
lum = arr.sum(2)
BG = 233  # background luminance of the card
# crop to the art bbox (found via earlier scan) with a little padding
x0, y0, x1, y1 = 230, 448, 1693, 1253
crop = lum[y0:y1, x0:x1]
# alpha = how much darker than background (anti-aliased lines preserved)
alpha = np.clip((BG - crop) / 150.0, 0, 1)
ch, cw = alpha.shape
# recolor the line art to brand dark green
logo = np.zeros((ch, cw, 4), dtype=np.uint8)
logo[..., 0] = GREEN_DARK[0]
logo[..., 1] = GREEN_DARK[1]
logo[..., 2] = GREEN_DARK[2]
logo[..., 3] = (alpha * 255).astype(np.uint8)
logo_img = Image.fromarray(logo, 'RGBA')

# scale logo to a tasteful width
target_w = 940
scale = target_w / cw
logo_img = logo_img.resize((target_w, int(ch * scale)), Image.LANCZOS)

# --- 2. Build the cream gradient canvas ---
canvas = Image.new('RGB', (W, H))
top = np.array(CREAM_TOP); bot = np.array(CREAM_BOT)
grad = np.zeros((H, W, 3), dtype=np.uint8)
for y in range(H):
    t = y / (H - 1)
    grad[y, :, :] = (top * (1 - t) + bot * t).astype(np.uint8)
canvas = Image.fromarray(grad, 'RGB')
draw = ImageDraw.Draw(canvas)

# soft rounded border frame for a "card" feel
draw.rounded_rectangle([14, 14, W - 15, H - 15], radius=34,
                       outline=GREEN_DARK, width=4)

# --- 3. Sun + tiny birds (cute touches) ---
draw.ellipse([W - 150, 56, W - 78, 128], fill=(251, 213, 122))
for i, (bx, by) in enumerate([(170, 150), (250, 130), (320, 165)]):
    draw.arc([bx, by, bx + 26, by + 16], 200, 340, fill=GREEN_MID, width=3)
    draw.arc([bx + 22, by, bx + 48, by + 16], 200, 340, fill=GREEN_MID, width=3)

# --- 4. Place the hand-drawn logo near the bottom ---
lw, lh = logo_img.size
lx = (W - lw) // 2
ly = H - lh - 24
canvas.paste(logo_img, (lx, ly), logo_img)

# --- 5. Wordmark + tagline + location pill ---
def font(path, size, index=0):
    return ImageFont.truetype(path, size, index=index)

def center_text(y, text, fnt, fill, ls=0):
    if ls:
        # manual letter spacing
        widths = [draw.textbbox((0, 0), c, font=fnt)[2] for c in text]
        total = sum(widths) + ls * (len(text) - 1)
        x = (W - total) / 2
        for c, w in zip(text, widths):
            draw.text((x, y), c, font=fnt, fill=fill)
            x += w + ls
    else:
        bb = draw.textbbox((0, 0), text, font=fnt)
        draw.text(((W - (bb[2] - bb[0])) / 2 - bb[0], y), text, font=fnt, fill=fill)

# Wordmark
wm = font(AVENIR, 76, index=2)  # Demi Bold (matches the airy card wordmark)
center_text(70, 'Foothills Forest School', wm, GREEN_DARK, ls=1)

# Cute hand-drawn tagline
tag = font(CHALK, 33, index=1)  # Chalkboard SE Regular
center_text(168, 'Inspiring a love for nature & lifelong learning', tag, BROWN)

# Location pill
pill = font(AVENIR, 23, index=0)  # Bold
ptxt = 'Montvale Springs  ·  Maryville, Tennessee'
bb = draw.textbbox((0, 0), ptxt, font=pill)
ptw = bb[2] - bb[0]
pad = 26
pw = ptw + pad * 2
px = (W - pw) / 2
py = 232
draw.rounded_rectangle([px, py, px + pw, py + 46], radius=23, fill=GREEN_DARK)
draw.text(((W - ptw) / 2 - bb[0], py + 9), ptxt, font=pill, fill=CREAM_TOP)

canvas.save('../wwwroot/images/og-card.png', 'PNG')
canvas.save('og-card-preview.png', 'PNG')
print('wrote wwwroot/images/og-card.png', canvas.size)
