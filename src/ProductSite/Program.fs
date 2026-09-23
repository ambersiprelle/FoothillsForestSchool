module ChiAha.ProductSite.Program

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco
open Falco.Routing
open ChiAha.ProductSite.Configuration
open ChiAha.ProductSite.Handlers
open ChiAha.ProductSite.Db
open ChiAha.ProductSite.Resend
open ChiAha.ProductSite.Signup
open ChiAha.ProductSite.Admin

[<EntryPoint>]
let main args =
    let config = loadConfig ()
    printConfigStatus config

    Db.init config.DbPath
    Resend.init config.Resend

    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    // Extensionless URLs such as /classes and /enrollment were indexed by Google
    // while the domain sat on GoDaddy. Permanently redirect any such path to the
    // .html page of the same name so those links keep working.
    let webRoot = app.Environment.WebRootPath
    app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
        let raw = ctx.Request.Path.Value
        let path = if isNull raw then "" else raw.TrimEnd('/')
        let candidate =
            if not (isNull webRoot) && path.Length > 1 && not (path.Contains ".")
            then System.IO.Path.Combine(webRoot, path.TrimStart('/') + ".html")
            else ""
        if candidate <> "" && System.IO.File.Exists candidate then
            ctx.Response.Redirect(path + ".html" + ctx.Request.QueryString.Value, true)
            System.Threading.Tasks.Task.CompletedTask
        else
            next.Invoke ctx) |> ignore

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseRouting() |> ignore

    let endpoints =
        [
            get "/health" healthCheck
            get "/api/turnstile-config" handleTurnstileConfig
            post "/signup" (Signup.handle config.Resend.NotifyEmail)
        ] @ Admin.routes config.Admin

    app.UseFalco(endpoints) |> ignore

    app.Run()
    0
