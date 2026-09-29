import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def create_cockpit_canopy(width=1920, height=1080, output_path=r"d:\Vibe Code Repo\galactic_view\unity_cosmic_engine\Assets\Textures\cockpit_canopy_overlay.png"):
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Color Palette: Elite Aerospace Gunmetal, Deep Navy, Electric Cyan, Neon Amber
    c_strut_dark = (14, 18, 26, 250)
    c_strut_mid = (24, 32, 44, 255)
    c_strut_bevel = (45, 60, 80, 255)
    c_cyan_glow = (0, 240, 255, 180)
    c_cyan_bright = (120, 255, 255, 230)
    c_amber = (255, 170, 0, 200)

    # 2. Draw Top Canopy Arch & Struts
    # Outer top arch
    top_bar_h = 44
    draw.polygon([(0, 0), (width, 0), (width, top_bar_h), (0, top_bar_h)], fill=c_strut_dark)
    # Angled top canopy struts
    draw.polygon([(0, top_bar_h), (180, top_bar_h + 30), (width - 180, top_bar_h + 30), (width, top_bar_h)], fill=c_strut_mid)
    draw.line([(0, top_bar_h + 30), (180, top_bar_h + 30), (width - 180, top_bar_h + 30), (width, top_bar_h + 30)], fill=c_cyan_glow, width=2)

    # Left A-Pillar (Angled Starfighter Cockpit Strut)
    left_strut = [
        (0, 0),
        (70, 0),
        (220, height * 0.45),
        (260, height * 0.72),
        (180, height),
        (0, height)
    ]
    draw.polygon(left_strut, fill=c_strut_dark)

    # Left Strut Bevel & Cyan Edge
    draw.line([(70, 0), (220, height * 0.45), (260, height * 0.72), (180, height)], fill=c_cyan_glow, width=3)
    draw.line([(72, 0), (222, height * 0.45), (262, height * 0.72), (182, height)], fill=c_cyan_bright, width=1)

    # Right A-Pillar (Mirrored Starfighter Cockpit Strut)
    right_strut = [
        (width, 0),
        (width - 70, 0),
        (width - 220, height * 0.45),
        (width - 260, height * 0.72),
        (width - 180, height),
        (width, height)
    ]
    draw.polygon(right_strut, fill=c_strut_dark)

    # Right Strut Bevel & Cyan Edge
    draw.line([(width - 70, 0), (width - 220, height * 0.45), (width - 260, height * 0.72), (width - 180, height)], fill=c_cyan_glow, width=3)
    draw.line([(width - 72, 0), (width - 222, height * 0.45), (width - 262, height * 0.72), (width - 182, height)], fill=c_cyan_bright, width=1)

    # 3. Bottom Dashboard Console / Avionics Coaming
    dash_top_y = height - 90
    dash_poly = [
        (0, height),
        (0, dash_top_y + 35),
        (260, dash_top_y + 15),
        (420, dash_top_y),
        (width - 420, dash_top_y),
        (width - 260, dash_top_y + 15),
        (width, dash_top_y + 35),
        (width, height)
    ]
    draw.polygon(dash_poly, fill=(10, 14, 20, 252))

    # Dashboard glowing border edge
    dash_edge = [
        (0, dash_top_y + 35),
        (260, dash_top_y + 15),
        (420, dash_top_y),
        (width - 420, dash_top_y),
        (width - 260, dash_top_y + 15),
        (width, dash_top_y + 35)
    ]
    draw.line(dash_edge, fill=c_cyan_bright, width=3)

    # Accent notches along dashboard
    for nx in [480, 640, 800, 960, 1120, 1280, 1440]:
        draw.line([(nx - 15, dash_top_y + 12), (nx + 15, dash_top_y + 12)], fill=c_amber, width=2)

    # 4. Subtle Canopy Glass Vignette & Tint (Very faint blue edge gradient, completely transparent center)
    # Radial falloff at edges
    glass_overlay = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    g_draw = ImageDraw.Draw(glass_overlay)

    # Corner brackets (HUD flight deck mounting brackets)
    # Top-Left Bracket
    g_draw.line([(240, 85), (280, 85)], fill=c_cyan_bright, width=2)
    g_draw.line([(240, 85), (240, 125)], fill=c_cyan_bright, width=2)

    # Top-Right Bracket
    g_draw.line([(width - 240, 85), (width - 280, 85)], fill=c_cyan_bright, width=2)
    g_draw.line([(width - 240, 85), (width - 240, 125)], fill=c_cyan_bright, width=2)

    # Compass Heading Tape Marks on Top Arch
    center_x = width // 2
    for offset in range(-360, 361, 30):
        tick_x = center_x + offset
        if 250 < tick_x < width - 250:
            h = 10 if offset % 90 == 0 else (6 if offset % 30 == 0 else 4)
            col = c_cyan_bright if offset % 90 == 0 else c_cyan_glow
            g_draw.line([(tick_x, top_bar_h + 16), (tick_x, top_bar_h + 16 + h)], fill=col, width=2)

    # Heading Center Caret
    g_draw.polygon([
        (center_x - 6, top_bar_h + 28),
        (center_x + 6, top_bar_h + 28),
        (center_x, top_bar_h + 36)
    ], fill=c_amber)

    # Subtle pitch ladder lines in center HUD (optically transparent)
    center_y = height // 2
    for r in [60, 120, 180]:
        # Horizon bracket left
        g_draw.line([(center_x - r - 30, center_y), (center_x - r, center_y)], fill=(0, 240, 255, 60), width=1)
        # Horizon bracket right
        g_draw.line([(center_x + r, center_y), (center_x + r + 30, center_y)], fill=(0, 240, 255, 60), width=1)

    # Composite
    final_img = Image.alpha_composite(img, glass_overlay)
    final_img.save(output_path, "PNG")
    print(f"Saved Starfighter Cockpit Canopy Overlay ({width}x{height}) to: {output_path}")

if __name__ == "__main__":
    create_cockpit_canopy()
