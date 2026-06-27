open System
open RaptoreumSdk

[<EntryPoint>]
let main argv =
    let host = match Environment.GetEnvironmentVariable("RTM_RPC_HOST") with null -> "127.0.0.1" | h -> h
    let portStr = match Environment.GetEnvironmentVariable("RTM_RPC_PORT") with null -> "8766" | p -> p
    let mutable port = 8766
    Int32.TryParse(portStr, &port) |> ignore
    let user = match Environment.GetEnvironmentVariable("RTM_RPC_USER") with null -> "rtm_rpc_user" | u -> u
    let pass = match Environment.GetEnvironmentVariable("RTM_RPC_PASS") with null -> "rtm_rpc_secure_password_98231" | p -> p

    printfn "Connecting to Raptoreum Node at http://%s:%d (F#)..." host port
    let client = RaptoreumClient(host, port, user, pass, false)

    try
        let info = client.GetBlockchainInfoAsync().Result
        printfn "\nConnection Successful!"
        printfn "Response: %O" info
    with
    | ex -> printfn "\nCould not connect to node: %s" ex.Message

    0
