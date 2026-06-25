// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class PublishLicenseTests
{
    // Taken from https://github.com/dotnet/dotnet-api-docs/blob/main/snippets/csharp/System.Security.RightsManagement/CryptoProvider/BlockSize/Content/Truffle.png.PublishLicense.xml.
    private const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
  <BODY type="Microsoft Rights Label" version="3.0">
    <ISSUEDTIME>2006-04-19T03:14</ISSUEDTIME>
    <ISSUER>
      <OBJECT type="Group-Identity">
        <ID type="Windows">S-1-5-21-2127521184-1604012920-1887927527-1723404</ID>
        <NAME>jack.davis@microsoft.com</NAME>
      </OBJECT>
      <PUBLICKEY>
        <ALGORITHM>RSA</ALGORITHM>
        <PARAMETER name="public-exponent">
          <VALUE encoding="integer32">65537</VALUE>
        </PARAMETER>
        <PARAMETER name="modulus">
          <VALUE encoding="base64" size="1024">r1rbEmgQhskJ29p8xpX00ZlROS13kHMzv0DrOQOn+4mijO3Eanelw1F+mYdSdrByU9MbamWHalUqku6wESZY2DCpRU2C3xw2lCvPtrYqtJkxSt835Ez4quDYVNQq+15DzA6qyl5gc07XuoQi1AC0Nfb5tMy0R85SocFE195VUq4=</VALUE>
        </PARAMETER>
      </PUBLICKEY>
      <SECURITYLEVEL name="SDK" value="5.2.3790.134" />
    </ISSUER>
    <DISTRIBUTIONPOINT>
      <OBJECT type="License-Acquisition-URL">
        <ID type="MS-GUID">{0F45FD50-383B-43EE-90A4-ED013CD0CFE5}</ID>
        <NAME>DRM Server Cluster</NAME>
        <ADDRESS type="URL">http://ed-drm-red3/_wmcs/licensing</ADDRESS>
      </OBJECT>
    </DISTRIBUTIONPOINT>
    <ISSUEDPRINCIPALS>
      <PRINCIPAL internal-id="1">
        <OBJECT type="MS-DRM-Server">
          <ID type="MS-GUID">{1d59a4ae-e6ae-4151-b458-afc5251fe0c3}</ID>
          <ADDRESS type="URL">http://ed-drm-red3/_wmcs</ADDRESS>
        </OBJECT>
        <PUBLICKEY>
          <ALGORITHM>RSA</ALGORITHM>
          <PARAMETER name="public-exponent">
            <VALUE encoding="integer32">65537</VALUE>
          </PARAMETER>
          <PARAMETER name="modulus">
            <VALUE encoding="base64" size="1024">43b8U8yG5ifu38tkAa8K/2DnMOZqgVdj8OZCY+V0332efhaocT7EGV8JE3Htolc2mqTDdLlHQQoJ9jrG36efYYqo4aivo7ddx5w9NlMo9O4mXb+s70LD1VPaM6TywWYYfho+6vTGI1SwPJVgmwS2Qgha/AXOJrK0t5gEX8CZPMo=</VALUE>
          </PARAMETER>
        </PUBLICKEY>
        <SECURITYLEVEL name="Server-Version" value="5.2.3790.134" />
        <SECURITYLEVEL name="Server-SKU" value="RMS 1.0" />
        <ENABLINGBITS type="sealed-key">
          <VALUE encoding="base64" size="1536">OmZReXce7iXuQZ+ySktmUyK0sApe4IxmBTIpzsPaIcYK/ll4SxzxwUO5BLUAV9SY41nPYX+zFMKKOkVC2GdKuKlERXYgR8LvyDIifKm8/OUL2q5XKsW4pRXMfm4ccGokq1lv0pCMS0qIreAmSURdK+FIVjWwPeFQu2N1iKwHigDjHKbva9ICtkxXfZtgEwgakypbFV/T7WqrpWxS8l4bBsIAKcYzuUbLgQOYCc/lJBUWDJqMMPsyV1J65ZHlO3Nd</VALUE>
        </ENABLINGBITS>
      </PRINCIPAL>
    </ISSUEDPRINCIPALS>
    <WORK>
      <OBJECT>
        <ID type="MS-GUID">{9257669d-2753-4f8f-94c6-028987c0434b}</ID>
      </OBJECT>
      <METADATA>
        <OWNER>
          <OBJECT>
            <ID type="Windows" />
            <NAME>jack.davis@microsoft.com</NAME>
          </OBJECT>
        </OWNER>
      </METADATA>
    </WORK>
    <AUTHENTICATEDDATA id="Encrypted-Rights-Data">Wy6PnRT6uGKHJ/b3uRktcgNL2bBMEXsSneudY49Oy2tZy44QG5WWIhjGHbRH5CNHC3zAE4H4KIc7MvYde/GvbHb6reWTfFDsw7P7DfERz5ArqKr6+wpxXVrX3CmA+wA0kh5KmPF142/NRrFpno1dF9Wv/+J8nBwvNFaj+T6LayF5kWG9GIl1bimAUaXZQpyJmxnRXP8T78Q2udn66osD2cm0rx0cl8r7d7m2gXR8VxQMSIc76wQ/nveDgGRlKrCXFKXhj8VqLx5j6OFkpSqxwGYBhqkJMd4wVUkb2Jhw4M8kPivGg1JlK2yBLN3hgXYxq0ASeNloUZteReOK9OE7UyKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKsyYJmY4FM5Pi6QoMyZQ/18NTRTdiPl0JVYA0tRYDa+J5fBadoyCY5WSDXuePmawI0OEcnhxOfLnB1CgUD+c0+AZpCwVlMocmYTpX90xX6yEOTf/aaNCcotIF+YndxZUvqkVT3HqHnRKrr10ZS6YwDS6YgvJX9w0UaDwMvH3UMEBxC8SKZfKQ0hgg8loZ38dhODT/Bpkf3YnBQ2Zo7bGTESKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKt70q5TxZhcVjJqTQzap0aD9a9wzYi6mMAVBZE3jDLxNMg8ysSrrFTKezKljLb9Yql5sD00pRZOAVmZL88z1JGLNw/zT42yJzqfchXuMNG/XuPETOsndlGBIRbmGfi4WKqaBsZ3Ac+JULrNIqBk2rupkE0HqSHYtTLxmeamFVZWAYK96yps+gtqPHGLNC36f93l/km1Z5cIg+jOXFhuutXPlu/tg/JqkAshwoAJYe6Fk72qRLXFBQtjtJSoAjlfHLODFKwNkX4Ggc0gGw+8VTxN0rnYnGC2ZKlLIpb3M2WEqpSqnG0Voh0tKruAmcTgXWSKwNlUjWTJ7A95tBUUw9g4pXGX5EXHqCSvw6dqt99eW1dUfrCIXupmR+YVIydID7z/vAMvoO5luQnatPZJrM9ETzyPS/OAcUCR7VpSnVqPFntz3Tb9Hzh+4hZ4dex8EEL34kyxOHoI2JYVJzSd73b7N7TahserIAlyaK0S6IUDmmPrituN5Mc9z2osevePbJsVpgRhiAglqIoJ5J2ReYPkaP3tJYlBWvPvaGsGqxVDDSxqxRCotZTlweB+/Prf2tqK8YTXxxTEsBtXD3xCvsWjiA67BMSduPTnKpbvlZDYEUbf8MFSA9UWVc0Xe3urRqtXVH6wiF7qZkfmFSMnSA+5rebPPxn+BLaMZCpbiOp/EKOZG5qTyHxdkvT8dfytZK0Oqu+1nlcTzlEf7zDve5dMPzF0MhNoT0/4vlOEKqx8gEcA9SNo+bY81mh/Vw0R3FqK8YTXxxTEsBtXD3xCvsWmgJGKbpKm7VZP49gYKGfQruR25tBiNopkPeiAK4UjDiQ7lzfsEN9xWy6g6B8Dp/6fEkKTxHlK5EUwPpszCZDhSwaRYhz6j+W7JeO9LC/LJfZ4dS3+iMTdxtodC2Rrm3Da1ZSPRGQiwTGElOAhc5XJYkdIrv3h+G3ObViRXevP0iLpiC8lf3DRRoPAy8fdQwQCU7ezn9Mdsj9MeezTYyEwp3mCXWx9ZrHfBl8Tpuj0enllQIjpcGWHaZZ0Pce31sRoi8vaoQCRu3pbm4cKKwU/sYlQNBX0D1DE2ZQB6C2NvtwdIU2AZcYvGjXDGaZ5YRh1+Kq/vvRISvQFSbl5uUPMpfgTqhDPYwjIfsEoVv9Rki9dFh4xLuk3n8Mo6lWm+BcQUNPG2fz+8MH3Hd2QfCNNynHEJAbT2KFh5VIKwzoughUBZR9r7VmSoYg2l3YXb5hg==</AUTHENTICATEDDATA>
  </BODY>
  <SIGNATURE>
    <ALGORITHM>RSA PKCS#1-V1.5</ALGORITHM>
    <DIGEST>
      <ALGORITHM>SHA1</ALGORITHM>
      <PARAMETER name="codingtype">
        <VALUE encoding="string">surface-coding</VALUE>
      </PARAMETER>
      <VALUE encoding="base64" size="160">0zkJi3NJke+OehTek7lsFT675JU=</VALUE>
    </DIGEST>
    <VALUE encoding="base64" size="1024">VObVcSvMdfw4pE7S2Q1o/oeE3d72yhuu42hZz5vpcg8elVfRPFrpxECWR1f9cNxSX6j32JJ9W1+vD2YNOeykWg9iIcbq0e82d/WTvkOs5eIFIWtqjGidOvnG7UCUYBktBiKJ5IDeo68P6GfsEtby8o54MlIztvWXeB5T21nhz2I=</VALUE>
  </SIGNATURE>
</XrML>
""";

    [Fact]
    public void Ctor_TruffleExample()
    {
        var license = new PublishLicense(PublishLicenseTemplate);
        Assert.Equal(new Guid("9257669d-2753-4f8f-94c6-028987c0434b"), license.ContentId);
        Assert.Null(license.ReferralInfoName);
        Assert.Null(license.ReferralInfoUri);
        Assert.Equal(new Uri("http://ed-drm-red3/_wmcs/licensing"), license.UseLicenseAcquisitionUrl);
    }

    [Fact]
    public void Ctor_NullSignedPublishLicense_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("signedPublishLicense", () => new PublishLicense(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("<manifest></manifest>")]
    public void Ctor_InvalidSignedPublishLicense_ThrowsRightsManagementException(string signedPublishLicense)
    {
        Assert.Throws<RightsManagementException>(() => new PublishLicense(signedPublishLicense));
    }

    [Fact]
    public void Ctor_EmptyUseLicenseAcquisitionUrl_ThrowsArgumentNullException()
    {
        const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
</XrML>
""";
        Assert.Throws<ArgumentNullException>("uriString", () => new PublishLicense(PublishLicenseTemplate));
    }

    [Fact]
    public void Ctor_InvalidUseLicenseAcquisitionUrl_ThrowsUriFormatException()
    {
        const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
    <BODY type="Microsoft Rights Label" version="3.0">
        <DISTRIBUTIONPOINT>
            <OBJECT type="License-Acquisition-URL">
                <ID type="MS-GUID">{0F45FD50-383B-43EE-90A4-ED013CD0CFE5}</ID>
                <NAME>DRM Server Cluster</NAME>
                <ADDRESS type="URL">invalid</ADDRESS>
            </OBJECT>
        </DISTRIBUTIONPOINT>
    </BODY>
</XrML>
""";
        Assert.Throws<UriFormatException>(() => new PublishLicense(PublishLicenseTemplate));
    }

    [Fact]
    public void Ctor_NoQueries_ThrowsRightsManagementException()
    {
        const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
    <BODY type="Microsoft Rights Label" version="3.0">
        <DISTRIBUTIONPOINT>
            <OBJECT type="License-Acquisition-URL">
                <ID type="MS-GUID">{0F45FD50-383B-43EE-90A4-ED013CD0CFE5}</ID>
                <NAME>DRM Server Cluster</NAME>
                <ADDRESS type="URL">https://google.com</ADDRESS>
            </OBJECT>
        </DISTRIBUTIONPOINT>
    </BODY>
</XrML>
""";
        Assert.Throws<RightsManagementException>(() => new PublishLicense(PublishLicenseTemplate));
    }

    [Fact]
    public void Ctor_InvalidContentId_ThrowsFormatException()
    {
        const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
  <BODY type="Microsoft Rights Label" version="3.0">
    <ISSUEDTIME>2006-04-19T03:14</ISSUEDTIME>
    <ISSUER>
      <OBJECT type="Group-Identity">
        <ID type="Windows">S-1-5-21-2127521184-1604012920-1887927527-1723404</ID>
        <NAME>jack.davis@microsoft.com</NAME>
      </OBJECT>
      <PUBLICKEY>
        <ALGORITHM>RSA</ALGORITHM>
        <PARAMETER name="public-exponent">
          <VALUE encoding="integer32">65537</VALUE>
        </PARAMETER>
        <PARAMETER name="modulus">
          <VALUE encoding="base64" size="1024">r1rbEmgQhskJ29p8xpX00ZlROS13kHMzv0DrOQOn+4mijO3Eanelw1F+mYdSdrByU9MbamWHalUqku6wESZY2DCpRU2C3xw2lCvPtrYqtJkxSt835Ez4quDYVNQq+15DzA6qyl5gc07XuoQi1AC0Nfb5tMy0R85SocFE195VUq4=</VALUE>
        </PARAMETER>
      </PUBLICKEY>
      <SECURITYLEVEL name="SDK" value="5.2.3790.134" />
    </ISSUER>
    <DISTRIBUTIONPOINT>
      <OBJECT type="License-Acquisition-URL">
        <ID type="MS-GUID">{0F45FD50-383B-43EE-90A4-ED013CD0CFE5}</ID>
        <NAME>DRM Server Cluster</NAME>
        <ADDRESS type="URL">http://ed-drm-red3/_wmcs/licensing</ADDRESS>
      </OBJECT>
    </DISTRIBUTIONPOINT>
    <ISSUEDPRINCIPALS>
      <PRINCIPAL internal-id="1">
        <OBJECT type="MS-DRM-Server">
          <ID type="MS-GUID">{1d59a4ae-e6ae-4151-b458-afc5251fe0c3}</ID>
          <ADDRESS type="URL">http://ed-drm-red3/_wmcs</ADDRESS>
        </OBJECT>
        <PUBLICKEY>
          <ALGORITHM>RSA</ALGORITHM>
          <PARAMETER name="public-exponent">
            <VALUE encoding="integer32">65537</VALUE>
          </PARAMETER>
          <PARAMETER name="modulus">
            <VALUE encoding="base64" size="1024">43b8U8yG5ifu38tkAa8K/2DnMOZqgVdj8OZCY+V0332efhaocT7EGV8JE3Htolc2mqTDdLlHQQoJ9jrG36efYYqo4aivo7ddx5w9NlMo9O4mXb+s70LD1VPaM6TywWYYfho+6vTGI1SwPJVgmwS2Qgha/AXOJrK0t5gEX8CZPMo=</VALUE>
          </PARAMETER>
        </PUBLICKEY>
        <SECURITYLEVEL name="Server-Version" value="5.2.3790.134" />
        <SECURITYLEVEL name="Server-SKU" value="RMS 1.0" />
        <ENABLINGBITS type="sealed-key">
          <VALUE encoding="base64" size="1536">OmZReXce7iXuQZ+ySktmUyK0sApe4IxmBTIpzsPaIcYK/ll4SxzxwUO5BLUAV9SY41nPYX+zFMKKOkVC2GdKuKlERXYgR8LvyDIifKm8/OUL2q5XKsW4pRXMfm4ccGokq1lv0pCMS0qIreAmSURdK+FIVjWwPeFQu2N1iKwHigDjHKbva9ICtkxXfZtgEwgakypbFV/T7WqrpWxS8l4bBsIAKcYzuUbLgQOYCc/lJBUWDJqMMPsyV1J65ZHlO3Nd</VALUE>
        </ENABLINGBITS>
      </PRINCIPAL>
    </ISSUEDPRINCIPALS>
    <WORK>
      <OBJECT>
        <ID type="MS-GUID">invalid</ID>
      </OBJECT>
      <METADATA>
        <OWNER>
          <OBJECT>
            <ID type="Windows" />
            <NAME>jack.davis@microsoft.com</NAME>
          </OBJECT>
        </OWNER>
      </METADATA>
    </WORK>
    <AUTHENTICATEDDATA id="Encrypted-Rights-Data">Wy6PnRT6uGKHJ/b3uRktcgNL2bBMEXsSneudY49Oy2tZy44QG5WWIhjGHbRH5CNHC3zAE4H4KIc7MvYde/GvbHb6reWTfFDsw7P7DfERz5ArqKr6+wpxXVrX3CmA+wA0kh5KmPF142/NRrFpno1dF9Wv/+J8nBwvNFaj+T6LayF5kWG9GIl1bimAUaXZQpyJmxnRXP8T78Q2udn66osD2cm0rx0cl8r7d7m2gXR8VxQMSIc76wQ/nveDgGRlKrCXFKXhj8VqLx5j6OFkpSqxwGYBhqkJMd4wVUkb2Jhw4M8kPivGg1JlK2yBLN3hgXYxq0ASeNloUZteReOK9OE7UyKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKsyYJmY4FM5Pi6QoMyZQ/18NTRTdiPl0JVYA0tRYDa+J5fBadoyCY5WSDXuePmawI0OEcnhxOfLnB1CgUD+c0+AZpCwVlMocmYTpX90xX6yEOTf/aaNCcotIF+YndxZUvqkVT3HqHnRKrr10ZS6YwDS6YgvJX9w0UaDwMvH3UMEBxC8SKZfKQ0hgg8loZ38dhODT/Bpkf3YnBQ2Zo7bGTESKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKt70q5TxZhcVjJqTQzap0aD9a9wzYi6mMAVBZE3jDLxNMg8ysSrrFTKezKljLb9Yql5sD00pRZOAVmZL88z1JGLNw/zT42yJzqfchXuMNG/XuPETOsndlGBIRbmGfi4WKqaBsZ3Ac+JULrNIqBk2rupkE0HqSHYtTLxmeamFVZWAYK96yps+gtqPHGLNC36f93l/km1Z5cIg+jOXFhuutXPlu/tg/JqkAshwoAJYe6Fk72qRLXFBQtjtJSoAjlfHLODFKwNkX4Ggc0gGw+8VTxN0rnYnGC2ZKlLIpb3M2WEqpSqnG0Voh0tKruAmcTgXWSKwNlUjWTJ7A95tBUUw9g4pXGX5EXHqCSvw6dqt99eW1dUfrCIXupmR+YVIydID7z/vAMvoO5luQnatPZJrM9ETzyPS/OAcUCR7VpSnVqPFntz3Tb9Hzh+4hZ4dex8EEL34kyxOHoI2JYVJzSd73b7N7TahserIAlyaK0S6IUDmmPrituN5Mc9z2osevePbJsVpgRhiAglqIoJ5J2ReYPkaP3tJYlBWvPvaGsGqxVDDSxqxRCotZTlweB+/Prf2tqK8YTXxxTEsBtXD3xCvsWjiA67BMSduPTnKpbvlZDYEUbf8MFSA9UWVc0Xe3urRqtXVH6wiF7qZkfmFSMnSA+5rebPPxn+BLaMZCpbiOp/EKOZG5qTyHxdkvT8dfytZK0Oqu+1nlcTzlEf7zDve5dMPzF0MhNoT0/4vlOEKqx8gEcA9SNo+bY81mh/Vw0R3FqK8YTXxxTEsBtXD3xCvsWmgJGKbpKm7VZP49gYKGfQruR25tBiNopkPeiAK4UjDiQ7lzfsEN9xWy6g6B8Dp/6fEkKTxHlK5EUwPpszCZDhSwaRYhz6j+W7JeO9LC/LJfZ4dS3+iMTdxtodC2Rrm3Da1ZSPRGQiwTGElOAhc5XJYkdIrv3h+G3ObViRXevP0iLpiC8lf3DRRoPAy8fdQwQCU7ezn9Mdsj9MeezTYyEwp3mCXWx9ZrHfBl8Tpuj0enllQIjpcGWHaZZ0Pce31sRoi8vaoQCRu3pbm4cKKwU/sYlQNBX0D1DE2ZQB6C2NvtwdIU2AZcYvGjXDGaZ5YRh1+Kq/vvRISvQFSbl5uUPMpfgTqhDPYwjIfsEoVv9Rki9dFh4xLuk3n8Mo6lWm+BcQUNPG2fz+8MH3Hd2QfCNNynHEJAbT2KFh5VIKwzoughUBZR9r7VmSoYg2l3YXb5hg==</AUTHENTICATEDDATA>
  </BODY>
  <SIGNATURE>
    <ALGORITHM>RSA PKCS#1-V1.5</ALGORITHM>
    <DIGEST>
      <ALGORITHM>SHA1</ALGORITHM>
      <PARAMETER name="codingtype">
        <VALUE encoding="string">surface-coding</VALUE>
      </PARAMETER>
      <VALUE encoding="base64" size="160">0zkJi3NJke+OehTek7lsFT675JU=</VALUE>
    </DIGEST>
    <VALUE encoding="base64" size="1024">VObVcSvMdfw4pE7S2Q1o/oeE3d72yhuu42hZz5vpcg8elVfRPFrpxECWR1f9cNxSX6j32JJ9W1+vD2YNOeykWg9iIcbq0e82d/WTvkOs5eIFIWtqjGidOvnG7UCUYBktBiKJ5IDeo68P6GfsEtby8o54MlIztvWXeB5T21nhz2I=</VALUE>
  </SIGNATURE>
</XrML>
""";
        Assert.Throws<FormatException>(() => new PublishLicense(PublishLicenseTemplate));
    }

    [Fact]
    public void Ctor_NoContentId_ThrowsRightsManagementException()
    {
        const string PublishLicenseTemplate = """
<XrML version="1.2" xmlns="">
  <BODY type="Microsoft Rights Label" version="3.0">
    <ISSUEDTIME>2006-04-19T03:14</ISSUEDTIME>
    <ISSUER>
      <OBJECT type="Group-Identity">
        <ID type="Windows">S-1-5-21-2127521184-1604012920-1887927527-1723404</ID>
        <NAME>jack.davis@microsoft.com</NAME>
      </OBJECT>
      <PUBLICKEY>
        <ALGORITHM>RSA</ALGORITHM>
        <PARAMETER name="public-exponent">
          <VALUE encoding="integer32">65537</VALUE>
        </PARAMETER>
        <PARAMETER name="modulus">
          <VALUE encoding="base64" size="1024">r1rbEmgQhskJ29p8xpX00ZlROS13kHMzv0DrOQOn+4mijO3Eanelw1F+mYdSdrByU9MbamWHalUqku6wESZY2DCpRU2C3xw2lCvPtrYqtJkxSt835Ez4quDYVNQq+15DzA6qyl5gc07XuoQi1AC0Nfb5tMy0R85SocFE195VUq4=</VALUE>
        </PARAMETER>
      </PUBLICKEY>
      <SECURITYLEVEL name="SDK" value="5.2.3790.134" />
    </ISSUER>
    <DISTRIBUTIONPOINT>
      <OBJECT type="License-Acquisition-URL">
        <ID type="MS-GUID">{0F45FD50-383B-43EE-90A4-ED013CD0CFE5}</ID>
        <NAME>DRM Server Cluster</NAME>
        <ADDRESS type="URL">http://ed-drm-red3/_wmcs/licensing</ADDRESS>
      </OBJECT>
    </DISTRIBUTIONPOINT>
    <ISSUEDPRINCIPALS>
      <PRINCIPAL internal-id="1">
        <OBJECT type="MS-DRM-Server">
          <ID type="MS-GUID">{1d59a4ae-e6ae-4151-b458-afc5251fe0c3}</ID>
          <ADDRESS type="URL">http://ed-drm-red3/_wmcs</ADDRESS>
        </OBJECT>
        <PUBLICKEY>
          <ALGORITHM>RSA</ALGORITHM>
          <PARAMETER name="public-exponent">
            <VALUE encoding="integer32">65537</VALUE>
          </PARAMETER>
          <PARAMETER name="modulus">
            <VALUE encoding="base64" size="1024">43b8U8yG5ifu38tkAa8K/2DnMOZqgVdj8OZCY+V0332efhaocT7EGV8JE3Htolc2mqTDdLlHQQoJ9jrG36efYYqo4aivo7ddx5w9NlMo9O4mXb+s70LD1VPaM6TywWYYfho+6vTGI1SwPJVgmwS2Qgha/AXOJrK0t5gEX8CZPMo=</VALUE>
          </PARAMETER>
        </PUBLICKEY>
        <SECURITYLEVEL name="Server-Version" value="5.2.3790.134" />
        <SECURITYLEVEL name="Server-SKU" value="RMS 1.0" />
        <ENABLINGBITS type="sealed-key">
          <VALUE encoding="base64" size="1536">OmZReXce7iXuQZ+ySktmUyK0sApe4IxmBTIpzsPaIcYK/ll4SxzxwUO5BLUAV9SY41nPYX+zFMKKOkVC2GdKuKlERXYgR8LvyDIifKm8/OUL2q5XKsW4pRXMfm4ccGokq1lv0pCMS0qIreAmSURdK+FIVjWwPeFQu2N1iKwHigDjHKbva9ICtkxXfZtgEwgakypbFV/T7WqrpWxS8l4bBsIAKcYzuUbLgQOYCc/lJBUWDJqMMPsyV1J65ZHlO3Nd</VALUE>
        </ENABLINGBITS>
      </PRINCIPAL>
    </ISSUEDPRINCIPALS>
    <WORK>
      <METADATA>
        <OWNER>
          <OBJECT>
            <ID type="Windows" />
            <NAME>jack.davis@microsoft.com</NAME>
          </OBJECT>
        </OWNER>
      </METADATA>
    </WORK>
    <AUTHENTICATEDDATA id="Encrypted-Rights-Data">Wy6PnRT6uGKHJ/b3uRktcgNL2bBMEXsSneudY49Oy2tZy44QG5WWIhjGHbRH5CNHC3zAE4H4KIc7MvYde/GvbHb6reWTfFDsw7P7DfERz5ArqKr6+wpxXVrX3CmA+wA0kh5KmPF142/NRrFpno1dF9Wv/+J8nBwvNFaj+T6LayF5kWG9GIl1bimAUaXZQpyJmxnRXP8T78Q2udn66osD2cm0rx0cl8r7d7m2gXR8VxQMSIc76wQ/nveDgGRlKrCXFKXhj8VqLx5j6OFkpSqxwGYBhqkJMd4wVUkb2Jhw4M8kPivGg1JlK2yBLN3hgXYxq0ASeNloUZteReOK9OE7UyKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKsyYJmY4FM5Pi6QoMyZQ/18NTRTdiPl0JVYA0tRYDa+J5fBadoyCY5WSDXuePmawI0OEcnhxOfLnB1CgUD+c0+AZpCwVlMocmYTpX90xX6yEOTf/aaNCcotIF+YndxZUvqkVT3HqHnRKrr10ZS6YwDS6YgvJX9w0UaDwMvH3UMEBxC8SKZfKQ0hgg8loZ38dhODT/Bpkf3YnBQ2Zo7bGTESKFGhE9Z5T0sDJiVrzeko/a/GcU/LnlfIko12ImlYaKt70q5TxZhcVjJqTQzap0aD9a9wzYi6mMAVBZE3jDLxNMg8ysSrrFTKezKljLb9Yql5sD00pRZOAVmZL88z1JGLNw/zT42yJzqfchXuMNG/XuPETOsndlGBIRbmGfi4WKqaBsZ3Ac+JULrNIqBk2rupkE0HqSHYtTLxmeamFVZWAYK96yps+gtqPHGLNC36f93l/km1Z5cIg+jOXFhuutXPlu/tg/JqkAshwoAJYe6Fk72qRLXFBQtjtJSoAjlfHLODFKwNkX4Ggc0gGw+8VTxN0rnYnGC2ZKlLIpb3M2WEqpSqnG0Voh0tKruAmcTgXWSKwNlUjWTJ7A95tBUUw9g4pXGX5EXHqCSvw6dqt99eW1dUfrCIXupmR+YVIydID7z/vAMvoO5luQnatPZJrM9ETzyPS/OAcUCR7VpSnVqPFntz3Tb9Hzh+4hZ4dex8EEL34kyxOHoI2JYVJzSd73b7N7TahserIAlyaK0S6IUDmmPrituN5Mc9z2osevePbJsVpgRhiAglqIoJ5J2ReYPkaP3tJYlBWvPvaGsGqxVDDSxqxRCotZTlweB+/Prf2tqK8YTXxxTEsBtXD3xCvsWjiA67BMSduPTnKpbvlZDYEUbf8MFSA9UWVc0Xe3urRqtXVH6wiF7qZkfmFSMnSA+5rebPPxn+BLaMZCpbiOp/EKOZG5qTyHxdkvT8dfytZK0Oqu+1nlcTzlEf7zDve5dMPzF0MhNoT0/4vlOEKqx8gEcA9SNo+bY81mh/Vw0R3FqK8YTXxxTEsBtXD3xCvsWmgJGKbpKm7VZP49gYKGfQruR25tBiNopkPeiAK4UjDiQ7lzfsEN9xWy6g6B8Dp/6fEkKTxHlK5EUwPpszCZDhSwaRYhz6j+W7JeO9LC/LJfZ4dS3+iMTdxtodC2Rrm3Da1ZSPRGQiwTGElOAhc5XJYkdIrv3h+G3ObViRXevP0iLpiC8lf3DRRoPAy8fdQwQCU7ezn9Mdsj9MeezTYyEwp3mCXWx9ZrHfBl8Tpuj0enllQIjpcGWHaZZ0Pce31sRoi8vaoQCRu3pbm4cKKwU/sYlQNBX0D1DE2ZQB6C2NvtwdIU2AZcYvGjXDGaZ5YRh1+Kq/vvRISvQFSbl5uUPMpfgTqhDPYwjIfsEoVv9Rki9dFh4xLuk3n8Mo6lWm+BcQUNPG2fz+8MH3Hd2QfCNNynHEJAbT2KFh5VIKwzoughUBZR9r7VmSoYg2l3YXb5hg==</AUTHENTICATEDDATA>
  </BODY>
  <SIGNATURE>
    <ALGORITHM>RSA PKCS#1-V1.5</ALGORITHM>
    <DIGEST>
      <ALGORITHM>SHA1</ALGORITHM>
      <PARAMETER name="codingtype">
        <VALUE encoding="string">surface-coding</VALUE>
      </PARAMETER>
      <VALUE encoding="base64" size="160">0zkJi3NJke+OehTek7lsFT675JU=</VALUE>
    </DIGEST>
    <VALUE encoding="base64" size="1024">VObVcSvMdfw4pE7S2Q1o/oeE3d72yhuu42hZz5vpcg8elVfRPFrpxECWR1f9cNxSX6j32JJ9W1+vD2YNOeykWg9iIcbq0e82d/WTvkOs5eIFIWtqjGidOvnG7UCUYBktBiKJ5IDeo68P6GfsEtby8o54MlIztvWXeB5T21nhz2I=</VALUE>
  </SIGNATURE>
</XrML>
""";
        Assert.Throws<RightsManagementException>(() => new PublishLicense(PublishLicenseTemplate));
    }

    [Fact]
    public void AcquireUseLicense_NullSecureEnvironment_ThrowsArgumentNullException()
    {
        var license = new PublishLicense(PublishLicenseTemplate);
        Assert.Throws<ArgumentNullException>("secureEnvironment", () => license.AcquireUseLicense(null));
    }

    [Fact]
    public void AcquireUseLicenseNoUI_NullSecureEnvironment_ThrowsArgumentNullException()
    {
        var license = new PublishLicense(PublishLicenseTemplate);
        Assert.Throws<ArgumentNullException>("secureEnvironment", () => license.AcquireUseLicenseNoUI(null));
    }

    [Fact]
    public void DecryptUnsignedPublishLicense_Invoke_Success()
    {
        _ = new PublishLicense(PublishLicenseTemplate);
    }

    [Fact]
    public void DecryptUnsignedPublishLicense_NullCryptoProvider_ThrowsArgumentNullException()
    {
        var license = new PublishLicense(PublishLicenseTemplate);
        Assert.Throws<ArgumentNullException>("cryptoProvider", () => license.DecryptUnsignedPublishLicense(null));
    }
}
