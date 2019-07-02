<?xml version="1.0" encoding="ISO-8859-1"?>


<!-- This is an IE centric presentation of the <FeatureVariationReport.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="DrtReport.xsl"?>
-->

<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <html>
  <head>
  	<!-- Use Internet Explorer 7 Standards mode -->
    <meta http-equiv="x-ua-compatible" content="IE=7"/>
    <Style>
      body {font:x-small 'Verdana';margin-right:1.5em}
      table {border-style:double;border-color:#CCCCCC;border-width: 2px;border-collapse: collapse;width: 800px;  table-layout: fixed;}
      thead {padding-left:3px;background-color:#C0504D;color:white;width:120;font-weight:bold;font-size:12px;}

      tbody{font-size: x-small;border-style: solid;border-color: #666699;border-width: 1px;background: #DBE5F1;}
      th {color:white;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #C0504D;width: auto;}

      .summary {color:black;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #EFD3D2;width: auto;}
      pre { white-space: pre-wrap; word-wrap: break-word; max-width:800px; }
    </Style>
  </head>
  <body>
  <h2>DRT Failures Report</h2>
    Pass Rate for this run was: <xsl:value-of select="Variations/@PassRate"/>. Details available on <a href="Summary.xml">summary report</a>.
  <table border="1">
    <Col Width="35%"/>
    <Col Width="35%"/>
    <Col Width="25%"/>
    <Col Width="80px"/>
    <Col Width="60px"/>
    <Col Width="70px"/>

    <tr bgcolor="#9acd32">
      <th>Area</th>
      <th>Test Name</th>
      <th>Variation</th>
      <th>Duration (S)</th>
      <th>Result</th>
      <th>Log Files</th>
    </tr>




    <xsl:for-each select="Variations/Variation">
    <xsl:if test="@Variation='Test Level Summary'">

    <tr class="summary">
      <td><xsl:value-of select="@Area"/></td>
      <td><xsl:value-of select="@TestName"/></td>
      <td><xsl:value-of select="@Variation"/></td>
      <td><xsl:value-of select="@Duration"/></td>
      <td><xsl:value-of select="@Result"/></td>
      <td><xsl:if test="@LogDir!=''"><a href="{@LogDir}" >Link</a></xsl:if></td>
    </tr>

    <xsl:if test="@Log!=''">
    <tr class="summary">
      <td Colspan="6"><b>Log: </b><pre><xsl:value-of select="@Log"/></pre></td> 
    </tr>
    </xsl:if>

    </xsl:if>

    <xsl:if test="@Variation!='Test Level Summary'">
    <tr>
      <td><xsl:value-of select="@Area"/></td>
      <td><xsl:value-of select="@TestName"/></td>
      <td><xsl:value-of select="@Variation"/></td>
      <td><xsl:value-of select="@Duration"/></td>
      <td><xsl:value-of select="@Result"/></td>
      <td><xsl:if test="@LogDir!=''"><a href="{@LogDir}" >Link</a></xsl:if></td>
    </tr>

    <xsl:if test="@Log!=''">
    <tr >
      <td Colspan="6"><b>Log: </b><pre><xsl:value-of select="@Log"/></pre></td> 
    </tr>
    </xsl:if>

    </xsl:if>    
    </xsl:for-each>
  </table>
  </body>
  </html>
</xsl:template>

</xsl:stylesheet>