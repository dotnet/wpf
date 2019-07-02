<?xml version="1.0" encoding="ISO-8859-1"?>


<!-- This is an IE centric presentation of the <FeatureVariationReport.xml report views 
     provided by QualityVault 

     It can be referenced via this tag:
     <?xml-stylesheet type="text/xsl" href="VariationReport.xsl"?>
-->

<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="/">
    <html>
      <Head>
	    <!-- Use Internet Explorer 7 Standards mode -->
        <meta http-equiv="x-ua-compatible" content="IE=7"/>
        <Style>
          body {font:x-small 'Verdana';margin-right:1.5em}
          table {border-style:double;border-color:#CCCCCC;border-width: 1px;border-collapse: separate; table-layout: auto; width:100%; }

          tbody{font-size: x-small;border-style: solid;border-color: #666699;border-width: 10px;background: #DBE5F1;}
          th {color:white;font-size: x-small;border-style: solid;border-color: #cccccc;border-width: 1px;}
          .log {color:black;font-size: x-small;;border-style: solid;border-color: #cccccc;border-width: 1px;background: #EEEEEE;}
          pre { white-space: pre-wrap;  max-width:800px; }
        </Style>
      </Head>
      <body>
        <table style="border-style:none; background:#ffffff">
          <tr bgcolor="white">
            <td align="center"><h2>Feature Results Report</h2></td>
            <td width="20%">
              <b>Per priority passrate :</b>
              <table style="table-layout: auto; background:lightblue" >
                <tr align="center">
                  <td>P0</td>
                  <td>P1</td>
                  <td>P2</td>
                  <td>P3</td>
                  <td>P4</td>
                </tr>
                <tr align="center">
                  <td>
                    <xsl:variable name ="P0Failures" select = 'sum(Variations/SubArea/Test[@Priority="0"]/Test/@Failures)'/>
                    <xsl:variable name ="P0Total" select = 'sum(Variations/SubArea/Test[@Priority="0"]/Test/@Total)'/>
                    <xsl:if test="$P0Total != 0">
                      <xsl:value-of select='format-number(100 - (100 * ($P0Failures div $P0Total)), "0.00")'/>
                    </xsl:if>&#xA0;
                  </td>
                  <td>
                    <xsl:variable name ="P1Failures" select = 'sum(Variations/SubArea/Test[@Priority="1"]/Test/@Failures)'/>
                    <xsl:variable name ="P1Total" select = 'sum(Variations/SubArea/Test[@Priority="1"]/Test/@Total)'/>
                    <xsl:if test="$P1Total != 0">
                      <xsl:value-of select='format-number(100 - (100 * ($P1Failures div $P1Total)), "0.00")'/>
                    </xsl:if>&#xA0;
                  </td>
                  <td>
                    <xsl:variable name ="P2Failures" select = 'sum(Variations/SubArea/Test[@Priority="2"]/Test/@Failures)'/>
                    <xsl:variable name ="P2Total" select = 'sum(Variations/SubArea/Test[@Priority="2"]/Test/@Total)'/>
                    <xsl:if test="$P2Total != 0">
                      <xsl:value-of select='format-number(100 - (100 * ($P2Failures div $P2Total)), "0.00")'/>
                    </xsl:if>&#xA0;
                  </td>
                  <td>
                    <xsl:variable name ="P3Failures" select = 'sum(Variations/SubArea/Test[@Priority="3"]/Test/@Failures)'/>
                    <xsl:variable name ="P3Total" select = 'sum(Variations/SubArea/Test[@Priority="3"]/Test/@Total)'/>
                    <xsl:if test="$P3Total != 0">
                      <xsl:value-of select='format-number(100 - (100 * ($P3Failures div $P3Total)), "0.00")'/>
                    </xsl:if>&#xA0;
                  </td>
                  <td>
                    <xsl:variable name ="P4Failures" select = 'sum(Variations/SubArea/Test[@Priority="4"]/Test/@Failures)'/>
                    <xsl:variable name ="P4Total" select = 'sum(Variations/SubArea/Test[@Priority="4"]/Test/@Total)'/>
                    <xsl:if test="$P4Total != 0">
                      <xsl:value-of select='format-number(100 - (100 * ($P4Failures div $P4Total)), "0.00")'/>
                    </xsl:if>&#xA0;
                  </td>
                </tr>
              </table>
            </td>

            <td width="20%">
              <b>Color coding:</b>
              <table style="table-layout: auto;" >
                <tr>
                  <td bgcolor="Orange">Fail</td>
                  <td bgcolor="#AAAAAA">Fail with known bug</td>
                </tr>
                <tr>
                  <td bgcolor="#FFFFCC">Ignore</td>
                  <td bgcolor="lightgreen">Pass</td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
        <br/>
        <table border="5" style="font-size:14px;">
          <Col Width="35%"/>
          <Col Width="25%"/>
          <Col Width="15%"/>
          <Col Width="15%"/>
          <Col Width="10%"/>
          <tr bgcolor="#C0504D">
            <th>SubArea</th>
            <th>Duration (S)</th>
            <th ALIGN="center">Failures</th>
            <th ALIGN="center">Total</th>
            <th ALIGN="center" colspan="2">Pass Rate</th>
          </tr>

          <xsl:for-each select="Variations/SubArea">
            <xsl:sort select="@SubArea"/>
            <tr>
              <xsl:if test="position() mod 2 = 0">
                <xsl:attribute name="bgcolor">#DDDDDD</xsl:attribute>
              </xsl:if>
              <xsl:if test="sum(Test/Test/@Failures) != 0">
                  <xsl:attribute name="bgcolor">Orange</xsl:attribute>
              </xsl:if>  
              <td>
                <code style="position:relative;top:-2;cursor:hand;"
                  title="Tests" expandTitle="Show Tests" collapseTitle="Hide Tests"
                  collapseText="[-]" expandText="[+]" >
                  <xsl:attribute name="onclick">
                    var rowDetails = this.parentElement.parentElement.nextSibling;
                    var needExpand = (rowDetails.style.display == "none");
                    rowDetails.style.display = (needExpand) ? "" : "none";
                    this.innerText = (needExpand) ? collapseText : expandText;
                    this.title = (needExpand) ? collapseTitle : expandTitle;
                  </xsl:attribute>
                  <xsl:attribute name="onmouseover">this.style.color = 'blue';</xsl:attribute>
                  <xsl:attribute name="onmouseout">this.style.color = '';</xsl:attribute>
                  [+]
                </code>
                <xsl:value-of select="@SubArea"/>
              </td>
              <td ALIGN="right">
                <xsl:value-of select='format-number(sum(Test/Test/@Duration), "0.00")'/>
              </td>

              <xsl:variable name ="Failures" select = 'sum(Test/Test/@Failures)'/>
              <xsl:variable name ="Total" select = 'sum(Test/Test/@Total)'/>

              <td ALIGN="right">
                <xsl:value-of select='$Failures'/>
              </td>
              <td ALIGN="right">
                <xsl:value-of select='$Total'/>
              </td>

              <td ALIGN="right">
                <xsl:if test="$Total != 0">
                  &#xA0;<xsl:value-of select='format-number(100 - (100 * ($Failures div $Total)), "0.00")'/>
                </xsl:if>
              </td>
            </tr>
            <tr style="display:none;">
              <td colspan="5">
                <table border="5" style="font-size:14px;">
                  <Col Width="50"/>
                  <Col Width="50px"/>
                  <Col Width="70px"/>
                  <Col Width="50px"/>
                  <Col Width="50px"/>
                  <Col Width="50px"/>
                  <Col Width="70px"/>
                  <Col Width="97px"/>                  
                  <tr bgcolor="#A0605D">                    
                    <th>Test Name</th>
                    <th>Priority</th>
                    <th>Duration (S)</th>
                    <th>Failures</th>
                    <th>Total</th>
                    <th>Known Bugs</th>
                    <th>Log Directory</th>
                    <th>Machine</th>
                  </tr>
                  <xsl:call-template name="SubAreaView" />
                </table>
              </td>
            </tr>
          </xsl:for-each>
        </table>
      </body>
    </html>
  </xsl:template>

  <xsl:template name="SubAreaView">
    <xsl:for-each select="Test">
      <xsl:sort select="Test/@Result"/>
      <xsl:sort select="@KnownBugs"/>
      <xsl:sort select="@Priority"/>
      <tr>
        <xsl:choose>
          <xsl:when test="Test/@Result='Pass'">
            <xsl:attribute name="BGCOLOR">LightGreen</xsl:attribute>
          </xsl:when>
          <xsl:when test="Test/@Result='Ignore'">
            <xsl:attribute name="BGCOLOR">#FFFFCC</xsl:attribute>
          </xsl:when>
          <xsl:when test="Test/@Result='Fail' and @KnownBugs!=''">
            <xsl:attribute name="BGCOLOR">#AAAAAA</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="BGCOLOR">Orange</xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>

        <td ALIGN="left">
          <code>
            <xsl:attribute name="onclick">
              var rowDetails = this.parentElement.parentElement.nextSibling;
              var needExpand = (rowDetails.style.display == "none");
              rowDetails.style.display = (needExpand) ? "" : "none";
              this.Style= (needExpand) ? "none" : "flipV";
            </xsl:attribute>
           <img src="..\expand_shrink.gif" style="filter: flipV"/> 
          </code> &#xA0;
          <xsl:value-of select="@Name"/>
          <xsl:if test="@TestInfo!=''">
            <a href="{@TestInfo}">
              (TestInfo)
            </a>
          </xsl:if>         
        </td>
        <td ALIGN="center">
          <xsl:value-of select="@Priority"/>
        </td>
        <td ALIGN="right">
          <xsl:value-of select="Test/@Duration"/>
        </td>
        <td ALIGN="right">
          <xsl:value-of select="Test/@Failures"/>
        </td>
        <td ALIGN="right">
          <xsl:value-of select="Test/@Total"/>
        </td>
        <td ALIGN="center">
          <xsl:value-of select="@KnownBugs"/>&#xA0;
        </td>
        <td ALIGN="center">
          <xsl:if test="@LogDir!=''">
            <a href="{@LogDir}" >Test Logs</a>
          </xsl:if>&#xA0;
        </td>
        <td ALIGN="center">
          <xsl:value-of select="@Machine"/>
        </td>
      </tr>
      <tr style="display:none;">
        <td colspan="9">
          <table border="0" style="font-size:14px;">
            <Col Width="50px"/>
            <Col Width="50px"/>
            <Col Width="70px"/>
            <Col Width="95px"/>
            <tr bgcolor="#A0706D">
              <th>Variation</th>
              <th>Duration (S)</th>
              <th>Result</th>
              <th>Log Directory</th>
            </tr>
            <xsl:call-template name="TestView" />
          </table>
        </td>
      </tr>
      <xsl:if test="@Log!=''">
        <tr class="log">
          <td Colspan="9">
            <b>Test Log: </b>
            <xsl:if test="@LogPath!=''">
              <a href="{@LogPath}" >Full Log</a>
            </xsl:if>
            <pre>
            <xsl:value-of select="@Log"/>
            </pre>
          </td>
        </tr>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>
  
  <xsl:template name="TestView">
    <xsl:for-each select="Variation">
      <xsl:sort select="@Result"/>
      <tr>
        <xsl:choose>
          <xsl:when test="@Result='Pass'">
            <xsl:attribute name="BGCOLOR">LightGreen</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="BGCOLOR">Orange</xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>

        <td ALIGN="center">
          <xsl:value-of select="@Variation"/>
        </td>
        <td ALIGN="right">
          <xsl:value-of select="@Duration"/>
        </td>
         
        <td ALIGN="center">
          <xsl:value-of select="@Result"/>
        </td>
          
        <td ALIGN="center">
          <xsl:if test="@LogDir!=''">
            <a href="{@LogDir}" >Logs</a>
          </xsl:if>
        </td>
      </tr>

      <xsl:if test="@Log!=''">
        <tr class="log">
          <td Colspan="9">
              <b>Variation Log: </b>
              <xsl:if test="@LogPath!=''">
                <a href="{@LogPath}" >Full Log</a>
              </xsl:if>            
            <pre>
              <xsl:value-of select="@Log"/>            
            </pre>
          </td>
        </tr>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>

</xsl:stylesheet>