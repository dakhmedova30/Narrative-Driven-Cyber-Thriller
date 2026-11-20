#!/usr/bin/env python3
"""Regenerate CrowGPT's Unity UI sprites from the web app's CSS values."""

from pathlib import Path
from PIL import Image, ImageDraw
import math
import shutil

ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "Sprites"
WEB_CROW = ROOT.parents[1] / "public" / "crow.png"
S = 4

INK = "#202521"
CREAM = "#FFFDF7"
GREEN = "#E7EDDF"
ORANGE = "#D65A38"
DISABLED = "#D8D6CC"
DISABLED_INK = "#8F918A"
DARK = "#202521"
YELLOW = "#E5A636"


def canvas(size):
    return Image.new("RGBA", (size[0] * S, size[1] * S), (0, 0, 0, 0))


def save_downsampled(image, name):
    image.resize(
        (image.width // S, image.height // S), Image.Resampling.LANCZOS
    ).save(SPRITES / name, optimize=True)


def rounded_panel(name, size, radius, fill, outline=INK, width=2):
    image = canvas(size)
    draw = ImageDraw.Draw(image)
    inset = width * S / 2 + S
    draw.rounded_rectangle(
        (inset, inset, image.width - inset, image.height - inset),
        radius=radius * S,
        fill=fill,
        outline=outline,
        width=width * S,
    )
    save_downsampled(image, name)


def speech_panel():
    # CSS corners: 30px 30px 30px 8px. Draw at 2x the on-screen size.
    image = canvas((512, 280))
    draw = ImageDraw.Draw(image)
    x0, y0, x1, y1 = 5 * S, 5 * S, 507 * S, 275 * S
    r, small = 60 * S, 16 * S
    path = [
        (x0 + r, y0),
        (x1 - r, y0),
        (x1, y0 + r),
        (x1, y1 - r),
        (x1 - r, y1),
        (x0 + small, y1),
        (x0, y1 - small),
        (x0, y0 + r),
    ]
    # Pillow's rounded_rectangle cannot express one smaller corner. Build a
    # polygon and cover each corner with the correct antialiased arc.
    draw.polygon(path, fill=CREAM)
    draw.rounded_rectangle((x0, y0, x1, y1), radius=r, fill=CREAM)
    draw.rectangle((x0, y1 - r, x0 + r, y1), fill=CREAM)
    draw.rounded_rectangle(
        (x0, y1 - 2 * small, x0 + 2 * small, y1),
        radius=small,
        fill=CREAM,
    )
    # Hand-drawn-compatible outline, with the asymmetric lower-left corner.
    draw.line((x0 + r, y0, x1 - r, y0), fill=INK, width=4 * S)
    draw.arc((x1 - 2 * r, y0, x1, y0 + 2 * r), 270, 360, fill=INK, width=4 * S)
    draw.line((x1, y0 + r, x1, y1 - r), fill=INK, width=4 * S)
    draw.arc((x1 - 2 * r, y1 - 2 * r, x1, y1), 0, 90, fill=INK, width=4 * S)
    draw.line((x1 - r, y1, x0 + small, y1), fill=INK, width=4 * S)
    draw.arc(
        (x0, y1 - 2 * small, x0 + 2 * small, y1),
        90,
        180,
        fill=INK,
        width=4 * S,
    )
    draw.line((x0, y1 - small, x0, y0 + r), fill=INK, width=4 * S)
    draw.arc((x0, y0, x0 + 2 * r, y0 + 2 * r), 180, 270, fill=INK, width=4 * S)
    save_downsampled(image, "panel_speech.png")


def speech_tail():
    image = canvas((96, 60))
    draw = ImageDraw.Draw(image)
    points = [(7 * S, 6 * S), (89 * S, 6 * S), (48 * S, 55 * S)]
    draw.polygon(points, fill=INK)
    inner = [(17 * S, 10 * S), (79 * S, 10 * S), (48 * S, 46 * S)]
    draw.polygon(inner, fill=CREAM)
    save_downsampled(image, "speech_tail.png")


def circle(name, size, fill, outline=None, width=2):
    image = canvas((size, size))
    draw = ImageDraw.Draw(image)
    pad = 3 * S
    draw.ellipse(
        (pad, pad, image.width - pad, image.height - pad),
        fill=fill,
        outline=outline,
        width=width * S if outline else 1,
    )
    save_downsampled(image, name)


def line_icon(name, segments, color=INK, width=2):
    image = canvas((64, 64))
    draw = ImageDraw.Draw(image)
    for points in segments:
        draw.line(
            [(x * S, y * S) for x, y in points],
            fill=color,
            width=width * S,
            joint="curve",
        )
        for x, y in (points[0], points[-1]):
            draw.ellipse(
                (
                    x * S - width * S / 2,
                    y * S - width * S / 2,
                    x * S + width * S / 2,
                    y * S + width * S / 2,
                ),
                fill=color,
            )
    save_downsampled(image, name)


def background():
    width, height = 390, 844
    image = Image.new("RGBA", (width * 2, height * 2), "#F7F3E9")
    pixels = image.load()
    center_x, center_y = width * 2 * 0.90, height * 2 * 0.08
    radius = width * 2 * 0.42
    accent = (255, 205, 83)
    base = (247, 243, 233)
    for y in range(image.height):
        for x in range(image.width):
            distance = math.hypot(x - center_x, y - center_y)
            alpha = max(0.0, 1.0 - distance / radius) * 0.12
            pixels[x, y] = tuple(
                round(base[channel] * (1 - alpha) + accent[channel] * alpha)
                for channel in range(3)
            ) + (255,)
    image.resize((width, height), Image.Resampling.LANCZOS).save(
        SPRITES / "background_390x844.png", optimize=True
    )


def phone_frame():
    """Transparent iPhone-style frame for a 390×844 screen opening."""
    width, height = 470, 944
    image = canvas((width, height))
    draw = ImageDraw.Draw(image)

    # Side controls sit behind the body.
    draw.rounded_rectangle(
        (12 * S, 205 * S, 22 * S, 258 * S),
        radius=4 * S,
        fill="#B6ACBF",
        outline="#5D5861",
        width=2 * S,
    )
    draw.rounded_rectangle(
        (12 * S, 276 * S, 22 * S, 352 * S),
        radius=4 * S,
        fill="#B6ACBF",
        outline="#5D5861",
        width=2 * S,
    )
    draw.rounded_rectangle(
        (448 * S, 300 * S, 458 * S, 410 * S),
        radius=4 * S,
        fill="#B6ACBF",
        outline="#5D5861",
        width=2 * S,
    )

    outer = (18 * S, 5 * S, 452 * S, 939 * S)
    draw.rounded_rectangle(
        outer,
        radius=61 * S,
        fill="#171819",
        outline="#BBB1C6",
        width=7 * S,
    )
    draw.rounded_rectangle(
        (24 * S, 11 * S, 446 * S, 933 * S),
        radius=56 * S,
        outline="#756B80",
        width=3 * S,
    )
    draw.rounded_rectangle(
        (30 * S, 18 * S, 440 * S, 926 * S),
        radius=51 * S,
        fill="#050606",
        outline="#343336",
        width=2 * S,
    )

    # Punch out the exact app viewport, leaving a slim black inner bezel.
    opening = Image.new("L", image.size, 0)
    opening_draw = ImageDraw.Draw(opening)
    opening_draw.rounded_rectangle(
        (40 * S, 50 * S, 430 * S, 894 * S),
        radius=42 * S,
        fill=255,
    )
    image.putalpha(
        Image.composite(
            Image.new("L", image.size, 0),
            image.getchannel("A"),
            opening,
        )
    )

    # Dynamic Island and camera lens are part of the device overlay.
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        (171 * S, 31 * S, 299 * S, 69 * S),
        radius=20 * S,
        fill="#020202",
    )
    draw.ellipse(
        (272 * S, 42 * S, 289 * S, 59 * S),
        fill="#080A10",
    )
    draw.ellipse(
        (277 * S, 47 * S, 284 * S, 54 * S),
        fill="#111A52",
    )
    save_downsampled(image, "phone_frame_470x944.png")


