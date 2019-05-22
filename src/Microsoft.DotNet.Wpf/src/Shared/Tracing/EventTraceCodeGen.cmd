@echo off
::
:: Updates the managed and unmanaged sources generated from wpf-etw.man
::

if /i "%1"=="/native" goto :native
if /i "%1"=="/managed" goto :managed
if /i "%1"=="/install" goto :install
if /i "%1"=="/evxml" goto :evxml
if /i "%1"=="/unmof" goto :unmof
if /i "%1"=="/updateversion" goto :fixver
if /i "%1"=="/fast" goto :fast
if /i "%1"=="/?" goto :usage
call :build_mcwpf
call :native
call :managed
call :evxml
goto :eof

:usage
echo.Updates the managed and unmanaged sources generated from wpf-etw.man
echo.EventTraceCodeGen [/native ^| /managed]
goto :eof

:fast
call :native
call :managed
goto:eof

:: Install the manifest
:install
    wevtutil im wpf-etw.man
    copy %_nttree%\wpf\wpf-etw.dll %systemroot%
goto :eof

:: Generate Native code
:native
    setlocal
    for %%f in (native\wpf-etw.h native\wpf-etw.mof resources\wpf-etw.rc resources\MSG00001.bin resources\wpf-etwTEMP.bin) do call :checkout %%f
    set /p foo="Generating native code..."<nul
    del native\wpf-etw.h
    mc.exe -um -mof -r resources -h native wpf-etw.man
    :: mc.exe always returns 0 so check success with the output file
    if exist native\wpf-etw.h echo SUCCESS
    if not exist native\wpf-etw.h echo FAILED
goto :eof

:: Generate managed code
:managed
    set mcwpf=%_nttree%\wpf\tools\build\mcwpf.exe
    if not exist %mcwpf% (
        echo.    Error: mcwpf.exe build failed.
        goto :eof
    )
    for /F "tokens=5" %%f in ('filever %mcwpf%') do set mcwpfver=%%f
    if not exist managed mkdir managed
    if exist managed\wpf-etw.cs call :checkout managed\wpf-etw.cs
    if exist managed\wpf-etw.cs call :checkout managed\wpf-etw-xaml.cs
    setlocal
    set COMPLUS_INSTALLROOT=
    set COMPLUS_VERSION=
    set /p foo="Generating managed code..."<nul
    %mcwpf% -man wpf-etw.man -template mcwpf\wpf_template.cs -out managed\wpf-etw.cs
    %mcwpf% -man wpf-etw.man -template mcwpf\wpf_template.cs -out managed\wpf-etw-xaml.cs -keyword KeywordXamlBaml
    if errorlevel 0 echo SUCCESS
    if not errorlevel 0 echo FAILED  (you may need to re-build mcwpf)
goto :eof

:: Build the managed code gen tool
:build_mcwpf
    echo Building managed manifest code gen tool...
    msbuild %sdxroot%\wpf\src\Shared\Tracing\mcwpf\mcwpf.csproj
goto :eof

:evxml
    if exist wpf-etw.evxml call :checkout wpf-etw.evxml
    perl -x -w %~f0 GenerateEvXmlManifest -i wpf-etw.man -o wpf-etw.evxml
goto :eof

:unmof
    perl -x -w %~f0 GenerateMofUninstallationManifest -i wpf-etw.man -o wpf-etw.mof.uninstall
goto :eof

:checkout
    call :isreadonly %1
    if %read_only%==1 (
        echo Checking out %1...
        call tf edit %1
    )
goto :eof

:isreadonly
set read_only=0
for /F "tokens=1,2,3 delims= " %%a in ('attrib %1') do (
    if "%%a"=="R" set read_only=1
    if "%%b"=="R" set read_only=1
    if "%%c"=="R" set read_only=1
)
goto :eof

:fixver
    perl -x -w %~f0 UpdateInstallPath %*
goto :eof

#!perl
#line 96
use strict;
use XML::DOM;
use Getopt::Long;

