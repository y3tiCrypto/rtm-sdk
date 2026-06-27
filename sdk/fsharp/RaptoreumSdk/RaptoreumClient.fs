namespace RaptoreumSdk

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Threading.Tasks

type RaptoreumRPCException(code: int, message: string) =
    inherit Exception(sprintf "RPC Error [%d]: %s" code message)
    member this.Code = code

type RaptoreumClient(host: string, port: int, user: string, password: string, useSsl: bool) =
    let scheme = if useSsl then "https" else "http"
    let baseUrl = sprintf "%s://%s:%d/" scheme host port
    let httpClient = new HttpClient()

    do
        httpClient.DefaultRequestHeaders.Accept.Clear()
        httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        if not (String.IsNullOrEmpty(user)) || not (String.IsNullOrEmpty(password)) then
            let authBytes = Encoding.ASCII.GetBytes(sprintf "%s:%s" user password)
            httpClient.DefaultRequestHeaders.Authorization <- 
                AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes))

    new() = RaptoreumClient("127.0.0.1", 8766, "", "", false)

    member this.RequestAsync(methodName: string, [<ParamArray>] parameters: obj[]) : Task<JsonElement> =
        task {
            let payload = {| jsonrpc = "1.0"; id = "rtm-sdk-fsharp"; method = methodName; ``params`` = parameters |}
            let jsonPayload = JsonSerializer.Serialize(payload)
            use content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            
            let! response = httpClient.PostAsync(baseUrl, content)
            if not response.IsSuccessStatusCode && response.StatusCode <> System.Net.HttpStatusCode.InternalServerError then
                raise (new HttpRequestException(sprintf "HTTP error status code: %O" response.StatusCode))
                
            let! responseString = response.Content.ReadAsStringAsync()
            use doc = JsonDocument.Parse(responseString)
            let root = doc.RootElement.Clone()
            
            let mutable errProp = new JsonElement()
            if root.TryGetProperty("error", &errProp) && errProp.ValueKind <> JsonValueKind.Null then
                let code = errProp.GetProperty("code").GetInt32()
                let message = errProp.GetProperty("message").GetString()
                raise (new RaptoreumRPCException(code, message))
                
            return root.GetProperty("result").Clone()
        }

    member this.GetBlockchainInfoAsync() = this.RequestAsync("getblockchaininfo", [||])
    member this.GetBlockCountAsync() = this.RequestAsync("getblockcount", [||])
    member this.GetBalanceAsync() = this.RequestAsync("getbalance", [||])
    member this.ValidateAddressAsync(address: string) = this.RequestAsync("validateaddress", [| address |])
    member this.SendManyAsync(amounts: System.Collections.Generic.IDictionary<string, double>, minconf: int, comment: string) = 
        this.RequestAsync("sendmany", [| "" :> obj; amounts :> obj; minconf :> obj; comment :> obj |])
