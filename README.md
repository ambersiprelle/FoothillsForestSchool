# Foothills Forest School — Website

Marketing site for Foothills Forest School, a nature-based preschool in Maryville, TN. Recreated from [FoothillsForestSchool.com](https://foothillsforestschool.com) and hosted on Fly.io.

**Live site:** https://foothills-forest-school.fly.dev/

---

## Tech Stack
- F# Falco 5.1 / .NET 8 serving static files from `wwwroot/` plus a lightweight CRM backend
- SQLite contacts DB on Fly volume (`/data/ffs.db`) via `Microsoft.Data.Sqlite`
- Resend for transactional + broadcast email (domain `foothillsforestschool.com` verified)
- Hosted on Fly.io (personal org, iad region)
- Fonts: Google Fonts (Raleway + Open Sans)

## Structure
```
wwwroot/          public assets served by Falco
  index.html      homepage
  *.html          content pages (philosophy, classes, summer camp, enrollment, etc.)
  css/            stylesheet
  js/             nav toggle + active-page highlight
  images/         photos
  robots.txt      allows all, points at sitemap
  sitemap.xml     URLs, lastmod 2026-04-12
Dockerfile        multi-stage .NET 8 build
src/ProductSite/  F# Falco app
  Configuration.fs  env vars
  Db.fs             SQLite contacts store
  Resend.fs         email client
  Signup.fs         POST /signup handler
  Admin.fs          basic-auth dashboard + broadcast
  Handlers.fs       static page routes
  Program.fs        Falco host
fly.toml          Fly.io app config
```

## Admin dashboard
`/admin` (basic auth via `ADMIN_USERNAME`/`ADMIN_PASSWORD` Fly secrets) shows all captured contacts with text search, multi-select tag filter (Cmd/Ctrl-click for OR), and source filter. Includes a broadcast-email form that sends via Resend to all contacts or a filtered tag.

## Deploy
```
fly deploy
```

No CI — manual deploys only.

## Pages (10 total)
| File | Content |
|------|---------|
| `index.html` | Homepage — hero, philosophy intro, programs overview, email signup |
| `nature-based-education.html` | 3-pillar educational philosophy |
| `classes.html` | Forest Preschool, Forest K/1, Homeschool Enrichment with pricing |
| `summer-camp.html` | 7 weekly themes, age groups, pricing |
| `enrollment.html` | 6-step process, tuition table, FAQ |
| `preview-days.html` | Spring 2026 preview dates |
| `family-fridays.html` | Spring 2026 sessions, pricing |
| `ways-to-support.html` | T-shirts, Venmo, Amazon Wishlist, building fund |
| `meet-our-staff.html` | Staff profiles and credentials |
| `hiring.html` | Open roles and how to apply |

## SEO
Full sweep complete:
- `robots.txt` + `sitemap.xml` (10 URLs)
- Open Graph + Twitter Card tags on every page
- Canonical URLs on every page
- Organization JSON-LD on homepage

## Suggested Next Steps

### High Priority
1. **Custom domain** — point `foothillsforestschool.com` at Fly.io (`fly certs create foothillsforestschool.com`). Then rewrite sitemap/canonical/og:url to the real domain.
2. **Real staff photos** — Meet Our Staff page uses placeholder icons.
3. **Photo gallery** — original site had one; a simple CSS grid would do.
4. **Parent Handbook PDF** — link from Enrollment page.

### Medium Priority
5. **Google Analytics** (GA4 snippet)
6. **Favicon**
7. **Wire hiring.html to `/signup`** — currently client-side only; removed from nav but page still exists.

### Nice to Have
8. **Enrollment status banner** — dismissable, easy to update
9. **Testimonials section**
10. **Interactive camp calendar**
