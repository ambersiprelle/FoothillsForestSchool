module OutlookScrape.Program

open System
open System.Collections.Generic
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open Microsoft.Identity.Client

// ===== Config =====

let clientId = "ca94cffe-e6b1-4794-8b59-88030d4bec9d"
let tenantId = "1a52cd6b-2281-44de-b39d-79642a7706c7"
let scopes = [| "Mail.Read"; "Contacts.Read"; "User.Read" |]
let importUrl = "https://foothills-forest-school.fly.dev/admin/import"
let adminUser = "katie"
let adminPass = "conley"
let batchSize = 500

/// Folders we never walk into (system noise + spam).
let folderBlacklist =
    set [
        "deleted items"; "drafts"; "junk email"; "junk e-mail"
        "outbox"; "conversation history"; "sync issues"
        "rss feeds"; "rss subscriptions"
        "recoverable items"
    ]

// ===== Auth (device code flow) =====

let acquireToken () =
    task {
        let app =
            PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithDefaultRedirectUri()
                .Build()

        let deviceCodeCallback (dcr: DeviceCodeResult) =
            printfn ""
            printfn "=========================================="
            printfn "%s" dcr.Message
            printfn "=========================================="
            printfn ""
            System.Threading.Tasks.Task.CompletedTask

        let! result = app.AcquireTokenWithDeviceCode(scopes, deviceCodeCallback).ExecuteAsync()
        return result.AccessToken
    }

// ===== Graph types =====

let http = new HttpClient()
do http.Timeout <- TimeSpan.FromMinutes 5.0

let jsonOpts =
    let o = JsonSerializerOptions()
    o.PropertyNameCaseInsensitive <- true
    o

let inline private isNil x = obj.ReferenceEquals(x, null)

type GraphEmailAddress = {
    [<JsonPropertyName("name")>] Name: string
    [<JsonPropertyName("address")>] Address: string
}

type GraphRecipient = {
    [<JsonPropertyName("emailAddress")>] EmailAddress: GraphEmailAddress
}

type GraphMessage = {
    [<JsonPropertyName("from")>] From: GraphRecipient
    [<JsonPropertyName("sender")>] Sender: GraphRecipient
    [<JsonPropertyName("toRecipients")>] ToRecipients: GraphRecipient[]
    [<JsonPropertyName("ccRecipients")>] CcRecipients: GraphRecipient[]
}

type GraphMessagePage = {
    [<JsonPropertyName("value")>] Value: GraphMessage[]
    [<JsonPropertyName("@odata.nextLink")>] NextLink: string
}

type GraphFolder = {
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("displayName")>] DisplayName: string
    [<JsonPropertyName("childFolderCount")>] ChildFolderCount: int
    [<JsonPropertyName("totalItemCount")>] TotalItemCount: int
}

type GraphFolderPage = {
    [<JsonPropertyName("value")>] Value: GraphFolder[]
    [<JsonPropertyName("@odata.nextLink")>] NextLink: string
}

// ===== Graph HTTP =====

