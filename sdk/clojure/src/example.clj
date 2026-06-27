(ns example
  (:require [raptoreum :as rtm]))

(defn -main []
  (let [host (or (System/getenv "RTM_RPC_HOST") "127.0.0.1")
        port-str (or (System/getenv "RTM_RPC_PORT") "8766")
        port (Integer/parseInt port-str)
        user (or (System/getenv "RTM_RPC_USER") "rtm_rpc_user")
        pass (or (System/getenv "RTM_RPC_PASS") "rtm_rpc_secure_password_98231")
        client (rtm/make-client :host host :port port :user user :password pass)]
    (println "Connecting to Raptoreum Node at http://" host ":" port " (Clojure)...")
    (try
      (let [info (rtm/get-blockchain-info client)]
        (println "\nConnection Successful!")
        (println info))
      (catch Exception e
        (println "\nCould not connect to node: " (.getMessage e))))))
