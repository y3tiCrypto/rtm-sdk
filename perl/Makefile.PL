use ExtUtils::MakeMaker;

WriteMakefile(
    NAME             => 'RaptoreumClient',
    VERSION_FROM     => 'lib/RaptoreumClient.pm',
    PREREQ_PM        => {
        'LWP::UserAgent' => 0,
        'JSON'           => 0,
        'MIME::Base64'   => 0,
    },
    AUTHOR           => 'Raptoreum Developers <dev@raptoreum.com>',
);