let private fetchJson<'T> (token: string) (url: string) =
    task {
        let mutable attempt = 0
        let mutable result: 'T option = None
        let mutable keepTrying = true
        while keepTrying do
            attempt <- attempt + 1
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            let! resp = http.SendAsync(req)
            if int resp.StatusCode = 429 || int resp.StatusCode = 503 then
                let wait =
                    if resp.Headers.RetryAfter <> null && resp.Headers.RetryAfter.Delta.HasValue
                    then resp.Headers.RetryAfter.Delta.Value
                    else TimeSpan.FromSeconds(float (min 60 (attempt * 5)))
                printfn "  [throttle] %d, sleeping %ds" (int resp.StatusCode) (int wait.TotalSeconds)
                do! System.Threading.Tasks.Task.Delay(wait)
            elif not resp.IsSuccessStatusCode then
                let! body = resp.Content.ReadAsStringAsync()
                printfn "  [Graph Error] %d %s" (int resp.StatusCode) body
                keepTrying <- false
            else
                let! body = resp.Content.ReadAsStringAsync()
                result <- Some (JsonSerializer.Deserialize<'T>(body, jsonOpts))
                keepTrying <- false
            if attempt > 6 then keepTrying <- false
        return result
    }

// ===== Tag slug =====

let slugify (s: string) =
    let lower = s.ToLowerInvariant()
    let sb = StringBuilder()
    for c in lower do
        if Char.IsLetterOrDigit(c) then sb.Append(c) |> ignore
        elif c = ' ' || c = '-' || c = '_' || c = '/' then
            if sb.Length > 0 && sb.[sb.Length - 1] <> '-' then sb.Append('-') |> ignore
    sb.ToString().Trim('-')

// ===== Folder walk =====

type FolderTarget = { Id: string; Tag: string; Name: string; Count: int }

let rec walkFolders (token: string) (parentPathSlug: string) (listUrl: string) : System.Threading.Tasks.Task<FolderTarget list> =
    task {
        let acc = ResizeArray<FolderTarget>()
        let mutable url = listUrl
        let mutable keepGoing = true
        while keepGoing do
            let! pageOpt = fetchJson<GraphFolderPage> token url
            match pageOpt with
            | None -> keepGoing <- false
            | Some p ->
                if not (isNil p.Value) then
                    for f in p.Value do
                        let name = if isNil f.DisplayName then "" else f.DisplayName
                        let lname = name.ToLowerInvariant()
                        if folderBlacklist.Contains lname then
                            () // skip entirely — don't descend
                        else
                            let tagBase =
                                if parentPathSlug = "" then slugify name
                                else sprintf "%s-%s" parentPathSlug (slugify name)
                            let tag = if tagBase = "" then "folder" else tagBase
                            acc.Add({ Id = f.Id; Tag = tag; Name = name; Count = f.TotalItemCount })
                            if f.ChildFolderCount > 0 then
                                let childUrl =
                                    sprintf "https://graph.microsoft.com/v1.0/me/mailFolders/%s/childFolders?$top=100" f.Id
                                let! children = walkFolders token tag childUrl
                                acc.AddRange(children)
                if isNil p.NextLink then keepGoing <- false
                else url <- p.NextLink
        return List.ofSeq acc
    }

// ===== Message scrape =====

let selectFields = "from,sender,toRecipients,ccRecipients"

/// Append contacts from a folder's messages, tagging them with the folder's tag.
let scrapeFolder
    (token: string)
    (folder: FolderTarget)
    (contacts: Dictionary<string, string * HashSet<string>>) =
    task {
        let mutable url =
            sprintf "https://graph.microsoft.com/v1.0/me/mailFolders/%s/messages?$top=100&$select=%s"
                folder.Id selectFields
        let mutable page = 0
        let mutable total = 0
        let mutable keepGoing = true
        while keepGoing do
            let! pageOpt = fetchJson<GraphMessagePage> token url
            match pageOpt with
            | None -> keepGoing <- false
            | Some p ->
                page <- page + 1
                if not (isNil p.Value) then
                    total <- total + p.Value.Length
                    for m in p.Value do
                        let addRecipient (r: GraphRecipient) =
                            if not (isNil r) && not (isNil r.EmailAddress) && not (isNil r.EmailAddress.Address) then
                                let email = r.EmailAddress.Address.Trim().ToLowerInvariant()
                                if email.Contains('@') then
                                    let name = if isNil r.EmailAddress.Name then "" else r.EmailAddress.Name
                                    match contacts.TryGetValue email with
                                    | true, (existingName, tagSet) ->
                                        let nameFinal = if existingName = "" then name else existingName
                                        tagSet.Add folder.Tag |> ignore
                                        contacts.[email] <- (nameFinal, tagSet)
                                    | _ ->
                                        let ts = HashSet<string>()
                                        ts.Add folder.Tag |> ignore
                                        contacts.[email] <- (name, ts)
                        addRecipient m.From
                        addRecipient m.Sender
                        if not (isNil m.ToRecipients) then for r in m.ToRecipients do addRecipient r
                        if not (isNil m.CcRecipients) then for r in m.CcRecipients do addRecipient r
                if isNil p.NextLink then keepGoing <- false
                else url <- p.NextLink
        printfn "  [%s] %d msgs → %d total unique contacts" folder.Tag total contacts.Count
    }

// ===== Filter =====

let private noreplyPatterns = [|
    "noreply"; "no-reply"; "donotreply"; "do-not-reply"; "mailer-daemon"
    "postmaster"; "notifications@"; "notification@"; "alerts@"; "bounce"
|]

let isLikelyPerson (email: string) =
    let e = email.ToLowerInvariant()
    not (noreplyPatterns |> Array.exists (fun p -> e.Contains p))

let isValidEmail (email: string) =
    Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")

// ===== Import to FFS =====

type ImportRow = {
    email: string
    name: string
    source: string
    tags: string[]
}

let postBatch (rows: ImportRow[]) =
    task {
        use req = new HttpRequestMessage(HttpMethod.Post, importUrl)
        let basic =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" adminUser adminPass))
        req.Headers.Authorization <- AuthenticationHeaderValue("Basic", basic)
        let json = JsonSerializer.Serialize(rows)
        req.Content <- new StringContent(json, Encoding.UTF8, "application/json")
        let! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()
        if resp.IsSuccessStatusCode then
            printfn "  [import] batch=%d result=%s" rows.Length body
        else
            printfn "  [import ERROR] %d %s" (int resp.StatusCode) body
    }

// ===== Main =====

[<EntryPoint>]
let main _ =
    task {
        printfn "Foothills Forest School — Outlook Scraper (recursive)"
        printfn "======================================================"
        printfn "Walks ALL mail folders (except system/trash), extracts"
        printfn "every email address, tags each contact with the folder"
        printfn "name(s) where they appeared, and uploads to %s" importUrl
        printfn ""

        let! token = acquireToken ()
        printfn "[auth] Access token acquired."
        printfn ""

        printfn "Discovering mail folders..."
        let! folders =
            walkFolders token "" "https://graph.microsoft.com/v1.0/me/mailFolders?$top=100"
        printfn "Found %d folders to scan:" folders.Length
        for f in folders do
            printfn "  - %s (tag=%s, items=%d)" f.Name f.Tag f.Count
        printfn ""

        let contacts = Dictionary<string, string * HashSet<string>>(StringComparer.OrdinalIgnoreCase)

        // Sort folders smallest-first so progress feels responsive
        let ordered = folders |> List.sortBy (fun f -> f.Count)

        for f in ordered do
            if f.Count > 0 then
                printfn "Scanning '%s' (%d items)..." f.Name f.Count
                do! scrapeFolder token f contacts

        printfn ""
        printfn "Total unique contacts extracted: %d" contacts.Count

        let filtered =
            contacts
            |> Seq.filter (fun kv -> isValidEmail kv.Key && isLikelyPerson kv.Key)
            |> Seq.map (fun kv ->
                let (name, tags) = kv.Value
                let tagArr = Seq.toArray tags
                let allTags = Array.append [| "outlook-scrape" |] tagArr
                { email = kv.Key; name = name; source = "outlook-scrape"; tags = allTags })
            |> Seq.toArray

        printfn "After filtering (valid + not noreply): %d" filtered.Length
        printfn ""
        printfn "Uploading in batches of %d..." batchSize

        let mutable i = 0
        while i < filtered.Length do
            let take = min batchSize (filtered.Length - i)
            let batch = filtered.[i .. i + take - 1]
            do! postBatch batch
            i <- i + take

        printfn ""
        printfn "Done. %d contacts imported." filtered.Length
        return 0
    }
    |> fun t -> t.GetAwaiter().GetResult()
