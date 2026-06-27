(defproject rtm-sdk "1.0.0"
  :description "Official Clojure SDK for Raptoreum (RTM) JSON-RPC API"
  :url "https://github.com/Raptor3um/rtm-sdk"
  :license {:name "MIT" :url "https://opensource.org/licenses/MIT"}
  :dependencies [[org.clojure/clojure "1.11.1"]
                 [clj-http "3.12.3"]
                 [cheshire "5.11.0"]]
  :repl-options {:init-ns raptoreum})
