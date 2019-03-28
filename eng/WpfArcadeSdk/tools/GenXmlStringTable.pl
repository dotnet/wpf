########################################################
#    Generate the XmlStringTable handling class.
#    This tool generates enums for xml strings that are
#    used to index into the xml string table
#    The entries in the xml string table:
#    Index   Name String    Value Type
#    This tool also generates XML Name table
########################################################
use File::Copy;
use Getopt::Std;

########################################################
#    usage: Print out usage
########################################################

sub usage()
{
    print  "Usage: $0 -n [Namespacefile] -x [XmlStringfile] -e [fullenumclassname] -c [fullxmlstringclassname] -o [output.cs]\n\n";
    print  "Example: perl $0 -n XmlNamespaceStringTable.txt -x xmlStringTable.txt -e XmlNamespaceId -c MS.Internal.IO.Packaging.XmlStringTable\n";
}

########################################################
#    Variables
########################################################

#hash to store all the arguements.
my %args
;
#error header.
my $error_msg = "GenXmlStringTable.pl: error:";


##########################################################
#    get the switches values do some minimum validataion
##########################################################

getopts('c:e:n:o:x:', \%args);

#all these switches are required.
if (!$args{c} || !$args{e} || !$args{n} || !$args{o} | !$args{x})
{
	usage();
	exit;
}

my $outfile = $args{o};
my $fullxmlnsenumclassname = $args{e};
my $fullxmlstrclassname = $args{c};
my $xmlnsfile = $args{n};
my $xmlstrfile = $args{x};

########################################################
#    Print out switch value information
########################################################

print "\n";
print "Xml namespaces file:              $xmlnsfile\n";
print "Xml strings file:                 $xmlstrfile\n";
print "Output .cs file:                  $outfile\n";
print "Enum class name for all strings:  $fullxmlnsenumclassname\n";
print "Xml string table class name:      $fullxmlstrclassname\n";


########################################################
#    Parse class names and their namespaces
########################################################

#extract the namespace and enum class name
my ($enumnamespace, $enumsrclass) = ($fullxmlnsenumclassname =~ /^(.+)\.(.+)$/);
my $srid = $srclass."ID";

#extract the namespace and table class name
my ($tablenamespace, $tablesrclass) = ($fullxmlstrclassname =~ /^(.+)\.(.+)$/);
my $srid = $srclass."ID";

########################################################
#    Print out class information
########################################################

print "enum namespace:   $enumnamespace\n";
print "enum class name:  $enumsrclass\n\n";

print "xml string table namespace:  $tablenamespace\n";
print "xml string class name:       $tablesrclass\n";

########################################################
#    Count all xml namespaces and xml strings
########################################################

# Need to start from 1 since the first item is NotDefined
my $tableSize = 1;

open(IN, $xmlnsfile) or die "$error_msg Cannot open $infile. $!\n";

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=/o)
    {
        $tableSize += 1;
    }
}

#### close files
close (IN) or die "$error_msg Cannot close $infile. $!\n";


open(IN, $xmlstrfile) or die "$error_msg Cannot open $infile. $!\n";

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=/o)
    {
        $tableSize += 1;
    }
}

#### close files
close (IN) or die "$error_msg Cannot close $infile. $!\n";


########################################################
#    Header of the generated cs file
#    1. Copyright information
#    2. Begining of the enum class
########################################################

my $header  = <<"END_OF_HEADER";
//-------------------------------------------------------------------------------
// <copyright from='1999' to='2005' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
// This file is generated from $xmlnsfile and $xmlstrfile by GenXmlStringTable.pl
//           - do not modify this file directly
//-------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using $enumnamespace;

namespace $enumnamespace
{

    //an enums for xml string identifiers.
    internal enum $enumsrclass : int
    {
        NotDefined = 0,
END_OF_HEADER


########################################################
#    Middle part of the generated cs file
#    1. Closing brakets for the enum class and the namespace
#    2. Begining of the enum class
#    3. Begining of the string table class
########################################################

my $tableclassheader = <<"END_OF_TABLE_CLASS_HEADER";
    }   // end of enum $enumsrclass
}   // end of namespace

namespace $tablenamespace
{
    internal static class $tablesrclass
    {
        static $tablesrclass()
        {
            Object str;

END_OF_TABLE_CLASS_HEADER

########################################################
#    Footer of the generated cs file
#    1. Helper functions for the xml string table
#    2. Definition of the item entry in the xml string table
#    3. Private member variables
#    4. Closing brakets for the xml string class and the namespace
########################################################

my $footer = <<"END_OF_FOOTER";
        }

        internal static $enumsrclass GetEnumOf(Object xmlString)
        {
            Debug.Assert(xmlString is String);

			// Index 0 is reserved for NotDefined and doesn't have a table entry
			//	so start from 1
            for (int i = 1; i < _xmlstringtable.GetLength(0) ; ++i)
            {
                if (Object.ReferenceEquals(_xmlstringtable[i].Name, xmlString))
                {
                    return ((PackageXmlEnum) i);
                }
            }

            return PackageXmlEnum.NotDefined;
        }

        internal static string GetXmlString($enumsrclass id)
        {
			CheckIdRange(id);

            return (string) _xmlstringtable[(int) id].Name;
        }

        internal static Object GetXmlStringAsObject($enumsrclass id)
        {
			CheckIdRange(id);

            return _xmlstringtable[(int) id].Name;
        }