my %dispatch = (
    "GenerateEvXmlManifest" => \&GenerateEvXmlManifest,
    "GenerateMofUninstallationManifest" => \&GenerateMofUninstallationManifest,
    "UpdateInstallPath" => \&UpdateInstallPath,
);

my $sub = $dispatch{$ARGV[0]};
if ($sub)
{
    $sub->();
}
else
{
    print "Unknown Export.";
}

##########################################################
# subroutine to generate an evxml from the crimson manifest
##########################################################
sub GenerateEvXmlManifest
{
    my $sClrEtwAllMan = "";
    my $sClrEtwAllEvXml = "";
    GetOptions('i=s' => \$sClrEtwAllMan, 'o=s' => \$sClrEtwAllEvXml);
    print "Generating "; 
    print $sClrEtwAllEvXml;
    print " from ";
    print $sClrEtwAllMan;
    print "\n";
    
    # get a file handle to the evxml manifest
    open(fhClrEtwAllEvXml, ">$sClrEtwAllEvXml") || die "Cannot open $sClrEtwAllEvXml\n";

    # following lines would be common to all evxml's
    # and would be at the very start
    print fhClrEtwAllEvXml "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
    print fhClrEtwAllEvXml "<!--\n\n";
    print fhClrEtwAllEvXml "** GENERATED FILE **\n";
    print fhClrEtwAllEvXml "Modifications to this file will be lost.  Edit wpf-etw.man and run EventTraceCodeGen.cmd to update this file.\n\n";
    print fhClrEtwAllEvXml "This file is a manifest for standalone use with XPerfInfo for describing ETW events on Windows XP.\n\n";
    print fhClrEtwAllEvXml "-->\n";
    print fhClrEtwAllEvXml "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\">\n";
    print fhClrEtwAllEvXml "<instrumentation xmlns=\"urn:schemas-microsoft-com:asm.v1\">\n";
    print fhClrEtwAllEvXml "  <events>\n";
    print fhClrEtwAllEvXml "      <eventSubTypes/>\n";    

    # get the following information from the crimson manifest and generate the evxml
    # 1. Event Guid
    # 2. Event Version
    # 3. Event Name (Task Name::Event Name)
    # 4. Event Type (Event Opcode) >> Need to create a hash table of opcodes within this provider
    # 5. Event Template

    my $rParser = new XML::DOM::Parser;
    my $rClrEtwAllMan = $rParser->parsefile($sClrEtwAllMan);
    my $noOfProviders = $rClrEtwAllMan->getElementsByTagName("provider")->getLength;

    ###########################################
    # Mapping from crimson types to evxml types
    ###########################################
    my %hWinType =  
    (
        "win:UInt8" => "\%Byte;",
        "win:Int8" => "\%Byte;",
        "win:Int32" => "\%Int32;",
        "win:UInt32" => "\%UInt32;",
        "win:UInt16" => "\%UInt16;",
        "win:UInt64" => "\%UInt64;",
        "win:Int64" => "\%Int64;",
        "win:Double" => "\%Double;",
        "win:Boolean" => "\%Uint32;",
        "win:HexInt64" => "\%UInt64;",
        "win:UnicodeString" => "\%String;",
        "win:AnsiString" => "\%AnsiString;",
        "win:Pointer" => "\%Pointer;",
        "win:GUID" => "\%Guid;",
        "win:Float" => "\%Float;",
    );
    
    # get rid of the private provider nodes
    foreach my $rProvider ($rClrEtwAllMan->getElementsByTagName("provider"))
    {
        # standard opcodes
        my %hOpcode = 
        (
            "win:Info" => 0,
            "win:Start" => 1,
            "win:Stop" => 2,
            "win:DC_Start" => 3,
            "win:DC_Stop" => 4,
            "win:Extension" => 5,
            "win:Reply" => 6,
            "win:Resume" => 7,
            "win:Suspend" => 8,
            "win:Send" => 9,
        );

        # populate the opcodes for this provider        
        # we will just need the opcode value given the opcode name
        foreach my $rOpcode (GetChildNodes($rProvider,'opcode'))
        {
            $hOpcode{$rOpcode->getAttribute('name')} = $rOpcode->getAttribute('value');
        }
        
        my %hTask;
        my %hTaskOpcodes;

        # populate the tasks for this provider
        # we will just need the task guid given the task name
        foreach my $rTaskNode (GetChildNodes($rProvider,'task'))
        {
            my $sTaskName = $rTaskNode->getAttribute('name'); 
            $hTask{$sTaskName} = $rTaskNode->getAttribute('eventGUID');
            
            # populate the opcodes for this task        
            # we will just need the opcode value given the opcode name
            foreach my $rOpcode (GetChildNodes($rTaskNode,'opcode'))
            {
                my $sOpcodeName = $rOpcode->getAttribute('name');
                my $sHashKey = $sTaskName."::".$sOpcodeName;
                $hTaskOpcodes{$sHashKey} = $rOpcode->getAttribute('value');
            }
        }
    
        my %hTemplates;
        # populate the templates for this provider
        # we will need the template field names and template field types given the template id
        foreach my $rTemplateNode (GetChildNodes($rProvider,'template'))
        {
            my $sTid = $rTemplateNode->getAttribute('tid');
            my @aDataNodes = $rTemplateNode->getChildNodes;
            my @aFieldNames = ();
            my @aFieldTypes = ();
            
            my $counter = 0;
            for (my $i = 0; $ i<@aDataNodes; $i++)
            {
                my $rDataNode = $aDataNodes[$i];
                ($rDataNode->getNodeName eq "data") || next;
                my $sType = $rDataNode->getAttribute('inType');
                my $sFieldName = $rDataNode->getAttribute('name');
                if (exists($hWinType{$sType}))
                {
                    push(@aFieldNames, $sFieldName);
                    push(@aFieldTypes, $hWinType{$sType});
                }
                else
                {
                    die "This script currently does not support the crimson type: $sType.\nPlease add the mapping type in \$hWinType\n";
                }
                $counter = $counter + 1;
            }

            $hTemplates{$sTid} = {'TemplateFieldNames' => \@aFieldNames, 'TemplateFieldTypes' => \@aFieldTypes, 'TemplateFieldCount' =>  $counter};
        }
        
        my %hEventVersions;
        # get the different versions under this provider
        foreach my $rEventNodeForVersion (GetChildNodes($rProvider,'event'))
        {
            my $sVersion = $rEventNodeForVersion->getAttribute('version');
            $hEventVersions{$sVersion} = $sVersion;
        }
        
        # lets print the event in the evxml file
        foreach my $key (keys(%hEventVersions))
        {
            foreach my $sTaskName (keys(%hTask))
            {
                my $nEventCount = 0;

                foreach my $rEventNode (GetChildNodes($rProvider,'event'))
                {
                    my $sVersion = $rEventNode->getAttribute('version');
                    my $sTask = $rEventNode->getAttribute('task');
                    my $sEventGuid = $hTask{$sTask};
                    
                    if($key eq $sVersion && $sTask eq $sTaskName)
                    {    
                        my $sOpcodeName = $rEventNode->getAttribute('opcode');
                        my $sOpcode = 0;
                        my $sHashKey = $sTask."::".$sOpcodeName;
                        if(exists $hTaskOpcodes{$sHashKey})
                        {
                            $sOpcode = $hTaskOpcodes{$sHashKey};
                        }
                        else
                        {
                            exists $hOpcode{$sOpcodeName} || die "Opcode $sOpcodeName not defined for this provider\n";                        
                            $sOpcode = $hOpcode{$sOpcodeName};
                        }
                        $sOpcodeName =~ s/win://;
                        my $sSymbol = $rEventNode->getAttribute('symbol'); 
                        my $sTemplate = $rEventNode->getAttribute('template');
                                
                        if($nEventCount eq 0)
                        {
                            # write the comment for the event
                            print fhClrEtwAllEvXml "      <!-- $sTaskName -->\n";
                            # write the event guid
                            print fhClrEtwAllEvXml "      <event guid=\"$hTask{$sTaskName}\">\n";
                            # write the version number
                            print fhClrEtwAllEvXml "          <diagnosticInstance version=\"$key\">\n";
                        }
                        $nEventCount = $nEventCount + 1;
                       
                        my $sTemplateName = $rProvider->getAttribute("name"); 
                        # write the data fields in the event
                        print fhClrEtwAllEvXml "              <!-- $sSymbol -->\n";
                        print fhClrEtwAllEvXml "              <classification subType=\"";
                        print fhClrEtwAllEvXml "/$sTask/$sOpcodeName";
                        print fhClrEtwAllEvXml "\" ";
                        print fhClrEtwAllEvXml "subTypeValue=\"$sOpcode\" />\n";
                        print fhClrEtwAllEvXml "              <template>\n";
                        print fhClrEtwAllEvXml "                  <$sTemplateName>\n";
                        for (my $i = 0; $i<$hTemplates{$sTemplate}->{'TemplateFieldCount'}; $i++)
                        {
                            my $sFieldName = $hTemplates{$sTemplate}->{'TemplateFieldNames'}->[$i];
                            my $sFieldType = $hTemplates{$sTemplate}->{'TemplateFieldTypes'}->[$i];
                            print fhClrEtwAllEvXml "                      <$sFieldName> $sFieldType </$sFieldName>\n";
                        }
                        print fhClrEtwAllEvXml "                  </$sTemplateName>\n";
                        print fhClrEtwAllEvXml "              </template>\n";
                    }
                }

                if($nEventCount > 0)
                {
                    print fhClrEtwAllEvXml "          </diagnosticInstance>\n";
                    print fhClrEtwAllEvXml "      </event>\n";
                    $nEventCount = 0;
                }
            }
        }
    }

    # following lines would be common to all evxml's 
    # and should be at the very end
    print fhClrEtwAllEvXml "  </events>\n";
    print fhClrEtwAllEvXml "</instrumentation>\n";
    print fhClrEtwAllEvXml "</assembly>";

    # close the evxml
    close(fhClrEtwAllEvXml);

    return;
}

