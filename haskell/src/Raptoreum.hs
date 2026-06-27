{-# LANGUAGE OverloadedStrings #-}
{-# LANGUAGE DeriveGeneric #-}

module Raptoreum (
    RaptoreumClient(..),
    newClient,
    request
) where

import Data.Aeson
import GHC.Generics
import Network.HTTP.Simple
import qualified Data.ByteString.Lazy.Char8 as LBS
import qualified Data.ByteString.Base64 as B64
import qualified Data.Text as T
import qualified Data.Text.Encoding as TE

data RaptoreumClient = RaptoreumClient {
    clientUrl  :: String,
    clientAuth :: Maybe T.Text
} deriving (Show)

data RPCRequest = RPCRequest {
    jsonrpc :: T.Text,
    id      :: T.Text,
    method  :: T.Text,
    params  :: [Value]
} deriving (Show, Generic)

instance ToJSON RPCRequest

data RPCResponse = RPCResponse {
    result :: Maybe Value,
    error  :: Maybe Value
} deriving (Show, Generic)

instance FromJSON RPCResponse

newClient :: String -> Int -> String -> String -> Bool -> RaptoreumClient
newClient host port user pass useSsl =
    let scheme = if useSsl then "https" else "http"
        url = scheme ++ "://" ++ host ++ ":" ++ show port ++ "/"
        authHeader = if null user && null pass
                     then Nothing
                     else Just $ "Basic " <> TE.decodeUtf8 (B64.encode (TE.encodeUtf8 (T.pack (user ++ ":" ++ pass))))
    in RaptoreumClient url authHeader

request :: RaptoreumClient -> T.Text -> [Value] -> IO (Either String Value)
request client m ps = do
    let payload = RPCRequest "1.0" "rtm-sdk-haskell" m ps
    initReq <- parseRequest (clientUrl client)
    let req = setRequestMethod "POST"
            $ setRequestBodyJSON payload
            $ setRequestHeader "Content-Type" ["application/json"]
            $ maybe id (\auth -> setRequestHeader "Authorization" [TE.encodeUtf8 auth]) (clientAuth client)
            $ initReq
            
    response <- httpLBS req
    let body = getResponseBody response
    case eitherDecode body of
        Left err -> return $ Left ("JSON Decode Error: " ++ err)
        Right rpcResp -> case Raptoreum.error rpcResp of
            Just errVal -> return $ Left ("RPC Error: " ++ show errVal)
            Nothing -> case result rpcResp of
                Just resVal -> return $ Right resVal
                Nothing -> return $ Right Null
