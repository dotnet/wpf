#------------------------------------------------------------------------------
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
# Description: Concatenates separate xaml files into one large xaml file
#              for use in the theme dictionary.
#              Converts string keys into smaller versions of the keys
#
#------------------------------------------------------------------------------

# set to 0 to disable replacing keys with short string for the checked in version
$optimizeKeys = 1;

# set to 0 to disable replacing keys with short string
# Currently disabled because Automation relies on template Names
$optimizeNames = 0;

use File::Basename;

if (@ARGV < 4)
{
    print "Usage: themexaml.pl <unoptimizedoutputfile.xaml> <outputfile.xaml> <comparison.xaml> <in1.xaml> <in2.xaml> ...  ";
    exit();
}

#Parallel arrays of:
#All style info from the file for this theme (x:Key's shortened to 1 char)
@styles = ();

#All style info from the file for this theme with full key names
@unoptimizedStyles = ();

#All resources defined by this style
@resourcesDefined = ();

#References to resources not defined in the file
@resourcesReferenced = ();

#hashtable of descriptive resource keys -> unicode resourceCharacter
%resourceKeyMap = ();

# starting resourceCharacter for the shorter strings - converted to a unicode resourceChar by xml parser
$resourceChar = 200;

# Pop off unoptimized output filename from argument list
$unoptimizedFile = shift @ARGV;

# Pop off output filename from argument list
$outputFile = shift @ARGV;

# Pop off comparison filename from argument list
$comparisonFile = shift @ARGV;

# Extract name of this theme from the comparison filename
$themeName = basename($comparisonFile, ".xaml");

# List of all keys used - this is to see if there are unreferenced resources
%usedKeys = ();

# List of all DynamicResource errors (reference to locally-named resource)
%dynamicResourceErrors = ();

######################################################################
#                                                                    #
# Read all style files, shorten keys and template names              #
#                                                                    #
######################################################################

# for each filename in the rest of the arguments
# Save the contents of the file that are under the [[ themename ]] tags,
# replace descriptive resource key strings with 1 char versions
# and replace long template Names with short versions
foreach $infile (@ARGV)
{
    # Read File given in the First Argument to an array "all"
    open(INFILE, "<$infile") || die "can't open file $infile";

    my @style = ();
    my @unoptimizedStyle = ();
    my %defined = ();
    my %referenced = ();
    my %templateNameMap = ();

    # Remove comments for unoptimized version
    my $inComment = 0;

    # starting character for the shorter template names (repeats ok)
    # this assumes there are < 26 named parts in a template
    my $nameChar = 'a';

    # only add lines to the style array if they occur after a [[ themename ]] line
    my $lineIsInContextOfCurrentTheme = 0;

    my $currentLine = 0;

    while (<INFILE>)
    {
        $currentLine++;

        if (/\[\[(.*)\]\]/)
        {
            $lineIsInContextOfCurrentTheme = ($1 =~ /$themeName/i);
            next;
        }

        if (!$lineIsInContextOfCurrentTheme)
        {
            # skip line until we are in current theme
            next;
        }

        if (/(\s*\S+\s*<!--|-->\s*\S+\s*)/)
        {
          die "error $infile:$currentLine: Comments must be on their own line (or this script needs xml processing)";
        }

        # remove comments for unoptimized version
        if (/<!--/)
        {
            $inComment = 1;
        }

        if (!$inComment)
        {
            # save an unoptimized version of this line of the style
            push(@unoptimizedStyle, $_);
        }
        else
        {
           push (@unoptimizedStyle, "");
        }

        if (/-->/)
        {
            $inComment = 0;
        }

        # Optimize the xaml by replacing verbose keys with a short version

        # Find All keys, no {markup extensions} allowed
        if (/x:Key="([^{}"]+)"/)
        {
            my $shortKey = $resourceKeyMap{$1};

            # if short key wasn't found in the map, create it and add to map
            if (!$shortKey)
            {
                $shortKey = $resourceChar++;
                $resourceKeyMap{$1} = $shortKey;
            }

            $defined{$1} = 1;

            # replace the key on this line
            if ($optimizeKeys)
            {
                $_ =~ s/"$1"/"&#$shortKey;"/g;
            }
        }

        # Find Dynamic/Static Resource references
        $line = $_;
        while ($line =~ /(StaticResource|DynamicResource) ([^{]+?)}/g)
        {
            # record DynamicResource reference to locally-named resource
            if ($1 eq "DynamicResource")
            {
                push(@dynamicResourceErrors, "$infile:$currentLine: $line");
            }

            my $shortKey = $resourceKeyMap{$2};

            # if short key wasn't found in the map, create it and add to map
            if (!$shortKey)
            {
                $shortKey = $resourceChar++;
                $resourceKeyMap{$2} = $shortKey;
            }

            # if the resource reference hasn't been defined yet,
            # the resource exists in a different file
            if (!exists $defined{$2})
            {
                $referenced{$2} = 1;
            }

            $usedKeys{$2} = 1;

            # replace the key on this line
            if ($optimizeKeys)
            {
                $_ =~ s/Resource $2}/Resource &#$shortKey;}/g;
            }
        }

        # Shorten Template Names
        if (!/PART_/ && /Name="?(.+?)[",]/)
        {
            $shortName = $templateNameMap{$1};

            # if short name wasn't found in the map, create it and add to map
            if (!$shortName)
            {
                $shortName = $templateNameMap{$1} = $nameChar++;
            }

            # replace the name on this line
            if ($optimizeNames)
            {
                $_ =~ s/Name=("?)$1/Name=$1$shortName/g;
            }
        }

        # save this line of the style
        push(@style, $_);
    }

    if (@style > 0)
    {
        push(@styles, \@style);
        push(@unoptimizedStyles, \@unoptimizedStyle);
        push(@resourcesDefined, \%defined);
        push(@resourcesReferenced, \%referenced);
    }

    close(INFILE);
}


