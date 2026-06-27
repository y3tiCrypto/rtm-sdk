{-# LANGUAGE OverloadedStrings #-}

module Main where

import System.Environment
import qualified Data.Text as T
import Raptoreum
import Control.Exception (catch, SomeException)

catchError :: IO a -> (SomeException -> IO a) -> IO a
catchError = catch

main :: IO ()
main = do
    host <- getEnv "RTM_RPC_HOST" `catchError` (\_ -> return "127.0.0.1")
    portStr <- getEnv "RTM_RPC_PORT" `catchError` (\_ -> return "8766")
    let port = read portStr :: Int
    user <- getEnv "RTM_RPC_USER" `catchError` (\_ -> return "rtm_rpc_user")
    pass <- getEnv "RTM_RPC_PASS" `catchError` (\_ -> return "rtm_rpc_secure_password_98231")

    putStrLn $ "Connecting to Raptoreum Node at http://" ++ host ++ ":" ++ show port ++ " (Haskell)..."
    let client = newClient host port user pass False
    
    res <- request client "getblockchaininfo" []
    case res of
        Left err -> putStrLn $ "\nCould not connect to node: " ++ err
        Right val -> do
            putStrLn "\nConnection Successful!"
            print val
