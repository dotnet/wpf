<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- This is an IE centric presentation of the MachineSummary.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="MachineSummary.xsl"?>
-->

<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <html>
  <Head>
	<!-- Use Internet Explorer 7 Standards mode -->
    <meta http-equiv="x-ua-compatible" content="IE=7"/>
    <Title>Test Results Machine Summary</Title>
    <Style>
      body {font:x-small 'Verdana';margin-right:1.5em}
      table {border-style:double;border-color:#CCCCCC;border-width: 2px;border-collapse: collapse;width: 760px;}
      thead {padding-left:3px;background-color:#C0504D;color:white;width:120;font-weight:bold;font-size:12px;}

      tbody {font-size: x-small;border-style: solid;border-color: #666699;border-width: 1px;background: #DBE5F1;}
      th {color:white;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #C0504D;width: auto;}

      tfoot {color:black;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #EFD3D2;width: auto;}
    </Style>
  </Head>
  <body>
  <h2>Test Results Machine Summary</h2>
  <table>
    <Col Width="160px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="80px"/>
    <Col Width="120px"/>
    <tr>
      <th>Machine</th>
      <th>No Variation Tests</th>
      <th>Failing Variations</th>
      <th>Total Variations</th>
      <th>Pass Rate</th>
      <th>Adjusted Pass Rate</th>
      <th>Test Execution Time</th>
    </tr>
    <tbody>

    <xsl:for-each select="Summary/MachineSummary[@MachineName!='[Did not Execute]']">
    <tr>
       <xsl:call-template name="Row" />
    </tr>
    </xsl:for-each>

    <xsl:for-each select="Summary/MachineSummary[@MachineName='[Did not Execute]']">
    <tr bgcolor="red">
       <xsl:call-template name="Row" />
    </tr>
    </xsl:for-each>

    </tbody>

  </table>
  </body>
  </html>
</xsl:template>

<xsl:template name="Row">
      <td><xsl:value-of select="@MachineName"/></td>
      <td><xsl:value-of select="@TestsWithoutVariation"/></td>
      <td><xsl:value-of select="@FailingVariations"/></td>
      <td><xsl:value-of select="@TotalVariations"/></td>
      <td><xsl:value-of select="@PassRate"/></td>
      <td><xsl:value-of select="@AdjustedPassRate"/></td>
      <td><xsl:value-of select="@TestExecutionTime"/></td>
</xsl:template>


</xsl:stylesheet>