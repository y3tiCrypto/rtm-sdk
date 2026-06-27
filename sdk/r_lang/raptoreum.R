library(httr)
library(jsonlite)

RaptoreumClient <- function(host = "127.0.0.1", port = 8766, user = "", password = "", use_ssl = FALSE) {
  scheme <- if (use_ssl) "https" else "http"
  url <- paste0(scheme, "://", host, ":", port, "/")
  
  auth <- NULL
  if (user != "" || password != "") {
    auth <- authenticate(user, password, type = "basic")
  }
  
  request <- function(method, params = list()) {
    payload <- list(
      jsonrpc = "1.0",
      id = "rtm-sdk-r",
      method = method,
      params = params
    )
    
    response <- POST(
      url,
      body = payload,
      encode = "json",
      add_headers("Content-Type" = "application/json"),
      auth
    )
    
    if (status_code(response) != 200 && status_code(response) != 500) {
      stop(paste("HTTP Error:", status_code(response)))
    }
    
    content_raw <- content(response, as = "text", encoding = "UTF-8")
    parsed <- fromJSON(content_raw)
    
    if (!is.null(parsed$error)) {
      err <- simpleError(paste("RPC Error [", parsed$error$code, "]:", parsed$error$message))
      err$code <- parsed$error$code
      err$rpc_message <- parsed$error$message
      class(err) <- c("RaptoreumRPCError", class(err))
      stop(err)
    }
    
    return(parsed$result)
  }
  
  list(
    request = request,
    getblockchaininfo = function() request("getblockchaininfo"),
    getblockcount = function() request("getblockcount"),
    getbalance = function() request("getbalance"),
    validateaddress = function(address) request("validateaddress", list(address)),
    sendmany = function(amounts_list, minconf = 1, comment = "") request("sendmany", list("", amounts_list, minconf, comment))
  )
}
