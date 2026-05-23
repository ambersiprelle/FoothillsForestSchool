module ChiAha.ProductSite.TurnstileVerifier

open System
open System.Net.Http
open System.Text.Json

let private turnstileSiteKey =
    Environment.GetEnvironmentVariable("TURNSTILE_SITE_KEY")
    |> Option.ofObj |> Option.defaultValue ""

let private turnstileSecretKey =
    Environment.GetEnvironmentVariable("TURNSTILE_SECRET_KEY")
    |> Option.ofObj |> Option.defaultValue ""

let enabled = turnstileSecretKey <> ""
let siteKey () = turnstileSiteKey

let private client =
    let c = new HttpClient()
    c.Timeout <- TimeSpan.FromSeconds(5.0)
    c

let verify (token: string) (clientIp: string) : System.Threading.Tasks.Task<bool> =
    task {
        if not enabled then return true
        elif String.IsNullOrEmpty token then return false
        else
            try
                let pairs =
                    [ System.Collections.Generic.KeyValuePair<string, string>("secret", turnstileSecretKey)
                      System.Collections.Generic.KeyValuePair<string, string>("response", token)
                      System.Collections.Generic.KeyValuePair<string, string>("remoteip", clientIp) ]
                use content = new FormUrlEncodedContent(pairs)
                let! resp = client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content)
                let! body = resp.Content.ReadAsStringAsync()
                use doc = JsonDocument.Parse(body)
                match doc.RootElement.TryGetProperty("success") with
                | true, v when v.ValueKind = JsonValueKind.True -> return true
                | _ ->
                    printfn "[turnstile] verification rejected: %s" body
                    return false
            with ex ->
                printfn "[turnstile] siteverify call failed: %s" ex.Message
                return false
    }
