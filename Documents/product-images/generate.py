from playwright.sync_api import sync_playwright
import os

TEMPLATE = open("template.html", encoding="utf-8").read()

PLANS = [
    {
        "slug": "tuning",
        "name_line1": "The",
        "name_line2": "Tuning",
        "badge": "Performance Upgrade",
        "track": "On-Site &middot; Durban",
        "tagline": "Good equipment that still doesn&#39;t sound the way it should. This is where it changes.",
        "d1": "Hands-on calibration that goes further than the basic AVR auto-setup",
        "d2": "Subwoofer positioning, crossover tuning, and cleaner bass integration",
        "d3": "A summary of what was adjusted and how it improved the listening experience",
        "pricing": "Hourly rate, minimum booking",
        "format": "Half-day to full-day session",
        "accent": "#c9a96e",
        "glow": "rgba(201,169,110,0.18)",
        "accent_border": "rgba(201,169,110,0.45)",
        "badge_bg": "rgba(201,169,110,0.07)",
        "track_color": "rgba(201,169,110,0.65)",
        "url_color": "rgba(201,169,110,0.5)",
    },
    {
        "slug": "director",
        "name_line1": "The",
        "name_line2": "Director",
        "badge": "End-to-End Oversight",
        "track": "On-Site &middot; Durban",
        "tagline": "Building a serious cinema room and wanting one person to keep the whole vision on track.",
        "d1": "Full room planning: acoustics, layout, equipment choices, and how the room should feel",
        "d2": "Contractor-ready guidance for cabling, power, lighting, control, and placement",
        "d3": "Oversight during the build so the final room still matches the original vision",
        "pricing": "Retainer or % of build cost",
        "format": "Concept to commissioning",
        "accent": "#b8a090",
        "glow": "rgba(184,160,144,0.16)",
        "accent_border": "rgba(184,160,144,0.4)",
        "badge_bg": "rgba(184,160,144,0.07)",
        "track_color": "rgba(184,160,144,0.65)",
        "url_color": "rgba(184,160,144,0.5)",
    },
    {
        "slug": "virtual-blueprint",
        "name_line1": "Virtual",
        "name_line2": "Blueprint",
        "badge": "Remote Planning",
        "track": "Remote &middot; South Africa",
        "tagline": "Expert room planning before you buy equipment or start building &mdash; delivered remotely.",
        "d1": "Guidance on seating, screen size, speaker positions, and surround sound foundation",
        "d2": "A wiring-priority guide so future upgrades are easier and cleaner",
        "d3": "A written plan you can follow yourself or hand to the people doing the work",
        "pricing": "Fixed remote consultation fee",
        "format": "WhatsApp intro + remote session",
        "accent": "#7eb8c9",
        "glow": "rgba(126,184,201,0.14)",
        "accent_border": "rgba(126,184,201,0.4)",
        "badge_bg": "rgba(126,184,201,0.07)",
        "track_color": "rgba(126,184,201,0.65)",
        "url_color": "rgba(126,184,201,0.5)",
    },
    {
        "slug": "procurement-report",
        "name_line1": "Procurement",
        "name_line2": "Report",
        "badge": "Remote Research",
        "track": "Remote &middot; South Africa",
        "tagline": "Independent buying advice so every rand goes on equipment that actually suits your room.",
        "d1": "A shopping list with sensible options across different budget levels",
        "d2": "Clear reasons for each recommendation &mdash; why it suits your room and goals",
        "d3": "South African pricing and stock guidance where available",
        "pricing": "Fixed report fee",
        "format": "WhatsApp intro + written report",
        "accent": "#7eb8c9",
        "glow": "rgba(126,184,201,0.14)",
        "accent_border": "rgba(126,184,201,0.4)",
        "badge_bg": "rgba(126,184,201,0.07)",
        "track_color": "rgba(126,184,201,0.65)",
        "url_color": "rgba(126,184,201,0.5)",
    },
]

base = "D:/Development/PureWave/Documents/product-images"

for plan in PLANS:
    html = TEMPLATE
    for k, v in plan.items():
        html = html.replace("{{" + k + "}}", v)
    path = os.path.join(base, f"{plan['slug']}.html")
    with open(path, "w", encoding="utf-8") as f:
        f.write(html)

with sync_playwright() as p:
    browser = p.chromium.launch()
    for plan in PLANS:
        page = browser.new_page(viewport={"width": 1080, "height": 1080})
        page.goto(f"http://localhost:7723/{plan['slug']}.html", wait_until="networkidle")
        out = os.path.join(base, f"{plan['slug']}.png")
        page.screenshot(path=out, clip={"x": 0, "y": 0, "width": 1080, "height": 1080})
        page.close()
        print(f"saved {plan['slug']}.png")
    browser.close()
