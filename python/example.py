import os
from raptoreum import RaptoreumClient

def main():
    # Instantiate the client (update with your node details or env)
    host = os.getenv("RTM_RPC_HOST", "127.0.0.1")
    port = int(os.getenv("RTM_RPC_PORT", 8766))
    user = os.getenv("RTM_RPC_USER", "rtm_rpc_user")
    password = os.getenv("RTM_RPC_PASS", "rtm_rpc_secure_password_98231")
    
    print(f"Connecting to Raptoreum Node at http://{host}:{port}...")
    client = RaptoreumClient(host=host, port=port, user=user, password=password)
    
    try:
        info = client.getblockchaininfo()
        print("\nConnection Successful!")
        print(f"Chain: {info.get('chain')}")
        print(f"Blocks: {info.get('blocks')}")
        print(f"Difficulty: {info.get('difficulty')}")
    except Exception as e:
        print(f"\nCould not connect to node: {e}")
        print("Make sure your Raptoreum node is running and the credentials in example.py match your raptoreum.conf configuration.")

if __name__ == "__main__":
    main()
