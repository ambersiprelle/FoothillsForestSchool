module ChiAha.ProductSite.Handlers

open Falco
open Microsoft.AspNetCore.Http
open ChiAha.ProductSite.TurnstileVerifier

let healthCheck : HttpHandler =
    fun ctx -> Response.ofPlainText "OK" ctx

let handleTurnstileConfig : HttpHandler =
    fun ctx ->
        task {
            ctx.Response.ContentType <- "application/json"
            let payload = sprintf "{\"siteKey\": \"%s\"}" (TurnstileVerifier.siteKey ())
            do! ctx.Response.WriteAsync(payload)
        }
