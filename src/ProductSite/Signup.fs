module ChiAha.ProductSite.Signup

open System
open Microsoft.AspNetCore.Http
open Falco
open ChiAha.ProductSite.Configuration
open ChiAha.ProductSite.Db
open ChiAha.ProductSite.Resend

let private getField (form: IFormCollection) (key: string) =
    match form.TryGetValue(key) with
    | true, v -> v.[0]
    | _ -> ""

let private isValidEmail (email: string) =
    email.Contains('@') && email.Contains('.') && email.Length > 4

/// POST /signup — email capture (and optional name/phone/message/source)
let handle (notifyEmail: string) : HttpHandler =
    fun ctx ->
        task {
            let form = ctx.Request.Form
            let email = (getField form "email").Trim()
            let name = (getField form "name").Trim()
            let phone = (getField form "phone").Trim()
            let message = (getField form "message").Trim()
            let sourceIn = (getField form "source").Trim()
            let source = if sourceIn = "" then "website" else sourceIn
            let tag = if sourceIn = "" then "newsletter" else sourceIn

            if not (isValidEmail email) then
                ctx.Response.StatusCode <- 400
                do! ctx.Response.WriteAsync("A valid email is required.")
            else
                try
                    upsertContact email name phone source tag
                    printfn "[Signup] %s (%s) source=%s" email name source

                    let notifyHtml =
                        sprintf """<div style="font-family:Arial,sans-serif;max-width:600px;">
                          <h2 style="color:#3f6e42;">New Signup — Foothills Forest School</h2>
                          <p><strong>Email:</strong> %s</p>
                          <p><strong>Name:</strong> %s</p>
                          <p><strong>Phone:</strong> %s</p>
                          <p><strong>Source:</strong> %s</p>
                          <p><strong>Message:</strong><br/>%s</p>
                          <p style="color:#999;font-size:12px;">Submitted: %s UTC</p>
                        </div>"""
                            email
                            (if name = "" then "—" else name)
                            (if phone = "" then "—" else phone)
                            source
                            (if message = "" then "—" else message)
                            (DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                    let subject = sprintf "New %s signup: %s" source email
                    let! _ = send notifyEmail subject notifyHtml
                    ()

                    ctx.Response.ContentType <- "text/html; charset=utf-8"
                    do! ctx.Response.WriteAsync(
                        """<p style="color:#3f6e42;font-weight:700;text-align:center;">Thanks! We'll be in touch.</p>""")
                with ex ->
                    printfn "[Signup Error] %s" ex.Message
                    ctx.Response.StatusCode <- 500
                    do! ctx.Response.WriteAsync("Something went wrong. Please try again.")
        }
