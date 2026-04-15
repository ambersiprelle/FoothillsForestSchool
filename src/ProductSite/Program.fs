module ChiAha.ProductSite.Program

open Microsoft.AspNetCore.Builder
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

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseRouting() |> ignore

    let endpoints =
        [
            get "/health" healthCheck
            post "/signup" (Signup.handle config.Resend.NotifyEmail)
        ] @ Admin.routes config.Admin

    app.UseFalco(endpoints) |> ignore

    app.Run()
    0
