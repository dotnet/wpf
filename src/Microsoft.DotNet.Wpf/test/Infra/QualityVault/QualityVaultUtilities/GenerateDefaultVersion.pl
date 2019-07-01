# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Generate the default value for the /Versions argument (set into FilterSettings.Versions)
# 	perl -f GenerateDefaultVersion.pl -o <output_file>  -v <version_id>

# get arguments from the command line
use Getopt::Std;
my %args;
getopts('o:v:', \%args);
my $outfile = $args{o};
my $versionID = $args{v};

#strip off leading 'v'
$versionID =~ s/^v//;

#error header.
my $error_msg = "GenerateDefaultVersion.pl: error:";

# initialize the output file
open(OUT, '>'.$outfile) or die "$error_msg Cannot create $outfile. $!\n";

# write the beginning
print OUT
"
using System;
using System.Collections.Generic;

namespace Microsoft.Test.Filtering
{
    internal static class DefaultVersion
    {
        internal const string Value = \""
or die "$error_msg Cannot write to $outfile. $!\n";

#write the version ID
print OUT $versionID or die "$error_msg Cannot write to $outfile. $!\n";

#write the end
print OUT
"\";
    }
}
"
or die "$error_msg Cannot write to $outfile. $!\n";

#close files
close (OUT) or die "$error_msg Cannot close $outfile. $!\n";


