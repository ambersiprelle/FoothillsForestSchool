module ChiAha.ProductSite.Db

open System
open System.IO
open Microsoft.Data.Sqlite

type Contact = {
    Id: int64
    Email: string
    Name: string
    Phone: string
    Source: string
    Tags: string
    Notes: string
    CreatedAt: string
}

let mutable private connString = ""

let init (dbPath: string) =
    let dir = Path.GetDirectoryName(dbPath)
    if dir <> "" && not (Directory.Exists dir) then
        Directory.CreateDirectory(dir) |> ignore
    connString <- sprintf "Data Source=%s" dbPath
    use conn = new SqliteConnection(connString)
    conn.Open()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS contacts (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            email TEXT NOT NULL UNIQUE COLLATE NOCASE,
            name TEXT NOT NULL DEFAULT '',
            phone TEXT NOT NULL DEFAULT '',
            source TEXT NOT NULL DEFAULT '',
            tags TEXT NOT NULL DEFAULT '',
            notes TEXT NOT NULL DEFAULT '',
            created_at TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_contacts_email ON contacts(email);
    """
    cmd.ExecuteNonQuery() |> ignore
    printfn "[Db] Initialized at %s" dbPath

let private openConn () =
    let c = new SqliteConnection(connString)
    c.Open()
    c

let private readContact (r: SqliteDataReader) =
    {
        Id = r.GetInt64(0)
        Email = r.GetString(1)
        Name = r.GetString(2)
        Phone = r.GetString(3)
        Source = r.GetString(4)
        Tags = r.GetString(5)
        Notes = r.GetString(6)
        CreatedAt = r.GetString(7)
    }

/// Insert-or-update a contact by email. Merges new tag into existing tags.
let upsertContact (email: string) (name: string) (phone: string) (source: string) (tag: string) =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        INSERT INTO contacts (email, name, phone, source, tags, created_at)
        VALUES ($email, $name, $phone, $source, $tag, $now)
        ON CONFLICT(email) DO UPDATE SET
            name = CASE WHEN excluded.name <> '' THEN excluded.name ELSE contacts.name END,
            phone = CASE WHEN excluded.phone <> '' THEN excluded.phone ELSE contacts.phone END,
            tags = CASE
                WHEN contacts.tags = '' THEN excluded.tags
                WHEN excluded.tags = '' THEN contacts.tags
                WHEN instr(',' || contacts.tags || ',', ',' || excluded.tags || ',') > 0 THEN contacts.tags
                ELSE contacts.tags || ',' || excluded.tags
            END
    """
    cmd.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant()) |> ignore
    cmd.Parameters.AddWithValue("$name", name) |> ignore
    cmd.Parameters.AddWithValue("$phone", phone) |> ignore
    cmd.Parameters.AddWithValue("$source", source) |> ignore
    cmd.Parameters.AddWithValue("$tag", tag) |> ignore
    cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")) |> ignore
    cmd.ExecuteNonQuery() |> ignore

let listContacts () : Contact list =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "SELECT id, email, name, phone, source, tags, notes, created_at FROM contacts ORDER BY id DESC"
    use r = cmd.ExecuteReader()
    [ while r.Read() do yield readContact r ]

let countContacts () =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "SELECT COUNT(*) FROM contacts"
    cmd.ExecuteScalar() :?> int64

let addTag (id: int64) (tag: string) =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        UPDATE contacts
        SET tags = CASE
            WHEN tags = '' THEN $tag
            WHEN instr(',' || tags || ',', ',' || $tag || ',') > 0 THEN tags
            ELSE tags || ',' || $tag
        END
        WHERE id = $id
    """
    cmd.Parameters.AddWithValue("$tag", tag) |> ignore
    cmd.Parameters.AddWithValue("$id", id) |> ignore
    cmd.ExecuteNonQuery() |> ignore

/// Set notes on a contact by email. Only overwrites if new value is non-empty.
let setNotes (email: string) (notes: string) =
    if notes <> "" then
        use conn = openConn ()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "UPDATE contacts SET notes = $notes WHERE email = $email"
        cmd.Parameters.AddWithValue("$notes", notes) |> ignore
        cmd.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant()) |> ignore
        cmd.ExecuteNonQuery() |> ignore

/// Set phone on a contact by email. Only overwrites if new value is non-empty and existing is empty.
let setPhoneIfEmpty (email: string) (phone: string) =
    if phone <> "" then
        use conn = openConn ()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "UPDATE contacts SET phone = $phone WHERE email = $email AND phone = ''"
        cmd.Parameters.AddWithValue("$phone", phone) |> ignore
        cmd.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant()) |> ignore
        cmd.ExecuteNonQuery() |> ignore

let contactsByTag (tag: string) : Contact list =
    if tag = "" then listContacts ()
    else
        use conn = openConn ()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
            SELECT id, email, name, phone, source, tags, notes, created_at
            FROM contacts
            WHERE instr(',' || tags || ',', ',' || $tag || ',') > 0
            ORDER BY id DESC
        """
        cmd.Parameters.AddWithValue("$tag", tag) |> ignore
        use r = cmd.ExecuteReader()
        [ while r.Read() do yield readContact r ]
