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
| **OK Comics** | HtmlAgilityPack | Leeds-based, ships UK. Pure HTML, paginated. Easy addition. |
| Magic Pockets | HtmlAgilityPack | UK manga retailer, paginated. |
| Page 45 | HtmlAgilityPack | Nottingham indie, well-curated stock. |

Currently 3 UK sites. These three roughly double the price-comparison signal for
UK buyers.

***

## Australia

| Site | Renders | Notes |
|---|---|---|
| **Kinokuniya Sydney** | Playwright likely | Separate from KinokuniyaUSA — different inventory + AUD pricing. Structurally similar to existing KinokuniyaUSA, so partial code reuse. **Highest AU impact.** |
| All Star Comics Melbourne | HtmlAgilityPack | Small but well-stocked. |
| Madman Entertainment Shop | TBD | Official Madman distributor (anime + manga). |

Currently 1 AU-specific site (MangaMate, plus SciFier). The AU region is the
thinnest by far — every addition has high marginal value.

***

## Canada

| Site | Renders | Notes |
|---|---|---|
| **Indigo / Chapters** | Unknown | README marks as ❌ "Not Working" — worth a re-audit; site may have changed. **High-impact if it works.** |
| Comics N More | HtmlAgilityPack | Canadian indie shop. |

Only SciFier currently covers Canada. Indigo would be the canonical CA option.

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
| Kinokuniya JP | Playwright likely | Local Kinokuniya catalog (different from KinokuniyaUSA / Sydney). |

The Japan region has zero working sites today. Any of these would be the first.

***

## Recommended Order

1. **OK Comics** (Britain) — easy HtmlAgilityPack add, immediate UK value.
2. **Kinokuniya Sydney** (Australia) — biggest AU impact, code-reuse from KinokuniyaUSA.
3. **Indigo / Chapters** (Canada) — re-audit broken integration, only canonical CA site.
4. **Half-the-rest-of-Britain** (Magic Pockets, Page 45) — round out UK coverage.
5. **Amazon Japan** — opens the JP region; longest tail.

***

## Excluded (used-book only)

The following were considered but excluded because they primarily sell
second-hand inventory and would not directly support publishers / creators:

- Half Price Books (US)
- eBay marketplace listings (US)
