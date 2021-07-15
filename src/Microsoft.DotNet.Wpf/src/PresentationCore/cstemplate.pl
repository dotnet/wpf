#------------------------------------------------------------------------------
#
# Description: This is a horrible script to instantiate csharp template files.
#              (Just learned Perl.)
#              We use this till the URT has generics build in.
#------------------------------------------------------------------------------

if (@ARGV < 1)
{
    print "Usage: cstemplate.pl <template name>";
    exit();
}

$templateName      = $ARGV[0];
$headerFile = $templateName . ".csh";
$srcFile = $templateName . ".cst";
$instantiations = $templateName . ".csi";

print "//------------------------------------------------------------------------------\n";
print "//  \n";
print "//  File:       $templateName.cs\n";
print "//  \n";
print "//  Auto-generated file: Generated out of $templateName.{csh, cst, csi}\n";
print "//------------------------------------------------------------------------------\n";
print "\n";
print "\n";

# Output the header file.

open(FHANDLE3, $headerFile) || die "Can not open header file: $headerFile";
while (<FHANDLE3>)
{
   print $_;
}
close(FHANDLE3);

print "\n";
print "\n";

# Instantiate the template.

open(FHANDLE2, $instantiations) || die "Can not open instantiation file: $instantiations";

NEXT:

while (1)
{
    $x = <FHANDLE2>;

    if ($x =~ m/::BEGIN_TEMPLATE/)
    {
        @searchString = {};
        @replaceString = {};
    }
    elsif ($x =~ m/::END_TEMPLATE/)
    {
        goto REPLACE;
    }
    elsif ($x =~ m/::END/)
    {
        goto END;
    }
    elsif ($x =~ m/(.*):(.*)/)
    {
        $searchString[$#searchString++] = "<<$1>>";
        $replaceString[$#replaceString++] = "$2";
    }
}


REPLACE:

open(FHANDLE, $srcFile) || die "Can not open file.";
while (<FHANDLE>)
{
    for ($i=0; $i<$#searchString; $i++)
    {
        $_ =~ s/$searchString[$i]/$replaceString[$i]/g;
    }
    print $_;
}
close(FHANDLE);

print "\n";
print "\n";

goto NEXT;

END:

close(FHANDLE2);
