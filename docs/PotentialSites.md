# Potential Sites To Add

Candidate retailers ordered by impact within each region. Only **new-copy** sellers
are listed — used-book stores are excluded so every entry directly supports
publishers and creators.

Each candidate notes the rendering stack (HtmlAgilityPack vs Playwright), what
makes it worth adding, and known friction.

***

## America

| Site | Renders | Notes |
|---|---|---|
| Walmart | Cloudflare-gated, JS | Stocks Viz / Yen / Kodansha at varying discounts. High volume but high friction — needs Playwright + CF clearance. |

The America bucket is already well-covered (9 sites). Walmart is the only
remaining new-only retailer with meaningful incremental inventory.

***

## Britain

| Site | Renders | Notes |
|---|---|---|
| Page 45 | HtmlAgilityPack (CGI Perl) | Nottingham indie, well-curated stock. Alive at `page45.com` but runs on a 1990s-style CGI Perl backend (`cgi-bin/ss000001.pl`). The obvious search URL returns a parameter error — requires reverse-engineering form params (`SEARCH=`, `JOIN=AND`, etc.) before a scraper can be written. Doable but **not a quick add**. |

UK currently has 4 working sites (ForbiddenPlanet, OKComics, SciFier,
TravellingMan). Page 45 would round out indie coverage but needs CGI
archaeology work.

### Probed and excluded (2026-06-21)

- ❌ **Magic Pockets** — `magicpockets.co.uk` connection refused (likely shuttered).

***

## Australia

| Site | Renders | Notes |
|---|---|---|
| Madman Entertainment Shop | TBD | Official Madman distributor (anime + manga). Needs probe. |

Currently 4 AU-specific sites: AllStarComics, KingsComics, MangaMate, plus
SciFier. AU coverage is now reasonable — Madman would round out distributor
diversity but isn't urgent.

### Probed and excluded (2026-06-21)

- ❌ **Minotaur Entertainment** (`minotaur.com.au`) — connection refused.

***

## Canada

| Site | Renders | Notes |
|---|---|---|
| Indigo / Chapters | Unknown | Marked ❌ "Not Working" in earlier README revision — worth a re-audit; site may have changed since. **High-impact if it works** (Indigo is the canonical CA mainstream bookseller). |

Only SciFier currently covers Canada. The CA region is the thinnest by far
after every other lead was probed dead. Indigo is the only remaining viable
target — recommend probing before any other CA work.

### Probed and excluded (2026-06-21)

- ❌ **Comics N More** (`comicsnmore.com`) — domain parked, redirects to a
  HugeDomains "for sale" page.
- ❌ **The Beguiling** (`beguiling.com` → `beguilingbooksandart.com`) — landing
  page "temporarily unavailable"; webstore at `beguilingbooks.com` returns 404.
  Effectively offline.
- ❌ **Strange Adventures** (`strangeadventures.com`) — alive but a WordPress
  blog-style site; no real product search or e-commerce; their store is
  hosted on eBay.

***

## Europe

| Site | Renders | Notes |
|---|---|---|
| Anime Limited Shop | TBD | UK/EU manga + anime, ships to mainland EU. |
| Hibernation Shop | TBD | EU manga distributor. |

Currently only SciFier covers Europe. Both candidates need region-detection
work to figure out shipping cost vs catalog price.

***

## Japan

| Site | Renders | Notes |
|---|---|---|
| Amazon Japan | Playwright | Listed as ⌛ Not Started in README. Hard: JP text encoding + region pricing. **High-impact for collectors of original-language volumes.** |
| CDJapan | TBD | Listed as ⌛ Paused — resume. |
| Honto | TBD | Mainstream JP manga retailer. |
| Kinokuniya JP | Playwright likely | Local Kinokuniya catalog (different from KinokuniyaUSA). |

The Japan region has zero working sites today. Any of these would be the first.

***

## Recommended Order

1. **Indigo / Chapters** (Canada) — re-audit broken integration. Canada has 1
   working site; Indigo is the only remaining viable lead.
2. **Page 45** (Britain) — needs CGI archaeology but adds real UK indie
   diversity. Defer until lower-effort options run out.
3. **Amazon Japan** — opens the JP region; longest tail and highest collector
   value.
4. **Walmart** (America) — only meaningful America addition left; CF friction.

***

## Recently Added

- **2026-06-21** — Kings Comics (Australia). Clean Shopify, listing-card
  carries title + price + availability. Same shape as AllStarComics.
- **2026-06-21** — OK Comics (Britain). Shopify with per-product detail fetch
  for price.
- **2026-06-21** — All Star Comics Melbourne (Australia). Shopify, Diamond
  catalog feed.

***

## Excluded (used-book only)

The following were considered but excluded because they primarily sell
second-hand inventory and would not directly support publishers / creators:

- Half Price Books (US)
- eBay marketplace listings (US)
