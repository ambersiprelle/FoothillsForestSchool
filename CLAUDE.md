# Foothills Forest School — Development Instructions

## Overview
Nature-based preschool and forest school in Maryville, TN. Static marketing site recreated from foothillsforestschool.com.

**Live:** https://foothills-forest-school.fly.dev/
**Repo:** ambersiprelle/FoothillsForestSchool

## Key Details
- **Branch:** main
- **Stack:** Pure static HTML/CSS/JS, served by `nginx:alpine`
- **Deploy:** `fly deploy` (direct, no CI)
- **Fly org:** personal
- **Region:** iad
- **Pages:** 10 (index, nature-based-education, classes, summer-camp, enrollment, preview-days, family-fridays, ways-to-support, meet-our-staff, hiring)
- **Assets:** `wwwroot/` contains all HTML, css/, js/, images/, robots.txt, sitemap.xml

## SEO Status
Full sweep done: robots.txt, sitemap.xml (10 URLs), OG + Twitter tags, canonicals, Organization JSON-LD on homepage. Hero image used for og:image.

## Notes
- Custom domain `foothillsforestschool.com` not yet wired — sitemap/canonical/og:url all point to fly.dev for now
- Email signup form is client-side only (no backend wired)
