module ChiAha.ProductSite.Resend

open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open ChiAha.ProductSite.Configuration

let mutable private config: ResendConfig option = None

let init (cfg: ResendConfig) =
    if cfg.ApiKey <> "" then
        config <- Some cfg
        printfn "[Resend] Registered (from: %s)" cfg.FromEmail
    else
        printfn "[Resend] Skipped — no API key"

let private httpClient = new HttpClient()

let send (toEmail: string) (subject: string) (html: string) =
    task {
        match config with
        | None ->
            printfn "[Resend] (no-op) would send '%s' to %s" subject toEmail
            return false
        | Some cfg ->
            try
                use req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
                req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", cfg.ApiKey)
                let payload =
                    {| ``from`` = cfg.FromEmail
                       ``to`` = [| toEmail |]
                       subject = subject
                       html = html |}
                let json = JsonSerializer.Serialize(payload)
                req.Content <- new StringContent(json, Encoding.UTF8, "application/json")
                let! resp = httpClient.SendAsync(req)
                if resp.IsSuccessStatusCode then
                    printfn "[Resend] Sent '%s' to %s" subject toEmail
                    return true
                else
                    let! body = resp.Content.ReadAsStringAsync()
                    printfn "[Resend Error] %d: %s" (int resp.StatusCode) body
                    return false
            with ex ->
                printfn "[Resend Exception] %s" ex.Message
                return false
    }
