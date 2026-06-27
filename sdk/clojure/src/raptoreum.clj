(ns raptoreum
  (:require [clj-http.client :as client]
            [cheshire.core :as json])
  (:import [java.util Base64]))

(defn- base64-encode [s]
  (.encodeToString (Base64/getEncoder) (.getBytes s "UTF-8")))

(defn make-client [& {:keys [host port user password use-ssl]
                      :or {host "127.0.0.1" port 8766 use-ssl false}}]
  (let [scheme (if use-ssl "https" "http")
        url (str scheme "://" host ":" port "/")
        auth (if (and user password (not= user "") (not= password ""))
               (str "Basic " (base64-encode (str user ":" password)))
               nil)]
    {:url url :auth auth}))

(defn request [client method & [params]]
  (let [payload (json/generate-string
                 {:jsonrpc "1.0"
                  :id "rtm-sdk-clojure"
                  :method method
                  :params (or params [])})
        headers (merge {"Content-Type" => "application/json"}
                       (if (:auth client) {"Authorization" => (:auth client)} {}))
        response (client/post (:url client)
                              {:headers headers
                               :body payload
                               :throw-exceptions false})
        status (:status response)
        body (json/parse-string (:body response) true)]
    (if (and (not= status 200) (not= status 500))
      (throw (Exception. (str "HTTP Error: Status " status)))
      (if-let [err (:error body)]
        (throw (ex-info (str "RPC Error [" (:code err) "]: " (:message err))
                        {:type :raptoreum/rpc-error
                         :code (:code err)
                         :message (:message err)}))
        (:result body)))))

(defn get-blockchain-info [client]
  (request client "getblockchaininfo"))

(defn get-block-count [client]
  (request client "getblockcount"))

(defn get-balance [client]
  (request client "getbalance"))

(defn validate-address [client address]
  (request client "validateaddress" [address]))

(defn send-many [client amounts & [minconf comment]]
  (request client "sendmany" ["" amounts (or minconf 1) (or comment "")]))

(defn list-assets [client & [mine]]
  (request client "listassets" [(or mine false)]))

(defn create-asset [client name amount & [options]]
  (request client "createasset" [name amount (or options {})]))

(defn send-asset [client asset-id amount address]
  (request client "sendasset" [asset-id amount address]))
