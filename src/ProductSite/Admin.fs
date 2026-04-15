module ChiAha.ProductSite.Admin

open System
open System.Text
open Microsoft.AspNetCore.Http
open Falco
open ChiAha.ProductSite.Configuration
open ChiAha.ProductSite.Db
open ChiAha.ProductSite.Resend

let private unauthorized: HttpHandler =
    fun ctx ->
        task {
            ctx.Response.StatusCode <- 401
            ctx.Response.Headers.["WWW-Authenticate"] <-
                Microsoft.Extensions.Primitives.StringValues("Basic realm=\"FFS Admin\"")
            do! ctx.Response.WriteAsync("Unauthorized")
        }

let private checkAuth (cfg: AdminConfig) (ctx: HttpContext) =
    if cfg.Password = "" then false
    else
        match ctx.Request.Headers.TryGetValue("Authorization") with
        | true, v when v.Count > 0 ->
            let raw = v.[0]
            if raw.StartsWith("Basic ") then
                try
                    let decoded = Convert.FromBase64String(raw.Substring(6)) |> Encoding.UTF8.GetString
                    let parts = decoded.Split(':', 2)
                    parts.Length = 2
                    && parts.[0] = cfg.Username
                    && parts.[1] = cfg.Password
                with _ -> false
            else false
        | _ -> false

let private withAuth (cfg: AdminConfig) (inner: HttpHandler) : HttpHandler =
    fun ctx ->
        if checkAuth cfg ctx then inner ctx
        else unauthorized ctx

let private htmlEscape (s: string) =
    s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")

let private renderDashboard () =
    let contacts = listContacts ()
    let total = contacts.Length
    let allTags =
        contacts
        |> List.collect (fun c ->
            if c.Tags = "" then [] else c.Tags.Split(',') |> Array.map (fun t -> t.Trim()) |> Array.toList)
        |> List.filter (fun t -> t <> "")
        |> List.distinct
        |> List.sort
    let allSources =
        contacts
        |> List.map (fun c -> c.Source)
        |> List.filter (fun s -> s <> "")
        |> List.distinct
        |> List.sort
    let tagOptions =
        allTags
        |> List.map (fun t -> sprintf "<option value=\"%s\">%s</option>" (htmlEscape t) (htmlEscape t))
        |> String.concat ""
    let sourceOptions =
        allSources
        |> List.map (fun s -> sprintf "<option value=\"%s\">%s</option>" (htmlEscape s) (htmlEscape s))
        |> String.concat ""
    let rows =
        contacts
        |> List.map (fun c ->
            let haystack =
                sprintf "%s %s %s %s %s"
                    c.Email c.Name c.Phone c.Notes c.Tags
                |> (fun s -> s.ToLowerInvariant())
            sprintf """<tr data-tags="%s" data-source="%s" data-haystack="%s">
              <td>%d</td>
              <td><a href="mailto:%s">%s</a></td>
              <td>%s</td>
              <td>%s</td>
              <td>%s</td>
              <td><code>%s</code></td>
              <td title="%s">%s</td>
              <td>%s</td>
            </tr>"""
                (htmlEscape ("," + c.Tags + ","))
                (htmlEscape c.Source)
                (htmlEscape haystack)
                c.Id
                (htmlEscape c.Email) (htmlEscape c.Email)
                (htmlEscape c.Name)
                (htmlEscape c.Phone)
                (htmlEscape c.Source)
                (htmlEscape c.Tags)
                (htmlEscape c.Notes)
                (htmlEscape (if c.Notes.Length > 60 then c.Notes.Substring(0, 60) + "…" else c.Notes))
                c.CreatedAt)
        |> String.concat "\n"
    sprintf """<!doctype html>
<html><head><meta charset="utf-8"><title>FFS Admin</title>
<style>
  body{font-family:system-ui,sans-serif;max-width:1400px;margin:2rem auto;padding:0 1rem;color:#283232;}
  h1{color:#3f6e42;}
  table{width:100%%;border-collapse:collapse;margin-top:1rem;font-size:0.85rem;}
  th,td{padding:0.5rem;border-bottom:1px solid #eee;text-align:left;vertical-align:top;}
  th{background:#f5f7f5;color:#3f6e42;position:sticky;top:0;}
  code{background:#eef;padding:2px 6px;border-radius:4px;font-size:0.8rem;}
  form{margin-top:2rem;padding:1rem;background:#f5f7f5;border-radius:8px;}
  textarea{width:100%%;min-height:120px;font-family:inherit;padding:0.5rem;}
  input,button,select{padding:0.5rem;font-family:inherit;}
  button{background:#3f6e42;color:white;border:0;border-radius:4px;cursor:pointer;padding:0.5rem 1rem;}
  .filters{display:flex;gap:0.75rem;align-items:center;flex-wrap:wrap;margin-top:1rem;padding:1rem;background:#f5f7f5;border-radius:8px;}
  .filters label{font-size:0.85rem;color:#3f6e42;font-weight:600;}
  .filters input[type=text]{min-width:240px;}
  #count{font-weight:700;color:#3f6e42;margin-left:auto;}
  tr.hidden{display:none;}
  td[title]{max-width:300px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
</style></head><body>
<h1>Foothills Forest School — Admin</h1>
<p><strong>%d contacts total</strong></p>

<div class="filters">
  <label>Search: <input type="text" id="q" placeholder="email, name, phone, notes, tag…" /></label>
  <label>Tag: <select id="tagFilter" multiple size="6">%s</select></label>
  <label>Source: <select id="sourceFilter"><option value="">— any —</option>%s</select></label>
  <button type="button" id="clearFilters">Clear</button>
  <span id="count"></span>
</div>

<table id="contactsTable">
  <thead><tr><th>ID</th><th>Email</th><th>Name</th><th>Phone</th><th>Source</th><th>Tags</th><th>Notes</th><th>Created</th></tr></thead>
  <tbody>
  %s
  </tbody>
</table>

<script>
(function(){
  var q=document.getElementById('q');
  var tagF=document.getElementById('tagFilter');
  var srcF=document.getElementById('sourceFilter');
  var clear=document.getElementById('clearFilters');
  var count=document.getElementById('count');
  var rows=document.querySelectorAll('#contactsTable tbody tr');
  function apply(){
    var needle=(q.value||'').toLowerCase().trim();
    var tags=Array.from(tagF.selectedOptions).map(function(o){return o.value;}).filter(Boolean);
    var src=srcF.value;
    var shown=0;
    rows.forEach(function(r){
      var h=r.getAttribute('data-haystack')||'';
      var ts=r.getAttribute('data-tags')||'';
      var s=r.getAttribute('data-source')||'';
      var ok=true;
      if(needle && h.indexOf(needle)===-1) ok=false;
      if(tags.length && !tags.some(function(t){return ts.indexOf(','+t+',')!==-1;})) ok=false;
      if(src && s!==src) ok=false;
      if(ok){r.classList.remove('hidden');shown++;} else {r.classList.add('hidden');}
    });
    count.textContent=shown+' shown';
  }
  q.addEventListener('input',apply);
  tagF.addEventListener('change',apply);
  srcF.addEventListener('change',apply);
  clear.addEventListener('click',function(){q.value='';Array.from(tagF.options).forEach(function(o){o.selected=false;});srcF.value='';apply();});
  apply();
})();
</script>

<form method="post" action="/admin/broadcast">
  <h2>Broadcast email</h2>
  <p>
    Filter by tag (blank = all): <input name="tag" placeholder="newsletter" />
  </p>
  <p>
    Subject: <input name="subject" style="width:80%%;" required />
  </p>
  <p>
    <label>HTML body:</label><br/>
    <textarea name="html" required></textarea>
  </p>
  <button type="submit">Send</button>
</form>
</body></html>""" total tagOptions sourceOptions rows

