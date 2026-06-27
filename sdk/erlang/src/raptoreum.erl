-module(raptoreum).
-export([new/5, request/3, get_blockchain_info/1, get_block_count/1, get_balance/1]).

new(Host, Port, User, Pass, UseSsl) ->
    Scheme = case UseSsl of
        true -> "https";
        false -> "http"
    end,
    Url = Scheme ++ "://" ++ Host ++ ":" ++ integer_to_list(Port) ++ "/",
    AuthHeader = case {User, Pass} of
        {"", ""} -> [];
        _ ->
            Credentials = User ++ ":" ++ Pass,
            Encoded = base64:encode_to_string(Credentials),
            [{"Authorization", "Basic " ++ Encoded}]
    end,
    {Url, AuthHeader}.

request({Url, AuthHeaders}, Method, ParamsListJson) ->
    Payload = "{\"jsonrpc\":\"1.0\",\"id\":\"rtm-sdk-erlang\",\"method\":\"" ++ Method ++ "\",\"params\":" ++ ParamsListJson ++ "}",
    Headers = [{"Content-Type", "application/json"} | AuthHeaders],
    
    inets:start(),
    ssl:start(),
    
    case httpc:request(post, {Url, Headers, "application/json", Payload}, [], []) of
        {ok, {{_, 200, _}, _, Body}} -> {ok, Body};
        {ok, {{_, 500, _}, _, Body}} -> {ok, Body};
        {ok, {{_, Status, _}, _, _}} -> {error, {http_status, Status}};
        {error, Reason} -> {error, Reason}
    end.

get_blockchain_info(Client) -> request(Client, "getblockchaininfo", "[]").
get_block_count(Client) -> request(Client, "getblockcount", "[]").
get_balance(Client) -> request(Client, "getbalance", "[]").
