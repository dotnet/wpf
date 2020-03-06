#------------------------------------------------------------------------------
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
# Description: Preprocesses XAML files
#
#------------------------------------------------------------------------------

use File::Basename;

if (@ARGV < 3)
{
    print "Usage: PreprocessXAML.pl <unprocessedfile.xaml> <outputfile.xaml> <DefinedConstant1> <DefinedConstant2> ...  ";
    exit();
}

# Pop off unprocessed filename from argument list
$unprocessedFile = shift @ARGV;

# Pop off output filename from argument list
$outputFile = shift @ARGV;

# Pop off defined constant from argument list
while ($constant = shift @ARGV)
{
    print "Constant '$constant' specified.\n";
    $definedConstants = $definedConstants . " /D" . "$constant";
}


######################################################################
#                                                                    #
# Preprocess the unprocessedFile into an intermediate                #
#                                                                    #
######################################################################

$comparisonFile = $unprocessedFile.".temp";

print "PreprocessXAML: CL.exe /nologo /EP $definedConstants $unprocessedFile > $comparisonFile\n";
$message = `CL.exe /nologo /EP $definedConstants $unprocessedFile > $comparisonFile`;


######################################################################
#                                                                    #
# Compare the generated file to the comparison file                  #
#                                                                    #
######################################################################



@diff = `diff $comparisonFile $outputFile`;

for (my $i = 0; $i < @diff; $i++)
{
    my $line = $diff[$i];
    if($line !~ /\S/)
    {
        # remove the line because it is only whitespace
        splice @diff,$i,1;
        $i--;
    }
}

if (@diff)
{
    my $message;
    if ($ENV{"THEMEXAML_AUTOUPDATE"} == 1)
    {
        print "PreprocessXAML : warning generating $outputFile\n";
        $message = "warning : PreprocessXAML :";
    }
    else
    {
        print "PreprocessXAML : error generating $outputFile\n";
        $message = "PreprocessXAML.pl : error";
    }

    print "$message Theme file is out of date. Diff:\n";
    my $i = 0;
    foreach $line (@diff) 
    {
        print "$message $line";
        if ($i++ > 10)
        {
            last;
        }
    }

    print "$message\n";
    print "$message Theme file needs updating";

    if ($ENV{"THEMEXAML_AUTOUPDATE"} == 1)
    {
        print "$message Updating theme file\n";
        print "$message\n";
        print "$message Running: tf edit $outputFile\n";
        print "$message ".`tf edit $outputFile`."\n";
        print "$message\n";
        print "$message Running: copy /y $comparisonFile $outputFile\n";
        print "$message".`copy /y $comparisonFile $outputFile`."\n";
    }
    else
    {
        print "$message Theme file needs updating\n";
        print "$message Run the following commands to replace theme files\n";
        print "$message\n";
        print "$message      tf edit $outputFile\n";
        print "$message      copy $comparisonFile $outputFile\n";
        print "$message\n";
        print "$message Or to automatically checkout and update:\n";
        print "$message      set THEMEXAML_AUTOUPDATE=1\n";
        print "$message\n";
        die "theme file needs updating";
    }
}

