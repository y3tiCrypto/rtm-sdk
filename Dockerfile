FROM ubuntu:22.04

# Avoid prompts during installation
ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && apt-get install -y \
    wget \
    tar \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install Raptoreum Core binaries
ENV RTM_VERSION=1.2.1.2
RUN wget https://github.com/Raptor3um/Raptoreum/releases/download/v${RTM_VERSION}/raptoreum-${RTM_VERSION}-x86_64-linux-gnu.tar.gz \
    && tar -xvf raptoreum-${RTM_VERSION}-x86_64-linux-gnu.tar.gz \
    && mv raptoreum-${RTM_VERSION}/bin/* /usr/local/bin/ \
    && rm -rf raptoreum-${RTM_VERSION} raptoreum-${RTM_VERSION}-x86_64-linux-gnu.tar.gz

# Configure default raptoreum.conf
RUN mkdir -p /root/.raptoreumcore && \
    echo "server=1" >> /root/.raptoreumcore/raptoreum.conf && \
    echo "rpcuser=rtmuser" >> /root/.raptoreumcore/raptoreum.conf && \
    echo "rpcpassword=rtmpassword" >> /root/.raptoreumcore/raptoreum.conf && \
    echo "rpcallowip=0.0.0.0/0" >> /root/.raptoreumcore/raptoreum.conf && \
    echo "rpcbind=0.0.0.0" >> /root/.raptoreumcore/raptoreum.conf && \
    echo "txindex=1" >> /root/.raptoreumcore/raptoreum.conf

# Port mappings: Mainnet RPC=8766, Testnet RPC=18766
EXPOSE 8766 8767 18766 18767

ENTRYPOINT ["raptoreumd", "-printtoconsole"]
