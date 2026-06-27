package = "rtm-sdk"
version = "scm-1"
source = {
   url = "git+https://github.com/Raptor3um/rtm-sdk.git"
}
description = {
   summary = "Official Lua SDK for Raptoreum (RTM) JSON-RPC API",
   license = "MIT",
   homepage = "https://github.com/Raptor3um/rtm-sdk"
}
dependencies = {
   "lua >= 5.1"
}
build = {
   type = "builtin",
   modules = {
      raptoreum = "raptoreum.lua"
   }
}
