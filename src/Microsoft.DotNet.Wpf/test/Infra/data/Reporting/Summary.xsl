<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- This is an IE centric presentation of the Summary.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="Summary.xsl"?>
-->

<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <html>
  <Head>
	<!-- Use Internet Explorer 7 Standards mode -->
    <meta http-equiv="x-ua-compatible" content="IE=7"/>
    <Title>Test Results Summary</Title>
    <Style>
      body {font:x-small 'Verdana';margin-right:1.5em}
      table {border-style:double;border-color:#CCCCCC;border-width: 2px;border-collapse: collapse;width: 760px;}
      thead {padding-left:3px;background-color:#C0504D;color:white;width:120;font-weight:bold;font-size:12px;}

      tbody {font-size: x-small;border-style: solid;border-color: #666699;border-width: 1px;background: #DBE5F1;}
      th {color:white;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #C0504D;width: auto;}

      tfoot {color:black;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #EFD3D2;width: auto;}
      .goodpassrate {padding-left:3px;color:green;font-weight:bold}
    </Style>
  </Head>
  <body>
  <h2>Test Results Summary</h2>
  <table>
    <Col Width="120px"/>
    <Col Width="60px"/>
    <Col Width="60px"/>
    <Col Width="60px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <tr>
      <th>Area</th>
      <th>Failed (need analysis)</th>
      <th>Failed (with BugIDs)</th>
      <th>Ignored</th>
      <th>Total</th>
      <th>PassRate</th>
      <th>RunTime</th>
      <th>TotalTime</th>
    </tr>


    <xsl:for-each select="Summary/AreaSummary[@AreaName='Total']">
  
    <tr bgcolor="#EFD3D2">
      <td ALIGN="left">Total</td>
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

    <xsl:for-each select="Summary/AreaSummary[@AreaName!='Total']">

    <tr>
      <xsl:if test="position() mod 2 = 0">
        <xsl:attribute name="bgcolor">lightgrey</xsl:attribute>
      </xsl:if>
      <td ALIGN="left"><a href="AreaReports\{@AreaName}VariationReport.xml" ><xsl:value-of select="@AreaName"/></a></td>
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
  </body>
  </html>
</xsl:template>

</xsl:stylesheet>