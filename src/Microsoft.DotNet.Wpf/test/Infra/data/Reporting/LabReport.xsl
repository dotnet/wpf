<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- This is an IE centric presentation of the Summary.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="LabReport.xsl"?>
-->

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <html>
  <Head>
	<!-- Use Internet Explorer 7 Standards mode -->
    <meta http-equiv="x-ua-compatible" content="IE=7"/>
    <Title>Test Results Mail - <xsl:value-of select="Configuration/@RunID"/></Title>
      <STYLE>
        body { font-family:Verdana,Tahoma,arial;font-size:smaller; }
        TH { color:white; }
        .subrow1 { background-color:#DCE8ED; }
        .thead {padding-left:3px;background-color:#C0504D;color:white;width:120;font-weight:bold;font-size:12px;}
        .tdata {padding-left:3px;background-color:#B8CCE4;}
        .goodpassrate {padding-left:3px;color:green;font-weight:bold;}
      </STYLE>
  </Head>
  <body>

  <xsl:call-template name="Subject" />
  <xsl:call-template name="AreaSummary" />
  <xsl:call-template name="Notes" />
  <xsl:call-template name="Configuration" />
  <xsl:call-template name="Paths" />


  </body>
  </html>
</xsl:template>

<xsl:template name="Subject">
    <h2>
      <xsl:value-of select="LabReport/Configurations/KeyValuePair[@Key='Run Type']/@Value"/>
      - (Job #<xsl:value-of select="LabReport/Configurations/KeyValuePair[@Key='Run ID']/@Value"/>)
      - <xsl:value-of select="LabReport/Summary/AreaSummary[@AreaName='Total']/@AdjustedPassRate"/>% 
      - <xsl:value-of select="normalize-space(LabReport/Configurations/KeyValuePair[@Key='Name']/@Value)"/>
    </h2>
</xsl:template>


<xsl:template name="AreaSummary">
  <xsl:variable name="ReportPath">
     <xsl:value-of select="LabReport/@ReportPath"/>
  </xsl:variable>

  <h2>Test Results Summary</h2>
  <table cellpadding="1" style="table-layout:fixed;font-size:14px;margin-top:-15px;">
    <tr BGCOLOR="#C0504D">
      <th align="left" style="padding-left:4px;" width="120">Area</th>
      <th width="80">Failed (need analysis)</th>
      <th width="80">Failed (with BugIDs)</th>
      <th width="70">Ignored</th>
      <th width="60">Total</th>
      <th  width="80">PassRate</th>
      <th  width="80">RunTime</th>
      <th  width="80">TotalTime</th>
    </tr>

    <xsl:for-each select="LabReport/Summary/AreaSummary[@AreaName='Total']">

    <tr bgcolor="#EFD3D2">
      <td ALIGN="left">Total</td>
      <td ALIGN="right"><xsl:value-of select="number(@FailingVariations)-number(@FailedVariationsWithBugs)"/></td>
      <td ALIGN="right"><xsl:value-of select="@FailedVariationsWithBugs"/></td>
      <td ALIGN="right"><xsl:value-of select="@IgnoredVariations"/></td>
      <td ALIGN="right"><xsl:value-of select="@TotalVariations"/></td>
      <td ALIGN="right"><xsl:value-of select="@AdjustedPassRate"/></td>
      <td ALIGN="center"><xsl:value-of select="@TestExecutionTime"/></td>
      <td ALIGN="center"><xsl:value-of select="@TotalExecutionTime"/></td>
    </tr>

    </xsl:for-each>

    <xsl:for-each select="LabReport/Summary/AreaSummary[@AreaName!='Total']">

    <tr>
      <xsl:if test="position() mod 2 = 0">
        <xsl:attribute name="bgcolor">lightgrey</xsl:attribute>
      </xsl:if>
      <td ALIGN="left"><a href="{$ReportPath}\AreaReports\{@AreaName}VariationReport.xml" ><xsl:value-of select="@AreaName"/></a></td>
      <td ALIGN="right"><xsl:value-of select="number(@FailingVariations)-number(@FailedVariationsWithBugs)"/></td>
      <td ALIGN="right"><xsl:value-of select="@FailedVariationsWithBugs"/></td>
      <td ALIGN="right"><xsl:value-of select="@IgnoredVariations"/></td>
      <td ALIGN="right"><xsl:value-of select="@TotalVariations"/></td>
      <td ALIGN="right">
        <xsl:variable name="PassRateScore" select="@AdjustedPassRate" />
        <xsl:if test="number($PassRateScore) > 99.5">
          <xsl:attribute name="class">goodpassrate</xsl:attribute>
        </xsl:if>
        <xsl:value-of select="@AdjustedPassRate"/>
      </td>
      <td ALIGN="center"><xsl:value-of select="@TestExecutionTime"/></td>
      <td ALIGN="center"><xsl:value-of select="@TotalExecutionTime"/></td>
    </tr>

    </xsl:for-each>

  </table>
</xsl:template>


<xsl:template name="Configuration">
<h3>Run Configuration:</h3>
  <table cellpadding="1" style="table-layout:fixed;font-size:14px;padding-top:0px;margin-top:-12px;">
    <xsl:for-each select="LabReport/Configurations/KeyValuePair">
    <tr>
      <td class="thead" width="150"><xsl:value-of select="@Key"/></td>
      <td class="tdata" width="300"><xsl:value-of select="@Value"/></td>
    </tr>  
    </xsl:for-each>
  </table>
</xsl:template>

<xsl:template name="Notes">
<h3>Notes:</h3>
  <table>
  
  </table>
</xsl:template>


<xsl:template name="Paths">
<h3>Paths:</h3>
  <table cellpadding="1" style="table-layout:fixed;font-size:14px;padding-top:0px;margin-top:-12px;">
    <xsl:for-each select="LabReport/Paths/KeyValuePair">
    <tr>
      <td class="thead" width="150"><xsl:value-of select="@Key"/></td>
      <td class="tdata" width="700"><a href="{@Value}"><xsl:value-of select="@Value"/></a></td>
    </tr>  
    </xsl:for-each>
  </table>
</xsl:template>



</xsl:stylesheet>