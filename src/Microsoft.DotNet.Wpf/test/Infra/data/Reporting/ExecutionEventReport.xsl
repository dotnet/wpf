<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- This is an IE centric presentation of the Summary.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="ExecutionEventReport.xsl"?>
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
      table {border-style:double;border-color:#CCCCCC;border-width: 2px;border-collapse: collapse;width: 100%;}
      thead {padding-left:3px;background-color:#C0504D;color:white;width:120;font-weight:bold;font-size:12px;}

      tbody {font-size: x-small;border-style: solid;border-color: #666699;border-width: 1px;background: #DBE5F1;}
      th {color:white;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #C0504D;width: auto;}

      tfoot {color:black;font-size: x-small;font-weight:bold;border-style: solid;border-color: #cccccc;border-width: 1px;background: #EFD3D2;width: auto;}
    </Style>
  </head>
  <body>
  <h2>Execution Event Report</h2>
  <table border="1">
    <tr bgcolor="#9acd32">
      <th>Source</th>
      <th>Time</th>
      <th>Event Type</th>
      <th>Message</th>
    </tr>
    <xsl:for-each select="EventLog/Event">
    <tr>
      <td><xsl:value-of select="@Source"/></td>
      <td><xsl:value-of select="@Time"/></td>
      <td><xsl:value-of select="@EventType"/></td>
      <td><xsl:value-of select="@Message"/></td>
    </tr>
    </xsl:for-each>
  </table>
  </body>
  </html>
</xsl:template>

</xsl:stylesheet>