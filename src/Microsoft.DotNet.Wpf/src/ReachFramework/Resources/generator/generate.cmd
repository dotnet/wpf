@ECHO OFF
REM Compile Schema preprocessor
%SDXROOT%\tools\managed\v2.0\csc /nologo /debug+ /reference:system.xml.dll /out:%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.exe %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.cs

REM Compile Schema resource generator
%SDXROOT%\tools\managed\v2.0\csc /nologo /debug+ /reference:system.xml.dll /out:%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.cs

REM check out generated resources file
sd edit %SDXROOT%\windows\wcp\print\reach\resources\generated\*
sd edit %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\s0schema.xsd
sd edit %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\OPC_DigSig.xsd
sd edit %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\ContentTypes.xsd
sd edit %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\*
sd edit %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\*

REM run preprocessor for pattern replacement
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.exe %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\s0schema.xsd.src %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\s0schema.xsd
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.exe %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\OPC_DigSig.xsd.src %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\OPC_DigSig.xsd
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.exe %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\ContentTypes.xsd.src %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\ContentTypes.xsd

REM run generator
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_S0.resources DEFATTR GuidelinesX DEFATTR GuidelinesY DEFATTR SnapsToDevicePixels rdkey.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\rdkey.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_rdkey.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__rdkey.xsd s0schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\s0schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_s0schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__s0schema.xsd
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_ContentTypes.resources ContentTypes.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\ContentTypes.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_ContentTypes.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__ContentTypes.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_CoreProperties.resources CoreProperties.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\CoreProperties.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_CoreProperties.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__CoreProperties.xsd dc.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\dc.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Publish\_dc.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Validation\__dc.xsd dcterms.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\dcterms.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Publish\_dcterms.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Validation\__dcterms.xsd dcmitype.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\dcmitype.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Publish\_dcmitype.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\Validation\__dcmitype.xsd
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_DiscardControl.resources DiscardControl.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\DiscardControl.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_DiscardControl.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__DiscardControl.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_DocStructure.resources DocStructure.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\DocStructure.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_DocStructure.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__DocStructure.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_Relationships.resources Relationships.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\Relationships.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_Relationships.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__Relationships.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_SignatureDefinitions.resources SignatureDefinitions.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\SignatureDefinitions.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_SignatureDefinitions.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__SignatureDefinitions.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_Versioning.resources Versioning.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\Versioning.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_Versioning.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__Versioning.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_OPC_DigSig.resources OPC_DigSig.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\OPC_DigSig.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_OPC_DigSig.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__OPC_DigSig.xsd 
%SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe %SDXROOT%\windows\wcp\print\reach\resources\generated\Schemas_xmldsig-core-schema.resources xmldsig-core-schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\xmldsig-core-schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_xmldsig-core-schema.xsd %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\__xmldsig-core-schema.xsd 

if %1.==-p. goto publish
goto nopublish
:publish

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\s0schema.xsd "\\metroportal\working group\Markup Schema\Master\s0schema.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_s0schema.xsd "\\metroportal\working group\Markup Schema\BookFragments\_s0schema.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\rdkey.xsd "\\metroportal\working group\Markup Schema\Master\rdkey.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_rdkey.xsd "\\metroportal\working group\Markup Schema\BookFragments\_rdkey.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\ContentTypes.xsd "\\metroportal\working group\Content Types Schema\Master\ContentTypes.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_ContentTypes.xsd "\\metroportal\working group\Content Types Schema\BookFragments\_ContentTypes.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\CoreProperties.xsd "\\metroportal\working group\CoreProps Schema\Master\CoreProperties.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_CoreProperties.xsd "\\metroportal\working group\CoreProps Schema\BookFragments\_CoreProperties.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\DiscardControl.xsd "\\metroportal\working group\DiscardControl Schema\Master\DiscardControl.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_DiscardControl.xsd "\\metroportal\working group\DiscardControl Schema\BookFragments\_DiscardControl.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\DocStructure.xsd "\\metroportal\working group\DocStruc Schema\Master\DocStructure.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_DocStructure.xsd "\\metroportal\working group\DocStruc Schema\BookFragments\_DocStructure.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\relationships.xsd "\\metroportal\working group\Relationships Schema\Master\relationships.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_relationships.xsd "\\metroportal\working group\Relationships Schema\BookFragments\_relationships.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\SignatureDefinitions.xsd "\\metroportal\working group\Signature Definitions Schema\Master\SignatureDefinitions.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_SignatureDefinitions.xsd "\\metroportal\working group\Signature Definitions Schema\BookFragments\_SignatureDefinitions.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\versioning.xsd "\\metroportal\working group\VE Schema\Master\versioning.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_versioning.xsd "\\metroportal\working group\VE Schema\BookFragments\_versioning.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\OPC_DigSig.xsd "\\metroportal\working group\OPC DigSig Schema\Master\OPC_DigSig.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_OPC_DigSig.xsd "\\metroportal\working group\OPC DigSig Schema\BookFragments\_OPC_DigSig.xsd"

copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\master\xmldsig-core-schema.xsd "\\metroportal\working group\XML DigSig Schema\Master\xmldsig-core-schema.xsd"
copy %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\_xmldsig-core-schema.xsd "\\metroportal\working group\XML DigSig Schema\BookFragments\_xmldsig-core-schema.xsd"


:nopublish

REM revert unchanged generated resources file
sd revert -a %SDXROOT%\windows\wcp\print\reach\resources\generated\*
sd revert -a %SDXROOT%\windows\wcp\print\reach\resources\schemas\Master\*
sd revert -a %SDXROOT%\windows\wcp\print\reach\resources\schemas\publish\*
sd revert -a %SDXROOT%\windows\wcp\print\reach\resources\schemas\validation\*

REM remove executable
del %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.exe
del %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaPP.pdb
del %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.exe
del %SDXROOT%\windows\wcp\print\reach\resources\generator\SchemaGen.pdb



