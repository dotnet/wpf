#------------------------------------------------------------------------------
# Microsoft Windows Client Platform
#
#
# Description: Allows CLR generic-like type creation without using actual
#              generics. Performance (lack of ngen support was the main
#              motivation behind this approach)
#------------------------------------------------------------------------------

if (@ARGV < 1)
{
    print "Usage: template.pl <template name> [<instance template>]";
    exit();
}

$templateName = $ARGV[0];
$templateHeader = $templateName . ".th";
$templateBody = $templateName . ".tb";
$templateInstances = $templateName . ".ti";

if (@ARGV > 1)
{
    $templateInstances = $ARGV[1] . ".ti";
}

# Output the header file.

open(FHANDLE3, $templateHeader) || die "Can not open header file: $templateHeader";
while (<FHANDLE3>)
{
   print $_;
}
close(FHANDLE3);

print "\n";
print "\n";

# Instantiate the template.

open(FHANDLE2, $templateInstances) || die "Can not open instantiation file: $templateInstances";

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
    elsif ($x =~ m/(.*):([^\r\n]*)/)
    {
        $searchString[$#searchString++] = "<<$1>>";
        $replaceString[$#replaceString++] = "$2";
    }
}


REPLACE:

open(FHANDLE, $templateBody) || die "Can not open file.";
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
