# Foothills Forest School — Development Instructions

## Overview
Nature-based preschool and forest school in Maryville, TN. F# Falco site with integrated lightweight CRM (SQLite + Resend).

**Live:** https://foothills-forest-school.fly.dev/
**Repo:** ambersiprelle/FoothillsForestSchool

## Stack
- F# / .NET 8 / Falco 5.1
- Microsoft.Data.Sqlite (contacts DB on Fly volume at `/data/ffs.db`)
- Resend (transactional + broadcast email, domain `foothillsforestschool.com` verified)
- Fly.io app `foothills-forest-school`, org personal, region iad
- Fly volume `ffs_data` mounted at `/data`

## Compilation Order
1. Configuration.fs
2. Db.fs
3. Resend.fs
4. Signup.fs
5. Admin.fs
6. Handlers.fs
7. Program.fs

## Endpoints
| Route | Auth | Purpose |
|-------|------|---------|
| `GET /` (and other *.html) | public | Static marketing pages |
| `POST /signup` | public | Email capture → SQLite + notify katie@ via Resend |
| `GET /admin` | basic auth | Contacts dashboard + broadcast form. Filters: text search, multi-select tag (Cmd/Ctrl-click for OR), source. |
| `GET /admin/contacts.json` | basic auth | Contacts as JSON |
| `POST /admin/broadcast` | basic auth | Send Resend broadcast to all / by tag |
| `GET /health` | public | Health check |

## Secrets (Fly)
```bash
fly secrets set \
  RESEND_API_KEY="..." \
  RESEND_FROM_EMAIL="Foothills Forest School <noreply@foothillsforestschool.com>" \
  RESEND_TO_EMAIL="katie@foothillsforestschool.com" \
  ADMIN_USERNAME="katie" \
  ADMIN_PASSWORD="..." \
  -a foothills-forest-school
```

## Volume (first-time setup)
```bash
fly volumes create ffs_data --region iad --size 1 -a foothills-forest-school
```

## Deploy
```bash
fly deploy -a foothills-forest-school
```

## Signup form pattern
Forms POST to `/signup` with `email` (required), optional `name`, `phone`, `message`, `source`. The `source` field becomes a tag. Homepage newsletter form posts `source=homepage-newsletter`.

## Notes
- Custom domain `foothillsforestschool.com` not yet wired to Fly
- Hiring form (`hiring.html`) still client-side only — not yet wired to `/signup`
