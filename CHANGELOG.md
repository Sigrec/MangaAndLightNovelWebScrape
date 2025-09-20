# Changelog

## Legend

- ✅ Added / Removed features
- 🔥 Bug fixes
- ⌛ Performance improvements
- 📜 Site-specific changes

---

## v5.0.1 – Sept 20, 2025

### 🚀 Major Changes

- ✅ Removed **Indigo** and **Waterstones** websites  
  - Waterstones removed due to new captcha and robots.txt rules
  - Indigo removed due to security blockers
- ✅ Added **MangaMart** website scraper for the `America` region
- ✅ Introduced `IWebsite` interface for all sites  
  - Improves code structure  
  - Simplifies adding new/custom sites
- ✅ Added URL constants (`const string`) for all website objects
- ✅ Replaced **Selenium** with **Playwright**
- ✅ Improved comments/documentation for all public-facing members
- ⌛ Updated several collections to use `Frozen` for performance

### 📜 Site-Specific Fixes & Improvements

#### RobertsAnimeCornerStore

- 🔥 Fixed box set numbering
- 🔥 Fixed issue where series containing ampersand (`&`) were not scraped

#### InStockTrades

- 🔥 Fixed box set numbering
- 🔥 Fixed issue where series with ampersand (`&`) were not scraped

#### Crunchyroll

- 🔥 Fixed stock status mapping (was always mapping to Backorder `BO`)
- 🔥 Fixed incorrect filtering of Blu-ray and Funko Pop entries
- 🔥 Fixed issue with titles containing HTML-encoded characters

#### MerryManga

- 🔥 Fixed page loading issue preventing scraping
- 🔥 Fixed duplication of Box Set entries
- ⌛ General performance improvements

#### Amazon USA

- 🔥 Fixed incorrect price parsing causing missing items
- 🔥 Fixed issue with leading text after volume numbers
- 🔥 Fixed multiple box set parsing issues

#### MangaMate

- 🔥 Fixed page loading issue for series without multiple pages
- 🔥 Fixed incorrect stock status mapping for some OOS entries

#### Forbidden Planet

- 🔥 Fixed parsing of HTML-encoded characters
- 🔥 Fixed incorrect parsing of titles with text after `:`
- 🔥 Fixed light novel parsing issues

#### Books-A-Million

- 🔥 Fixed issue where series with ampersands (`&`) in titles were not scraped

#### SciFier

- 🔥 Fixed missing `"Novel"` suffix for novel entries
- 🔥 Fixed inconsistent removal of author text

#### Kinokuniya USA

- 🔥 Fixed scraping hang for certain series

#### TravellingMan

- 🔥 Fixed incorrect matching of entries to book type
- 🔥 Fixed parsing issues with ASCII characters
- 🔥 Fixed parsing of entries with extra text after volume/novel numbers
