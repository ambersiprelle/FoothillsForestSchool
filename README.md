# Foothills Forest School — Static Website

Marketing site for Foothills Forest School, a nature-based preschool in Maryville, TN. Recreated from [FoothillsForestSchool.com](https://foothillsforestschool.com) and hosted on Fly.io.

**Live site:** https://foothills-forest-school.fly.dev/

---

## Tech Stack
- F# Falco 5.1 / .NET 8 serving static files from `wwwroot/`
- Hosted on Fly.io (personal org, iad region)
- Fonts: Google Fonts (Raleway + Open Sans)
- Falco chosen over pure nginx so the email signup form can be wired to a real backend without a platform migration

## Structure
```
wwwroot/          all public assets served by nginx
  index.html      homepage
  *.html          9 content pages (philosophy, classes, summer camp, enrollment, etc.)
  css/            stylesheet
  js/             nav toggle + active-page highlight
  images/         photos
  robots.txt      allows all, points at sitemap
  sitemap.xml     10 URLs, lastmod 2026-04-12
Dockerfile        multi-stage .NET 8 build
src/ProductSite/  F# Falco app (Program.fs, Configuration.fs, Handlers.fs)
fly.toml          Fly.io app config
```

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
6. **Contact form** via Formspree (currently email link only)
7. **Favicon**
8. **Email signup backend** — homepage form currently shows a thanks message but doesn't capture addresses. Wire to Mailchimp/ConvertKit.

### Nice to Have
9. **Enrollment status banner** — dismissable, easy to update
10. **Testimonials section**
11. **Interactive camp calendar**
