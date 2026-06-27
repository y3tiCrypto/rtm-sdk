-module(example).
-export([main/0]).

main() ->
    Host = os:getenv("RTM_RPC_HOST", "127.0.0.1"),
    PortStr = os:getenv("RTM_RPC_PORT", "8766"),
    Port = list_to_integer(PortStr),
    User = os:getenv("RTM_RPC_USER", "rtm_rpc_user"),
    Pass = os:getenv("RTM_RPC_PASS", "rtm_rpc_secure_password_98231"),

    io:format("Connecting to Raptoreum Node at http://~s:~p (Erlang)...~n", [Host, Port]),
    Client = raptoreum:new(Host, Port, User, Pass, false),

    case raptoreum:get_blockchain_info(Client) of
        {ok, Body} ->
            io:format("~nConnection Successful!~n"),
            io:format("Response: ~s~n", [Body]);
        {error, Reason} ->
            io:format("~nCould not connect to node: ~p~n", [Reason])
    end.
