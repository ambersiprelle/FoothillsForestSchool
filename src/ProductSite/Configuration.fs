module ChiAha.ProductSite.Configuration

open System

type ResendConfig = {
    ApiKey: string
    FromEmail: string
    NotifyEmail: string
}

type AdminConfig = {
    Username: string
    Password: string
}

type ProductConfig = {
    ProductName: string
    DbPath: string
    Resend: ResendConfig
    Admin: AdminConfig
}

let private getEnv key fallback =
    match Environment.GetEnvironmentVariable(key) with
    | null | "" -> fallback
    | v -> v

let loadConfig () =
    {
        ProductName = getEnv "PRODUCT_NAME" "FoothillsForestSchool"
        DbPath = getEnv "DB_PATH" "/data/ffs.db"
        Resend = {
            ApiKey = getEnv "RESEND_API_KEY" ""
            FromEmail = getEnv "RESEND_FROM_EMAIL" "Foothills Forest School <noreply@foothillsforestschool.com>"
            NotifyEmail = getEnv "RESEND_TO_EMAIL" "katie@foothillsforestschool.com"
        }
        Admin = {
            Username = getEnv "ADMIN_USERNAME" "katie"
            Password = getEnv "ADMIN_PASSWORD" ""
        }
    }

let printConfigStatus (config: ProductConfig) =
    printfn "[%s] Configuration loaded" config.ProductName
    printfn "  DB: %s" config.DbPath
    printfn "  Resend: %s" (if config.Resend.ApiKey <> "" then "configured" else "DISABLED (no API key)")
    printfn "  Admin: %s" (if config.Admin.Password <> "" then sprintf "user=%s" config.Admin.Username else "DISABLED (no password)")
