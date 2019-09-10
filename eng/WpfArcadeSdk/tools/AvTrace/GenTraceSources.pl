########################################################
# Generate the enum used for retrieving wrapper classes for managed debug tracing.
#
# This scripts reads an input file with the trace information,
# and generates a cs file with the enum containing trace area values.
#
########################################################
use File::Copy;
use Getopt::Std;

sub usage()
{
    print  "Usage: $0 -i [Inputfile1,Inputfile2,...] -o [output.cs] \n\n";
}

#hash to store all the arguements.
my %args
;    
#error header.
my $error_msg = "GenTraceSources.pl: error:";    

#get the switches values.
getopts('i:o:', \%args);

#all these switches are required. 

my $outfile = $args{o};
my @inputfilelist = split(/,/, $args{i});

print "\n";


#
# The header for the file
#

my $header  = <<"END_OF_HEADER";
//-------------------------------------------------------------------------------
// <copyright from='2006' to='2006' company='Microsoft Corporation'>           
//    Copyright (c) Microsoft Corporation. All Rights Reserved.                
//    Information Contained Herein is Proprietary and Confidential.            
// </copyright>                                                                
//
// This file is generated from AvTraceMessages.txt by gentracesources.pl - do not modify this file directly
//-------------------------------------------------------------------------------

using MS.Internal;

namespace System.Diagnostics
{
    /// <summary>Access point for TraceSources</summary>
    public static partial class PresentationTraceSources
    {
END_OF_HEADER


#
# The footer for the file
#

my $footer = <<"END_OF_FOOTER";
    }
}//endof namespace
END_OF_FOOTER


# 
#  Initialize the output file
# 

open(OUT, '>'.$outfile) or die "$error_msg Cannot create $outfile. $!\n";

print OUT $header or die "$error_msg Cannot write to $outfile. $! \n";

{  
    # Now we read the trace areas from the files

    for $inputfile (@inputfilelist)
    {  

        open(IN, $inputfile) or die "$error_msg Cannot open $inputfile. $!\n";


        # Loop through the strings

        my $traceArea;
        my $traceClass;
        my $traceName;
        my $traceSourceName;

        while ($stringIn = <IN>) 
        {
            chomp;

            # Find the beginning of a section, with the trace source name
            # and the name of the trace area.
            # E.g. "[System.Windows.ComponentModel.Events,RoutedEvent]"

            if (($traceName, $traceArea, $traceClass) = ($stringIn =~ /^\[(.*),(.*),(.*)\]/))
            {
                # append "Source" for the TraceSource name
                $traceSourceName = $traceArea."Source";

                # Write out the property

                print OUT
"
        /// <summary>$traceSourceName for $traceArea</summary>
        public static TraceSource $traceSourceName
        {
            get
            {
                if (_$traceSourceName == null)
                {
                    _$traceSourceName = CreateTraceSource(\"$traceName\");
                }
                return _$traceSourceName;
            }
        }
        internal static TraceSource _$traceSourceName;
"
            or die "$error_msg Cannot write to $outfile. $! \n";

            }
        }

        #close files
        close (IN) or die "$error_msg Cannot close $infile. $!\n";
    }
}

print OUT "\n".$footer or die "$error_msg Cannot write to $outfile. $! \n";

#close files
close (OUT) or die "$error_msg Cannot close $outfile. $!\n";

