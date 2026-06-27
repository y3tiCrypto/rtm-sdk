source("raptoreum.R")

host <- Sys.getenv("RTM_RPC_HOST", unset = "127.0.0.1")
port <- as.numeric(Sys.getenv("RTM_RPC_PORT", unset = "8766"))
user <- Sys.getenv("RTM_RPC_USER", unset = "rtm_rpc_user")
pass <- Sys.getenv("RTM_RPC_PASS", unset = "rtm_rpc_secure_password_98231")

print(paste("Connecting to Raptoreum Node at http://", host, ":", port, "(R)..."))
client <- RaptoreumClient(host, port, user, pass, FALSE)

tryCatch({
  info <- client$getblockchaininfo()
  print("Connection Successful!")
  print(info)
}, error = function(e) {
  print(paste("Could not connect to node:", e$message))
})
