# Foothills Forest School — Static Website

A static HTML/CSS website recreated from [FoothillsForestSchool.com](https://foothillsforestschool.com), hosted on GitHub Pages.

**Live site:** https://ambersiprelle.github.io/FoothillsForestSchool/

---

## What's Been Built

### Pages (10 total)
| File | Content |
|------|---------|
| `index.html` | Homepage — hero, philosophy intro, programs overview, email signup |
| `nature-based-education.html` | Full 3-pillar educational philosophy with feature sections |
| `classes.html` | Forest Preschool, Forest K/1, and Homeschool Enrichment with pricing |
| `summer-camp.html` | All 7 weekly themes, age groups (Tadpoles → Black Bears), pricing |
| `enrollment.html` | 6-step process, tuition table, FAQ, links to Brightwheel |
| `preview-days.html` | Spring 2026 dates, what to expect, registration info |
| `family-fridays.html` | Spring 2026 session topics, pricing, what to bring |
| `ways-to-support.html` | T-shirt shop, Venmo donations, Amazon Wishlist, building fund |
| `meet-our-staff.html` | Staff profiles, credentials, team philosophy |
| `hiring.html` | Open roles, ideal candidate qualities, how to apply |

### Assets
| File | Description |
|------|-------------|
| `css/style.css` | Full stylesheet — green palette, Raleway/Open Sans fonts, responsive |
| `js/main.js` | Mobile nav toggle, active page highlighting |
| `images/hero.jpg` | Homepage hero — outdoor learning scene |
| `images/classes.jpg` | Used on classes and feature sections |
| `images/nature-1.jpg` | Nature/preschool imagery |
| `images/nature-2.jpg` | Additional nature imagery |
| `images/summer-camp.jpg` | Summer camp photography |

### Features
- Fully responsive — works on mobile, tablet, and desktop
- Sticky navigation with mobile hamburger menu
- Active page highlighting in nav
- Forest green color scheme matching original brand
- All content from original site: programs, pricing, schedules, staff, enrollment steps
- External links preserved: Brightwheel enrollment, t-shirt shop, Amazon Wishlist, Venmo

---

## Suggested Next Steps

### High Priority

**1. Custom Domain**
Point `FoothillsForestSchool.com` to GitHub Pages so the site lives at the real domain instead of the github.io URL.
- In your domain registrar (GoDaddy), add a CNAME record: `www` → `ambersiprelle.github.io`
- In GitHub repo Settings → Pages → add custom domain: `www.FoothillsForestSchool.com`
- Enable "Enforce HTTPS"

**2. Add Real Staff Photos**
The Meet Our Staff page currently uses placeholder icons. Replace with actual headshots for a more personal feel. Just drop photos into the `images/` folder and update the `<img>` tags in `meet-our-staff.html`.

**3. Add a Photo Gallery**
The original site had a photo gallery section. Adding one to the homepage (or a dedicated `/gallery.html` page) would bring the site to life. A simple CSS grid of images works well with no JavaScript library needed.

**4. Parent Handbook PDF**
The original site referenced a downloadable Parent Handbook. Add the PDF to the repo and link it from the Enrollment page.

---

### Medium Priority

**5. Google Analytics**
Add a GA4 tracking snippet to measure traffic, popular pages, and where visitors drop off before enrolling.

**6. Contact Form**
Replace the email link with a real contact form using a free service like [Formspree](https://formspree.io) — no backend needed, just a form that emails you on submission.

**7. SEO Improvements**
- Add Open Graph meta tags (for social sharing previews)
- Add a `sitemap.xml`
- Add a `robots.txt`
- Add structured data (Schema.org) for the school's address and contact info

**8. Favicon**
Add a small favicon (the school logo or a leaf icon) so the browser tab looks polished.

**9. Email Signup Integration**
The homepage has an email signup form that currently just shows a "thanks" message. Connect it to Mailchimp, ConvertKit, or a similar service to actually capture emails.

---

### Nice to Have

**10. Enrollment Status Banner**
A dismissable banner at the top of the site that can be quickly updated to say "Now enrolling for 2026–2027!" or "Summer Camp spots filling fast!" — useful for time-sensitive announcements.

**11. Testimonials Section**
Add a section on the homepage or enrollment page with quotes from current families. Social proof significantly increases enrollment inquiries.

**12. Interactive Camp Calendar**
A visual calendar showing all 7 summer camp weeks at a glance, with availability status per week.

---

## How to Make Updates

1. Edit files locally in `/Users/ambersiprelle/foothills-forest-school/`
2. Commit changes: `git add . && git commit -m "describe your change"`
3. Push to GitHub: `git push`
4. GitHub Pages automatically rebuilds — changes are live within ~1 minute

## Tech Stack
- Plain HTML5, CSS3, vanilla JavaScript — no frameworks or build tools needed
- Hosted on GitHub Pages (free)
- Fonts: Google Fonts (Raleway + Open Sans)