# report DynamicResource errors
if (@dynamicResourceErrors > 0)
{
    print "DynamicResource references to locally-named resources:\n";
    foreach $dynamicResourceError (@dynamicResourceErrors)
    {
        print " $dynamicResourceError\n";
    }
    print "Replace these by StaticResource or by DynamicResoure to ComponentResourceKey or to a well-known string\n";
    die "DynamicResource errors found."
}


######################################################################
#                                                                    #
# Resolve dependencies between files and add to output               #
#                                                                    #
######################################################################

#Generate the version number of Avalon for this file
$objroot = $ENV{"OBJECT_ROOT"};
$o = $ENV{"O"};

#open(VERSION, "<$objroot/$o/WCP.FileVersion") ||  die "failed to get version number";
$version = 3.0.0.0; #<VERSION>;
close(VERSION);

#Open output file - this should be in the output directory
open(OUTFILE,">$outputFile") || die "can't open the output file \"$outputFile\"";

#Open unoptimized output file - this should be in the output directory
open(UNOPTFILE,">$unoptimizedFile") || die "can't open the output file \"$unoptimizedFile\"";

# output Copyright and building description to optimized file

print OUTFILE <<'END';

<!--=================================================================
Licensed to the .NET Foundation under one or more agreements.
The .NET Foundation licenses this file to you under the MIT license.
See the LICENSE file in the project root for more information.

This file was generated from individual xaml files found
   in WPF\src\Themes\XAML\, please do not edit it directly.

To generate this file, bcz in WPF\src\Themes\Generator and copy
   the generated theme files from the output directory to
   the corresponding themes\ folder.

To automatically copy the files, set the environment variable
   set THEMEXAML_AUTOUPDATE=1

==================================================================-->
END

# output Copyright and version number to unoptimized file

print UNOPTFILE <<END;
<!--=================================================================
Licensed to the .NET Foundation under one or more agreements.
The .NET Foundation licenses this file to you under the MIT license.
See the LICENSE file in the project root for more information.

Theme Styles For Windows Presentation Foundation Version $version
==================================================================-->
END

# output ResourceDictionary open tag to both files