        internal static $enumsrclass GetXmlNamespace($enumsrclass id)
        {
			CheckIdRange(id);

            return _xmlstringtable[(int) id].Namespace;
        }

        internal static string GetValueType($enumsrclass id)
        {
			CheckIdRange(id);

            return _xmlstringtable[(int) id].ValueType;
        }

        internal static NameTable NameTable
        {
            get
            {
                return _nameTable;
            }
        }

#if false
        internal static IEqualityComparer EqualityComparer
        {
            get
            {
                return _referenceComparer;
            }
        }
#endif

		private static void CheckIdRange($enumsrclass id)
		{
			// Index 0 is reserved for NotDefined and doesn't have a table entry
        	if ((int) id <= 0 || (int) id >= $tableSize)
        	{
        		throw new ArgumentOutOfRangeException("id");
        	}
        }

        internal static NameTable CloneNameTable()
        {
            NameTable nameTable = new NameTable();

            // Index 0 is reserved for NotDefined and doesn't have a table entry
            for (int i=1; i<$tableSize; ++i)
            {
                nameTable.Add((string)_xmlstringtable[i].Name);
            }

            return nameTable;
        }

        private struct XmlStringTableStruct
        {
            private Object _nameString;
            private $enumsrclass _namespace;
            private string _valueType;

            internal XmlStringTableStruct(Object nameString, $enumsrclass ns, string valueType)
            {
                _nameString = nameString;
                _namespace = ns;
                _valueType = valueType;
            }

            internal Object Name { get { return (String) _nameString; } }
            internal $enumsrclass Namespace { get { return _namespace; } }
            internal string ValueType { get { return _valueType; } }
        }

#if false
        // The Hashtable comparer that takes advantage of the fact
        // that we know the object identities of all keys to find.
        private class ReferenceComparer : IEqualityComparer
        {
            // Perform reference comparison.
            // Explicit implementation to avoid conflict with object.Equals.
            bool IEqualityComparer.Equals(object x, object y)
            {
                return object.ReferenceEquals(x, y);
            }

            // Hash on object identity.
            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

#endif

        private static XmlStringTableStruct[] _xmlstringtable = new XmlStringTableStruct[$tableSize];
        private static NameTable _nameTable = new NameTable();
#if false
        private static ReferenceComparer _referenceComparer = new ReferenceComparer();
#endif
    }    //endof class $tablesrclass

}   // end of namespace

END_OF_FOOTER


########################################################
#
#    Generate cs file
#
########################################################

#### open output c# file.

open(OUT, '>'.$outfile) or die "$error_msg Cannot create $outfile. $!\n";

########################################################
#    Write out the predefined header
########################################################

print OUT $header or die "$error_msg Cannot write to $outfile. $! \n";

########################################################
#    Read in all xml namespace list and declare them in the enum class
########################################################

open(IN, $xmlnsfile) or die "$error_msg Cannot open $infile. $!\n";

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=/o)
    {
        print OUT "        $1,\n" or die "$error_msg Cannot write to $outfile. $! \n";
    }
}

#### close files
close (IN) or die "$error_msg Cannot close $infile. $!\n";

########################################################
#    Read in all xml strings and declare them in the enum class
########################################################

open(IN, $xmlstrfile) or die "$error_msg Cannot open $infile. $!\n";

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=/o)
    {
        print OUT "        $1,\n" or die "$error_msg Cannot write to $outfile. $! \n";
    }
}

#### close files
close (IN) or die "$error_msg Cannot close $infile. $!\n";


########################################################
#    Write out the end of the enum class and the start of the xml string table
########################################################

print OUT $tableclassheader or die "$error_msg Cannot write to $outfile. $! \n";


########################################################
#    Add all the xml namespaces to the xml string table
########################################################

open(IN, $xmlnsfile) or die "$error_msg Cannot open $infile. $!\n";

#### insert all namespaces and prefixes strings

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=(\S+)/o)
    {
        print OUT "             str = _nameTable.Add(\"$2\");\n" or die "$error_msg Cannot write to $outfile. $! \n";
        print OUT "             _xmlstringtable[(int) $enumsrclass.$1] = new XmlStringTableStruct(str, $enumsrclass.NotDefined, null);\n" or die "$error_msg Cannot write to $outfile. $! \n";
    }
}

print OUT "\n" or die "$error_msg Cannot write to $outfile. $! \n";

########################################################
#    Add all the xml strings to the xml string table
########################################################

open(IN, $xmlstrfile) or die "$error_msg Cannot open $infile. $!\n";

#### insert all xml strings

while ($string = <IN>)
{
    if ($string =~ /^(\w+)=(\S+)\s+(\S+)(?:\s+(\S+))?/o)
    {
        print OUT "             str = _nameTable.Add(\"$2\");\n" or die "$error_msg Cannot write to $outfile. $! \n";
        print OUT "             _xmlstringtable[(int) $enumsrclass.$1] = new XmlStringTableStruct(str, $enumsrclass.$3, \"$4\");\n" or die "$error_msg Cannot write to $outfile. $! \n";
    }
}

########################################################
#    Write out the end of the xml string table
########################################################

print OUT "\n".$footer or die "$error_msg Cannot write to $outfile. $! \n";

#### close files

close (OUT) or die "$error_msg Cannot close $outfile. $!\n";