let private dashboard : HttpHandler =
    fun ctx ->
        task {
            ctx.Response.ContentType <- "text/html; charset=utf-8"
            do! ctx.Response.WriteAsync(renderDashboard ())
        }

let private contactsJson : HttpHandler =
    fun ctx ->
        task {
            let json = System.Text.Json.JsonSerializer.Serialize(listContacts ())
            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsync(json)
        }

let private broadcast : HttpHandler =
    fun ctx ->
        task {
            let form = ctx.Request.Form
            let get (k: string) =
                match form.TryGetValue(k) with true, v -> v.[0] | _ -> ""
            let tag = (get "tag").Trim()
            let subject = (get "subject").Trim()
            let html = get "html"
            if subject = "" || html = "" then
                ctx.Response.StatusCode <- 400
                do! ctx.Response.WriteAsync("subject and html required")
            else
                let targets = contactsByTag tag
                let mutable sent = 0
                for c in targets do
                    let! ok = send c.Email subject html
                    if ok then sent <- sent + 1
                ctx.Response.ContentType <- "text/html; charset=utf-8"
                do! ctx.Response.WriteAsync(
                    sprintf """<p>Sent %d / %d emails. <a href="/admin">Back</a></p>""" sent targets.Length)
        }

type ImportRow = {
    email: string
    name: string
    phone: string
    notes: string
    source: string
    tags: string[]
}

let private import : HttpHandler =
    fun ctx ->
        task {
            use reader = new System.IO.StreamReader(ctx.Request.Body)
            let! body = reader.ReadToEndAsync()
            try
                let rows = System.Text.Json.JsonSerializer.Deserialize<ImportRow[]>(body)
                let mutable n = 0
                for r in rows do
                    if r.email <> null && r.email.Contains('@') then
                        let src = if r.source = null || r.source = "" then "outlook-scrape" else r.source
                        let name = if r.name = null then "" else r.name
                        let phone = if r.phone = null then "" else r.phone
                        let notes = if r.notes = null then "" else r.notes
                        let tagList =
                            if r.tags = null || r.tags.Length = 0 then [| src |]
                            else r.tags
                        upsertContact r.email name phone src tagList.[0]
                        for i in 1 .. tagList.Length - 1 do
                            upsertContact r.email name phone src tagList.[i]
                        setPhoneIfEmpty r.email phone
                        setNotes r.email notes
                        n <- n + 1
                ctx.Response.ContentType <- "application/json"
                do! ctx.Response.WriteAsync(sprintf """{"imported":%d,"received":%d}""" n rows.Length)
            with ex ->
                ctx.Response.StatusCode <- 400
                do! ctx.Response.WriteAsync(sprintf "bad request: %s" ex.Message)
        }

let routes (cfg: AdminConfig) =
    [
        Falco.Routing.get "/admin" (withAuth cfg dashboard)
        Falco.Routing.get "/admin/contacts.json" (withAuth cfg contactsJson)
        Falco.Routing.post "/admin/broadcast" (withAuth cfg broadcast)
        Falco.Routing.post "/admin/import" (withAuth cfg import)
    ]