def main():
    SPRITES.mkdir(parents=True, exist_ok=True)
    crow_target = SPRITES / "crow.png"
    # In the Make repository, refresh from public/crow.png. Once copied into a
    # Unity project, the bundled sprite remains the self-contained source.
    if WEB_CROW.exists() and WEB_CROW.resolve() != crow_target.resolve():
        shutil.copy2(WEB_CROW, crow_target)
    elif not crow_target.exists():
        raise FileNotFoundError("Sprites/crow.png is required")
    background()
    phone_frame()
    speech_panel()
    speech_tail()
    rounded_panel("panel_input.png", (512, 112), 52, CREAM, width=3)
    rounded_panel("panel_starters.png", (512, 320), 42, GREEN, width=3)
    rounded_panel("panel_drawer_button.png", (512, 96), 28, DARK, outline=DARK, width=2)
    circle("button_outline.png", 96, (0, 0, 0, 0), INK, 3)
    circle("button_send.png", 96, ORANGE)
    circle("button_send_disabled.png", 96, DISABLED)
    circle("badge_question.png", 64, (0, 0, 0, 0), INK, 3)
    circle("badge_crow.png", 64, YELLOW, INK, 3)

    line_icon(
        "icon_menu.png",
        [[(20, 23), (44, 23)], [(20, 32), (44, 32)], [(20, 41), (38, 41)]],
        width=3,
    )
    line_icon(
        "icon_close.png",
        [[(21, 21), (43, 43)], [(43, 21), (21, 43)]],
        width=3,
    )
    line_icon(
        "icon_plus.png",
        [[(32, 17), (32, 47)], [(17, 32), (47, 32)]],
        width=3,
    )
    line_icon(
        "icon_arrow.png",
        [[(17, 32), (46, 32)], [(35, 21), (46, 32), (35, 43)]],
        width=3,
    )
    line_icon(
        "icon_arrow_disabled.png",
        [[(17, 32), (46, 32)], [(35, 21), (46, 32), (35, 43)]],
        color=DISABLED_INK,
        width=3,
    )
    print(f"Generated {len(list(SPRITES.glob('*.png')))} sprites in {SPRITES}")


if __name__ == "__main__":
    main()