$openTag = <<'END';

<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:theme="clr-namespace:Microsoft.Windows.Themes"
                    xmlns:ui="clr-namespace:System.Windows.Documents;assembly=PresentationUI"
                    xmlns:framework="clr-namespace:MS.Internal;assembly=PresentationFramework"
                    xmlns:base="clr-namespace:System.Windows;assembly=WindowsBase">
END

print OUTFILE $openTag;
print UNOPTFILE $openTag;


#indicates a resource has been written to the output file
%resourcesWritten = ();

# list of unused resources
@unusedKeys = ();

# loop while there are still styles unwritten to the output file
do
{
    my $wroteStyle = 0;
    for (my $i = 0; $i < @styles;)
    {
        # flag to indicate there are not unwritten dependencies for this style
        my $canWrite = 1;

        my $stylePtr = $styles[$i];
        my @style = @$stylePtr;

        my $unoptimizedStylePtr = $unoptimizedStyles[$i];
        my @unoptimizedStyle = @$unoptimizedStylePtr;

        my $definedPtr = $resourcesDefined[$i];

        my %defined = %$definedPtr;

        my $referencedPtr = $resourcesReferenced[$i];
        my %referenced = %$referencedPtr;

        foreach $reference (keys %referenced)
        {
            if (!exists $resourcesWritten{$reference})
            {
                $canWrite = 0;
            }
        }

        # if resources this file depends on haven't been written yet, try next style
        if (!$canWrite)
        {
            $i++;
            next;
        }

        # flag all resources defined by this file as written
        foreach $definition (keys %defined)
        {
            $resourcesWritten{$definition} = 1;

            # see if the key was not used
            if (!$usedKeys{$definition})
            {
                push(@unusedKeys, $definition);
            }
        }

        # print the contents of the file
        print OUTFILE @style;
        print UNOPTFILE @unoptimizedStyle;

        # remove the style from the list
        splice @styles,$i,1;
        splice @unoptimizedStyles,$i,1;
        splice @resourcesDefined,$i,1;
        splice @resourcesReferenced,$i,1;

        $wroteStyle = 1;
    }

    # check to see that we actually wrote a style in the previous loop
    if (!$wroteStyle)
    {
        for (my $i = 0; $i < @styles;$i++)
        {
            my $referencedPtr = $resourcesReferenced[$i];
            my %referenced = %$referencedPtr;

            foreach $key (keys %referenced)
            {
                print "Cannot resolve resource: $key\n";
            }
        }

        die "Unable to resolve dependencies between references";
    }

} while(@styles);


# output ResourceDictionary close tag

print OUTFILE "\n</ResourceDictionary>\n";
print UNOPTFILE "\n</ResourceDictionary>\n";

close(OUTFILE);
close(UNOPTFILE);

# See if there were unused keys
if (@unusedKeys > 0)
{
    foreach $unusedKey (@unusedKeys)
    {
        print "Unused resource found with key $unusedKey\n";
    }
    #die "There are unused resources in the theme dictionary";
}



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
        print "ThemeGenerator : warning generating $themeName.xaml\n";
        $message = "warning : ThemeGenerator :";
    }
    else
    {
        print "ThemeGenerator : error generating $themeName.xaml\n";
        $message = "ThemeGenerator.pl : error";
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
        print "$message Running: tf edit $comparisonFile\n";
        print "$message ".`tf edit $comparisonFile`."\n";
        print "$message\n";
        print "$message Running: copy /y $outputFile $comparisonFile\n";
        print "$message".`copy /y $outputFile $comparisonFile`."\n";
    }
    else
    {
        print "$message Theme file needs updating\n";
        print "$message Run the following commands to replace theme files\n";
        print "$message\n";
        print "$message      tf edit $comparisonFile\n";
        print "$message      copy $outputFile $comparisonFile\n";
        print "$message\n";
        print "$message Or to automatically checkout and update:\n";
        print "$message      set THEMEXAML_AUTOUPDATE=1\n";
        print "$message\n";
        die "theme file needs updating";
    }
}

