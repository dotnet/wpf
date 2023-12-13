########################################################
# Generate the wrapper classes for managed debug tracing.
#
# This scripts reads an input file with the trace information,
# and generates a cs file with the wrapper classes.
#
########################################################
use File::Copy;
use Getopt::Std;

sub usage()
{
    print  "Usage: $0 -i [Inputfile1] -o [output.cs] \n\n";
}

#hash to store all the arguements.
my %args
;
#error header.
my $error_msg = "GenTraceStrings.pl: error:";

#get the switches values.
getopts('i:o:', \%args);

#all these switches are required.

my $outfile = $args{o};
my $inputfile = $args{i};

print "\n";


#
# The header for the file
#

my $header  = <<"END_OF_HEADER";

using System;
using System.Diagnostics;

namespace MS.Internal
{
END_OF_HEADER


#
# The footer for the file
#

my $footer = <<"END_OF_FOOTER";
}//endof namespace
END_OF_FOOTER


#
# The footer section for each class
#

my $classFooter  = <<"END_OF_FOOTER";

        // Send a single trace output
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, params object[] parameters )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        // these help delay allocation of object array
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[0] );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void Trace( TraceEventType type, AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.Trace( type, traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        // Send a singleton "activity" trace (really, this sends the same trace as both a Start and a Stop)
        static public void TraceActivityItem( AvTraceDetails traceDetails, params Object[] parameters )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, parameters );
        }

        // these help delay allocation of object array
        static public void TraceActivityItem( AvTraceDetails traceDetails )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[0] );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2 } );
        }
        static public void TraceActivityItem( AvTraceDetails traceDetails, object p1, object p2, object p3 )
        {
            _avTrace.TraceStartStop( traceDetails.Id, traceDetails.Message, traceDetails.Labels, new object[] { p1, p2, p3 } );
        }

        // Is tracing enabled here?
        static public bool IsEnabled
        {
            get { return _avTrace != null && _avTrace.IsEnabled; }
        }

        // Is there a Tracesource?  (See comment on AvTrace.IsEnabledOverride.)
        static public bool IsEnabledOverride
        {
            get { return _avTrace.IsEnabledOverride; }
        }

        // Re-read the configuration for this trace source
        static public void Refresh()
        {
            _avTrace.Refresh();
        }

    }//endof class $srclass
END_OF_FOOTER



#
#  Initialize the output file
#

open(OUT, '>'.$outfile) or die "$error_msg Cannot create $outfile. $!\n";

print OUT $header or die "$error_msg Cannot write to $outfile. $! \n";



{
    # Now we read the trace information from the file

    open(IN, $inputfile) or die "$error_msg Cannot open $inputfile. $!\n";


    # Loop through the strings

    my $prev_id = 0;
    my $max_id = 0;
    my $inClass = 0;
    my $traceArea;
    my $traceClass;
    my $traceName;
    my $traceSourceName;

    while ($stringIn = <IN>)
    {
        chomp;
        next if ($stringIn =~ /^;/);    # ignore all comments
        next if ($stringIn =~ /^\r/);    # ignore newline

        # Handle the beginning of a section, with the trace source name,
        # the name of the area, and the name of the class to be generated.
        # E.g. "[System.Windows.ComponentModel.Events,RoutedEvent,TraceRoutedEvent]"

        if (($traceName, $traceArea, $traceClass) = ($stringIn =~ /^\[(.*),(.*),(.*)\]/))
        {
            # append "Source" for the property name to be used in PresentationTraceSources

            $traceSourceName = $traceArea."Source";

            # Write out the class header

            print OUT
"
    static internal partial class $traceClass
    {
        static private AvTrace _avTrace = new AvTrace(
                delegate() { return PresentationTraceSources.$traceSourceName; },
                delegate() { PresentationTraceSources._$traceSourceName = null; }
                );

"
            or die "$error_msg Cannot write to $outfile. $! \n";

            # reset id auto-generation counters
            $max_id = $prev_id = 0;
            $inClass = 1;
            next;
        }

        # Check for the end of a section, e.g. "[end]"

        if ($stringIn =~ /^\[end]/)
        {
            print OUT $classFooter or die "$error_msg Cannot write to $outfile. $! \n";
            $inClass = 0;
            next;
        }


        # Handle a line within a section, which has the trace strings
        # E.g. MyEvent=AUTO,FORMAT,{"Basic message or format string", "Param1", "Param2"}

        if (($name, $id, $shouldFormat, $labels) = ($stringIn =~ /^(\w+)=(\w*)\,(\w*)\,(.*)/))
        {
            $inClass or die "Trace string '$stringIn' is not inside a section.";

            # auto-generate id if int is not specified
            if ($id =~ /^\d+$/)
            {
                $max_id = $id if $id > $max_id;
            }
            elsif ($id == "" || $id == "AUTO")
            {
                $id = ++$max_id;
            }
            elsif ($id == "PREVIOUS")
            {
                $id = $prev_id;
            }
            else
            {
                die "invalid id '$id' for trace string.";
            }

            if ($shouldFormat)
            {
                # create a method that passes args for the format string

                # TODO: calculate number of parameters and create precise method signature

                print OUT
"
        static AvTraceDetails _$name;
        static public AvTraceDetails $name(params object[] args)
        {
            if ( _$name == null )
            {
                _$name = new AvTraceDetails( $id, new string[] $labels );
            }

            return new AvTraceFormat(_$name, args);
        }
"
                or die "$error_msg Cannot write to $outfile. $! \n";

            }
            elsif ($shouldFormat == "" || $shouldFormat =~ /false/i)
            {
                # create a property
                print OUT
"
        static AvTraceDetails _$name;
        static public AvTraceDetails $name
        {
            get
            {
                if ( _$name == null )
                {
                    _$name = new AvTraceDetails( $id, new string[] $labels );
                }

                return _$name;
            }
        }
"
                or die "$error_msg Cannot write to $outfile. $! \n";

            }
            else
            {
                die "invalid value '$id' for trace string ShouldFormat boolean.";
            }

            $prev_id = $id;
            next;
        }


        next if ($stringIn =~ /^$/);

        die "Invalid trace string '$stringIn'";
    }

    #close files
    close (IN) or die "$error_msg Cannot close $infile. $!\n";
}

print OUT "\n".$footer or die "$error_msg Cannot write to $outfile. $! \n";

#close files
close (OUT) or die "$error_msg Cannot close $outfile. $!\n";