########################################################################
# subroutine to generate an uninstallation mof from the crimson manifest
########################################################################
sub GenerateMofUninstallationManifest
{
    my $sClrEtwAllMan = "";
    my $sClrEtwUninstallMof = "";
    GetOptions('i=s' => \$sClrEtwAllMan, 'o=s' => \$sClrEtwUninstallMof);
    print "Generating "; 
    print $sClrEtwUninstallMof;
    print " from ";
    print $sClrEtwAllMan;
    print "\n";

    # get a file handle to the uninstall mof manifest
    open(fhClrEtwUninstallMof, ">$sClrEtwUninstallMof") || die "Cannot open $sClrEtwUninstallMof\n";

    my $rParser = new XML::DOM::Parser;
    my $rClrEtwAllMan = $rParser->parsefile($sClrEtwAllMan);

    print fhClrEtwUninstallMof "//**************************************************\n";
    print fhClrEtwUninstallMof "// *** WPF Event Tracing Uninstallation MOF *** \n";
    print fhClrEtwUninstallMof "//**************************************************\n";
    print fhClrEtwUninstallMof "#pragma autorecover\n";
    print fhClrEtwUninstallMof "#pragma classflags(\"forceupdate\")\n";
    print fhClrEtwUninstallMof "#pragma namespace (\"\\\\\\\\.\\\\root\\\\WMI\")\n";

    # get the provider names
    foreach my $rProviderNode ($rClrEtwAllMan->getElementsByTagName("provider"))
    {
        print fhClrEtwUninstallMof "#pragma deleteclass(\"";
        my $providerName = $rProviderNode->getAttribute('name');
        $providerName =~ s/-/_/g;
        print fhClrEtwUninstallMof $providerName;
        print fhClrEtwUninstallMof "\", NOFAIL)\n";
    }

    # get the task names
    foreach my $rTaskNode ($rClrEtwAllMan->getElementsByTagName("task"))
    {
        print fhClrEtwUninstallMof "#pragma deleteclass(\"";
        my $taskName = $rTaskNode->getAttribute('name');
        $taskName =~ s/-/_/g;
        print fhClrEtwUninstallMof $taskName;
        print fhClrEtwUninstallMof "\", NOFAIL)\n";
    }

    # Close the mof
    close(fhClrEtwUninstallMof);

    return;
}

