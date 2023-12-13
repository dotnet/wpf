#------------------------------------------------------------------------------
# Microsoft Windows Client Platform
# Copyright (c) Microsoft Corporation, 2006
#
# Description: Prepares TrustUI theme files for SDK and Sparkle
#
#------------------------------------------------------------------------------

if (@ARGV < 2)
{
    print "Usage: themegenerator.pl <inputfile.xaml> <outputfile.xaml>";
    exit();
}

#Get the version number of Avalon for this file
$objroot = $ENV{"OBJECT_ROOT"};
$o = $ENV{"O"};

#open(VERSION, "<$objroot/$o/WCP.FileVersion") ||  die "failed to get version number";
$version = 3.0.0.0; #<VERSION>;
close(VERSION);

# Pop off unoptimized output filename from argument list
$inputFile = shift @ARGV;

# Pop off output filename from argument list
$outputFile = shift @ARGV;


#Open the input file 
open(INFILE, "<$inputFile") || die "can't open file $inputFile";

#Open output file - this should be in the output directory
open(OUTFILE,">$outputFile") || die "can't open the output file \"$outputFile\"";

print OUTFILE <<END;
<!--===========================================================================
Copyright (C) Microsoft Corporation.  All rights reserved.

PresentationUI Styles For Windows Presentation Foundation Version $version 
============================================================================-->
END

$currentLine = 0;
$inComment = 0;
while (<INFILE>)
{
    $currentLine++;
    
    #ignore first line - '\S' matches Unicode BOM
    if($currentLine > 1 && /(\s*\S+\s*<!--|-->\s*\S+\s*)/)
    {
        die "error $inputFile:$currentLine: Comments must be on their own line (or this script needs xml processing)";
    }

    # remove comments for 
    if (/<!--/)
    {
        $inComment = 1;
    }
        
    if (!$inComment)
    {
        print OUTFILE $_;
    }
    else 
    {
        print OUTFILE "";
    }

    if (/-->/)
    {
        $inComment = 0;
    }
}

close(INFILE);
close(OUTFILE);
