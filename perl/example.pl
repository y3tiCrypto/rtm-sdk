use strict;
use warnings;
use lib 'lib';
use RaptoreumClient;
use Data::Dumper;

my $host = $ENV{RTM_RPC_HOST} || '127.0.0.1';
my $port = $ENV{RTM_RPC_PORT} || 8766;
my $user = $ENV{RTM_RPC_USER} || 'rtm_rpc_user';
my $pass = $ENV{RTM_RPC_PASS} || 'rtm_rpc_secure_password_98231';

print "Connecting to Raptoreum Node at http://$host:$port (Perl)...\n";
my $client = RaptoreumClient->new(
    host => $host,
    port => $port,
    user => $user,
    password => $pass
);

eval {
    my $info = $client->getblockchaininfo();
    print "\nConnection Successful!\n";
    print Dumper($info);
};
if ($@) {
    print "\nCould not connect to node: $@\n";
}