########################################################################
# subroutine to update the CLR version in the install path
########################################################################
sub UpdateInstallPath
{
    my $sCurrentManifest = "";
    my $sNewManifest = "";
    my $sLookupFile = "";
    GetOptions('i=s' => \$sCurrentManifest, 'o=s' => \$sNewManifest, 'verfile=s' => \$sLookupFile);

    print "Generating "; 
    print $sNewManifest;
    print " from ";
    print $sCurrentManifest;
    print "\n";

    my $sLookupString = "\%CLR_INSTALL_PATH\%";    
    my $sReplaceString = "%WINDIR%\\Microsoft.NET\\Framework";
    
    # get a file handle to the version header
    open(fhVersionHeader, $sLookupFile) || die "Cannot open lookup file: $sLookupFile\n";
    
    # read the contents of the version header
    my @versionText = <fhVersionHeader>;

    # close the version header
    close(fhVersionHeader);
    
    # construct the replace string
    my $sBuildArch = lc($ENV{"_BuildArch"});
    if($sBuildArch eq "amd64" || $sBuildArch eq "ia64")
    {
        $sReplaceString .= "64";
    }
    $sReplaceString .= "\\v";
    
    # construct the version string and append to the replace string
    foreach my $sVersionEntry (@versionText)
    {
        chomp($sVersionEntry);

        if ($sVersionEntry =~ /^#define/)
        {
            (my $unused, my $macroname, my $macrovalue) = split(/ /, $sVersionEntry, 3);
            $macrovalue =~ s/^\s+//;
            if(lc($macroname) eq "rmj" || lc($macroname) eq "rmm" || lc($macroname) eq "rup")
            {
                $sReplaceString .= $macrovalue;
                if(lc($macroname) eq "rmj" || lc($macroname) eq "rmm")
                {
                    $sReplaceString .= "."
                }
            }
        }
    }

    # get a file handle to the current manifest
    open(fhCurrentManifest, "<$sCurrentManifest") || die "Cannot open current manifest: $sCurrentManifest\n";
    
    # read the text from the current manifest
    my @aCurrentText = <fhCurrentManifest>;
    
    # close the current manifest
    close(fhCurrentManifest);

    # replace the current string with the replacement string
    for(my $currentOffset=0; $currentOffset<=$#aCurrentText; $currentOffset++)
    {
        $aCurrentText[$currentOffset] =~ s/$sLookupString/$sReplaceString/ig;
    }
    
    # get a file handle to the new manifest
    open(fhNewManifest, ">$sNewManifest") || die "Cannot open new manifest: $sNewManifest\n";
    
    # put the modified contents to the output file
    for(my $currentOffset=0; $currentOffset<=$#aCurrentText; $currentOffset++)
    {
        print fhNewManifest $aCurrentText[$currentOffset];
    }
    
    # close the new manifest
    close(fhNewManifest);
    
    return;
}

##########################################################
# subroutine to get the child nodes from the parent node
##########################################################
sub GetChildNodes
{
    my ($rRootNode, $sNodeName) = @_;
    my @aRetNodes = GetChildrenByName($rRootNode, $sNodeName.'s');
    @aRetNodes == 1 || return ();
    return GetChildrenByName($aRetNodes[0], $sNodeName);
}

##########################################################
# subroutine to get the child nodes from the parent node using the node name
##########################################################
sub GetChildrenByName
{
    my ($rRootNode, $sKey) = @_;
    my @aRetNodes = ();
    foreach my $rNode ($rRootNode->getChildNodes)
    {
        if ($rNode->getNodeName eq $sKey)
        {
            push (@aRetNodes, $rNode);
        }
    }
    return @aRetNodes;
}


