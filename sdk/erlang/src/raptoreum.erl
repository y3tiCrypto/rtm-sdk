-module(raptoreum).
-export([new/5, request/3, get_blockchain_info/1, get_block_count/1, get_balance/1, validate_address/2, send_many/4, list_assets/2, create_asset/4, send_asset/4]).

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
        {ok, {{_, 200, _}, _, Body}} -> check_rpc_error(Body);
        {ok, {{_, 500, _}, _, Body}} -> check_rpc_error(Body);
        {ok, {{_, Status, _}, _, _}} -> {error, {http_status, Status}};
        {error, Reason} -> {error, Reason}
    end.

check_rpc_error(Body) ->
    case string:str(Body, "\"error\":") of
        0 -> {ok, Body};
        _ ->
            case (string:str(Body, "\"error\":null") =:= 0) andalso (string:str(Body, "\"error\": null") =:= 0) of
                true -> {error, {rpc_error, Body}};
                false -> {ok, Body}
            end
    end.

get_blockchain_info(Client) -> request(Client, "getblockchaininfo", "[]").
get_block_count(Client) -> request(Client, "getblockcount", "[]").
get_balance(Client) -> request(Client, "getbalance", "[]").
validate_address(Client, Address) -> request(Client, "validateaddress", "[\"" ++ Address ++ "\"]").
send_many(Client, AmountsJson, MinConf, Comment) ->
    request(Client, "sendmany", "[\"\", " ++ AmountsJson ++ ", " ++ integer_to_list(MinConf) ++ ", \"" ++ Comment ++ "\"]").

list_assets(Client, Mine) ->
    MineStr = case Mine of
        true -> "true";
        false -> "false"
    end,
    request(Client, "listassets", "[" ++ MineStr ++ "]").

create_asset(Client, Name, Amount, OptionsJson) ->
    request(Client, "createasset", "[\"" ++ Name ++ "\", " ++ io_lib:format("~.8f", [Amount * 1.0]) ++ ", " ++ OptionsJson ++ "]").

send_asset(Client, AssetId, Amount, Address) ->
    request(Client, "sendasset", "[\"" ++ AssetId ++ "\", " ++ io_lib:format("~.8f", [Amount * 1.0]) ++ ", \"" ++ Address ++ "\"]").
