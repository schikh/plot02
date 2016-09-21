/* 
	Copyright 1988-2000 by Autodesk, Inc.

	Permission to use, copy, modify, and distribute this software
	for any purpose and without fee is hereby granted, provided
	that the above copyright notice appears in all copies and
	that both that copyright notice and the limited warranty and
	restricted rights notice below appear in all supporting
	documentation.

	AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
	AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
	MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
	DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
	UNINTERRUPTED OR ERROR FREE.

	Use, duplication, or disclosure by the U.S. Government is subject to
	restrictions set forth in FAR 52.227-19 (Commercial Computer
	Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii) 
	(Rights in Technical Data and Computer Software), as applicable.

    Autodesk Publish to Web JavaScript 

    Autodesk® Express Viewer detection    
*/

parent.document.adsk_ptw_viewer_validate = true;

function adsk_ptw_validate_viewer_gfunc(version)
{ 
    if (parent != null)  {
        if (!parent.document.adsk_ptw_viewer_validate) 
            return;

        parent.document.adsk_ptw_viewer_validate = false;
    }

	if (!adsk_ptw_validate_checkViewerVersion (version)) {
        parent.window.navigate(xmsg_adsk_ptw_all_validate_viewer_url);
        return;
	}
	
 	return;
}

function adsk_ptw_onerror()
{
	if (!adsk_ptw_validate_checkViewerVersion(null))
	parent.window.navigate(xmsg_adsk_ptw_all_validate_viewer_url);
}

function adsk_ptw_validate_checkViewerVersion(version)
{ 
	if ((version == null) || (version < 3.0)) {
		return !window.confirm(xmsg_adsk_ptw_all_validate_viewer);
	}
	
	return true;
}

function  adsk_ptw_validate_viewer_is_dwf_file(file_name) {
    var ext = file_name.substring(file_name.lastIndexOf('.') + 1, (file_name.length));
    return("dwf" == ext || "dwfx" == ext);
}

// SIG // Begin signature block
// SIG // MIIUkAYJKoZIhvcNAQcCoIIUgTCCFH0CAQExDjAMBggq
// SIG // hkiG9w0CBQUAMGYGCisGAQQBgjcCAQSgWDBWMDIGCisG
// SIG // AQQBgjcCAR4wJAIBAQQQEODJBs441BGiowAQS9NQkAIB
// SIG // AAIBAAIBAAIBAAIBADAgMAwGCCqGSIb3DQIFBQAEEFkC
// SIG // kjl6JPGAuTRh1nXOWRqggg+PMIICvDCCAiUCEEoZ0jiM
// SIG // glkcpV1zXxVd3KMwDQYJKoZIhvcNAQEEBQAwgZ4xHzAd
// SIG // BgNVBAoTFlZlcmlTaWduIFRydXN0IE5ldHdvcmsxFzAV
// SIG // BgNVBAsTDlZlcmlTaWduLCBJbmMuMSwwKgYDVQQLEyNW
// SIG // ZXJpU2lnbiBUaW1lIFN0YW1waW5nIFNlcnZpY2UgUm9v
// SIG // dDE0MDIGA1UECxMrTk8gTElBQklMSVRZIEFDQ0VQVEVE
// SIG // LCAoYyk5NyBWZXJpU2lnbiwgSW5jLjAeFw05NzA1MTIw
// SIG // MDAwMDBaFw0wNDAxMDcyMzU5NTlaMIGeMR8wHQYDVQQK
// SIG // ExZWZXJpU2lnbiBUcnVzdCBOZXR3b3JrMRcwFQYDVQQL
// SIG // Ew5WZXJpU2lnbiwgSW5jLjEsMCoGA1UECxMjVmVyaVNp
// SIG // Z24gVGltZSBTdGFtcGluZyBTZXJ2aWNlIFJvb3QxNDAy
// SIG // BgNVBAsTK05PIExJQUJJTElUWSBBQ0NFUFRFRCwgKGMp
// SIG // OTcgVmVyaVNpZ24sIEluYy4wgZ8wDQYJKoZIhvcNAQEB
// SIG // BQADgY0AMIGJAoGBANMuIPBofCwtLoEcsQaypwu3EQ1X
// SIG // 2lPYdePJMyqy1PYJWzTz6ZD+CQzQ2xtauc3n9oixncCH
// SIG // Jet9WBBzanjLcRX9xlj2KatYXpYE/S1iEViBHMpxlNUi
// SIG // WC/VzBQFhDa6lKq0TUrp7jsirVaZfiGcbIbASkeXarSm
// SIG // NtX8CS3TtDmbAgMBAAEwDQYJKoZIhvcNAQEEBQADgYEA
// SIG // YVUOPnvHkhJ+ERCOIszUsxMrW+hE5At4nqR+86cHch7i
// SIG // We/MhOOJlEzbTmHvs6T7Rj1QNAufcFb2jip/F87lY795
// SIG // aQdzLrCVKIr17aqp0l3NCsoQCY/Os68olsR5KYSS3P+6
// SIG // Z0JIppAQ5L9h+JxT5ZPRcz/4/Z1PhKxV0f0RY2MwggOq
// SIG // MIIDE6ADAgECAhBKKT6dHYxAfxdJ/31hX451MA0GCSqG
// SIG // SIb3DQEBBQUAMF8xCzAJBgNVBAYTAlVTMRcwFQYDVQQK
// SIG // Ew5WZXJpU2lnbiwgSW5jLjE3MDUGA1UECxMuQ2xhc3Mg
// SIG // MyBQdWJsaWMgUHJpbWFyeSBDZXJ0aWZpY2F0aW9uIEF1
// SIG // dGhvcml0eTAeFw0wMTEyMTIwMDAwMDBaFw0wNDAxMDYy
// SIG // MzU5NTlaMIGpMRcwFQYDVQQKEw5WZXJpU2lnbiwgSW5j
// SIG // LjEfMB0GA1UECxMWVmVyaVNpZ24gVHJ1c3QgTmV0d29y
// SIG // azE7MDkGA1UECxMyVGVybXMgb2YgdXNlIGF0IGh0dHBz
// SIG // Oi8vd3d3LnZlcmlzaWduLmNvbS9ycGEgKGMpMDExMDAu
// SIG // BgNVBAMTJ1ZlcmlTaWduIENsYXNzIDMgQ29kZSBTaWdu
// SIG // aW5nIDIwMDEtNCBDQTCBnzANBgkqhkiG9w0BAQEFAAOB
// SIG // jQAwgYkCgYEAnoG1Ys2H82OZbSnKmKsRtbVGNLUilYKo
// SIG // e1b9Xg0YGyhjKUJJAxmGin3lUFFJ+pHaz7MOy3PEOOBA
// SIG // 5Go0sNzr6+DMw8qR2Nr7QNKF09rf4l8ulxnbntEI0H2F
// SIG // wCDOzIxxpuVNWj4ZlzD/yM76m0Y3vNL2zClfJ3OToaA4
// SIG // 3hScu6MCAwEAAaOCARowggEWMBIGA1UdEwEB/wQIMAYB
// SIG // Af8CAQAwRAYDVR0gBD0wOzA5BgtghkgBhvhFAQcXAzAq
// SIG // MCgGCCsGAQUFBwIBFhxodHRwczovL3d3dy52ZXJpc2ln
// SIG // bi5jb20vcnBhMDMGA1UdHwQsMCowKKImhiRodHRwOi8v
// SIG // Y3JsLnZlcmlzaWduLmNvbS9wY2EzLjEuMS5jcmwwHQYD
// SIG // VR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMDMA4GA1Ud
// SIG // DwEB/wQEAwIBBjARBglghkgBhvhCAQEEBAMCAAEwJAYD
// SIG // VR0RBB0wG6QZMBcxFTATBgNVBAMTDENsYXNzM0NBMS0x
// SIG // MzAdBgNVHQ4EFgQUT+u6lxTKm1OV7rF6TlSXDbUEoRww
// SIG // DQYJKoZIhvcNAQEFBQADgYEAWumXyXj/yYyx+PzeX9zk
// SIG // pD0cuf/TIcrXABFuJtFnKyZyWgbE1sPwWQQewgiuRpxG
// SIG // TtHSAW6amXe/1R3uHNwpqr3eBVHH8o0ZtdkK7Bum62q6
// SIG // SRhDU16W/MtpqAWNPgqLDkC8x1STQPy2a1cPoS/0ebVq
// SIG // J5C+e/yOp3xlSmQvHAEwggQCMIIDa6ADAgECAhAIem1c
// SIG // b2KTT7rE/UPhFBidMA0GCSqGSIb3DQEBBAUAMIGeMR8w
// SIG // HQYDVQQKExZWZXJpU2lnbiBUcnVzdCBOZXR3b3JrMRcw
// SIG // FQYDVQQLEw5WZXJpU2lnbiwgSW5jLjEsMCoGA1UECxMj
// SIG // VmVyaVNpZ24gVGltZSBTdGFtcGluZyBTZXJ2aWNlIFJv
// SIG // b3QxNDAyBgNVBAsTK05PIExJQUJJTElUWSBBQ0NFUFRF
// SIG // RCwgKGMpOTcgVmVyaVNpZ24sIEluYy4wHhcNMDEwMjI4
// SIG // MDAwMDAwWhcNMDQwMTA2MjM1OTU5WjCBoDEXMBUGA1UE
// SIG // ChMOVmVyaVNpZ24sIEluYy4xHzAdBgNVBAsTFlZlcmlT
// SIG // aWduIFRydXN0IE5ldHdvcmsxOzA5BgNVBAsTMlRlcm1z
// SIG // IG9mIHVzZSBhdCBodHRwczovL3d3dy52ZXJpc2lnbi5j
// SIG // b20vcnBhIChjKTAxMScwJQYDVQQDEx5WZXJpU2lnbiBU
// SIG // aW1lIFN0YW1waW5nIFNlcnZpY2UwggEiMA0GCSqGSIb3
// SIG // DQEBAQUAA4IBDwAwggEKAoIBAQDAemGH67KnA2MbKxph
// SIG // 3oC3FR2gi5A9uyeShBQ564XOKZIGZkikA0+N6E+n8K9e
// SIG // 0S8Zx5HxtZ57kSHO6f/jTvD8r5VYuGMt5o72KRjNcI5Q
// SIG // w+2Wu0DbviXoQlXW9oXyBueLmRwx8wMP1EycJCrcGxuP
// SIG // gvOw76dN4xSn4I/Wx2jCYVipctT4MEhP2S9vYyDZicqC
// SIG // e8JLvCjFgWjn5oJArEY6oPk/Ns1Mu1RCWnple/6E5MdH
// SIG // VKy5PeyAxxr3xDOBgckqlft/XjqHkBTbzC518u9r5j2p
// SIG // YL5CAapPqluoPyIxnxIV+XOhHoKLBCvqRgJMbY8fUC6V
// SIG // Syp4BoR0PZGPLEcxAgMBAAGjgbgwgbUwQAYIKwYBBQUH
// SIG // AQEENDAyMDAGCCsGAQUFBzABhiRodHRwOi8vb2NzcC52
// SIG // ZXJpc2lnbi5jb20vb2NzcC9zdGF0dXMwCQYDVR0TBAIw
// SIG // ADBEBgNVHSAEPTA7MDkGC2CGSAGG+EUBBwEBMCowKAYI
// SIG // KwYBBQUHAgEWHGh0dHBzOi8vd3d3LnZlcmlzaWduLmNv
// SIG // bS9ycGEwEwYDVR0lBAwwCgYIKwYBBQUHAwgwCwYDVR0P
// SIG // BAQDAgbAMA0GCSqGSIb3DQEBBAUAA4GBAC3zT2NgLBja
// SIG // 9SQPUrMM67O8Z4XCI+2PRg3PGk2+83x6IDAyGGiLkrsy
// SIG // mfCTuDsVBid7PgIGAKQhkoQTCsWY5UBXxQUl6K+vEWqp
// SIG // 5TvL6SP2lCldQFXzpVOdyDY6OWUIc3OkMtKvrL/HBTz/
// SIG // RezD6Nok0c5jrgmn++Ib4/1BCmqWMIIFFzCCBICgAwIB
// SIG // AgIQIQ/ItWeoaJ+iNv1eJpFWIjANBgkqhkiG9w0BAQQF
// SIG // ADCBqTEXMBUGA1UEChMOVmVyaVNpZ24sIEluYy4xHzAd
// SIG // BgNVBAsTFlZlcmlTaWduIFRydXN0IE5ldHdvcmsxOzA5
// SIG // BgNVBAsTMlRlcm1zIG9mIHVzZSBhdCBodHRwczovL3d3
// SIG // dy52ZXJpc2lnbi5jb20vcnBhIChjKTAxMTAwLgYDVQQD
// SIG // EydWZXJpU2lnbiBDbGFzcyAzIENvZGUgU2lnbmluZyAy
// SIG // MDAxLTQgQ0EwHhcNMDIwOTA5MDAwMDAwWhcNMDMwOTIy
// SIG // MjM1OTU5WjCByzELMAkGA1UEBhMCVVMxEzARBgNVBAgT
// SIG // CkNhbGlmb3JuaWExEzARBgNVBAcTClNhbiBSYWZhZWwx
// SIG // FjAUBgNVBAoUDUF1dG9kZXNrLCBJbmMxPjA8BgNVBAsT
// SIG // NURpZ2l0YWwgSUQgQ2xhc3MgMyAtIE1pY3Jvc29mdCBT
// SIG // b2Z0d2FyZSBWYWxpZGF0aW9uIHYyMSIwIAYDVQQLFBlE
// SIG // ZXNpZ24gU29sdXRpb25zIERpdmlzaW9uMRYwFAYDVQQD
// SIG // FA1BdXRvZGVzaywgSW5jMIGfMA0GCSqGSIb3DQEBAQUA
// SIG // A4GNADCBiQKBgQDn89FlMMksQAE/S7d+0QDhHbq6ZLxl
// SIG // ptXbBUs9y6kgKaY2WiG7cKNAd1oekhX4drb/CLwYSBJ5
// SIG // wexMmEsD4b1Nqt5PGyK1GpwyVV28k5UXNDVzTemnlnxL
// SIG // hn6y+iALPLFx1Lh+pbGt/OG3DXyOFGpXdthmVFminB3A
// SIG // WqKuIoSIlQIDAQABo4ICGjCCAhYwCQYDVR0TBAIwADAO
// SIG // BgNVHQ8BAf8EBAMCB4AwRAYDVR0fBD0wOzA5oDegNYYz
// SIG // aHR0cDovL2NybC52ZXJpc2lnbi5jb20vQ2xhc3MzQ29k
// SIG // ZVNpZ25pbmdDQTIwMDEuY3JsMIGgBgNVHSAEgZgwgZUw
// SIG // gZIGC2CGSAGG+EUBBwEBMIGCMDMGCCsGAQUFBwIBFido
// SIG // dHRwczovL3d3dy52ZXJpc2lnbi5jb20vcmVwb3NpdG9y
// SIG // eS9DUFMwSwYIKwYBBQUHAgIwPxo9VmVyaVNpZ24ncyBD
// SIG // UFMgaW5jb3JwLiBieSByZWZlcmVuY2UgbGlhYi4gbHRk
// SIG // LiAoYyk5OSBWZXJpU2lnbjATBgNVHSUEDDAKBggrBgEF
// SIG // BQcDAzA1BggrBgEFBQcBAQQpMCcwJQYIKwYBBQUHMAGG
// SIG // GWh0dHBzOi8vb2NzcC52ZXJpc2lnbi5jb20wgZgGA1Ud
// SIG // IwSBkDCBjYAUT+u6lxTKm1OV7rF6TlSXDbUEoRyhY6Rh
// SIG // MF8xCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5WZXJpU2ln
// SIG // biwgSW5jLjE3MDUGA1UECxMuQ2xhc3MgMyBQdWJsaWMg
// SIG // UHJpbWFyeSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eYIQ
// SIG // Sik+nR2MQH8XSf99YV+OdTARBglghkgBhvhCAQEEBAMC
// SIG // BBAwFgYKKwYBBAGCNwIBGwQIMAYBAQABAf8wDQYJKoZI
// SIG // hvcNAQEEBQADgYEAR/N0E/CyozBkCNlYnH+I3qmVveNR
// SIG // 9gJ8dSmHiXpLPn1l68xDVsQjvL5OEQy0cDBLLuZ2+Ucm
// SIG // yi65WBeTJVqeUJsb2pTJDZEyGqD0vEaYWPAJ71+BO36i
// SIG // MOjM4h/ZQCIR6iKUozbRpnc1n3G+AA0G0QKWnricAnNp
// SIG // mt5ak07ew3IxggRrMIIEZwIBATCBvjCBqTEXMBUGA1UE
// SIG // ChMOVmVyaVNpZ24sIEluYy4xHzAdBgNVBAsTFlZlcmlT
// SIG // aWduIFRydXN0IE5ldHdvcmsxOzA5BgNVBAsTMlRlcm1z
// SIG // IG9mIHVzZSBhdCBodHRwczovL3d3dy52ZXJpc2lnbi5j
// SIG // b20vcnBhIChjKTAxMTAwLgYDVQQDEydWZXJpU2lnbiBD
// SIG // bGFzcyAzIENvZGUgU2lnbmluZyAyMDAxLTQgQ0ECECEP
// SIG // yLVnqGifojb9XiaRViIwDAYIKoZIhvcNAgUFAKCBsDAZ
// SIG // BgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEE
// SIG // AYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAfBgkqhkiG9w0B
// SIG // CQQxEgQQ1fy+G0hwaoLKr3e8024/KjBUBgorBgEEAYI3
// SIG // AgEMMUYwRKAmgCQAQQB1AHQAbwBkAGUAcwBrACAAQwBv
// SIG // AG0AcABvAG4AZQBuAHShGoAYaHR0cDovL3d3dy5hdXRv
// SIG // ZGVzay5jb20gMA0GCSqGSIb3DQEBAQUABIGAKo19SBai
// SIG // 2gfBjLPbYMbaD82dA3oslpLbDKIQse+CnK231XlktLv0
// SIG // ujGlANUqWjXXrBAZoxSC+UD3lDc5U5B/XZwnqnLaL/5e
// SIG // zRFfzUgIA6M6yeRxYcxSM3T2hMOiynwbiuzb42+MyF3D
// SIG // Obme5aCnyAK0fI1UWcTCeCSHxlFDZk+hggJMMIICSAYJ
// SIG // KoZIhvcNAQkGMYICOTCCAjUCAQEwgbMwgZ4xHzAdBgNV
// SIG // BAoTFlZlcmlTaWduIFRydXN0IE5ldHdvcmsxFzAVBgNV
// SIG // BAsTDlZlcmlTaWduLCBJbmMuMSwwKgYDVQQLEyNWZXJp
// SIG // U2lnbiBUaW1lIFN0YW1waW5nIFNlcnZpY2UgUm9vdDE0
// SIG // MDIGA1UECxMrTk8gTElBQklMSVRZIEFDQ0VQVEVELCAo
// SIG // Yyk5NyBWZXJpU2lnbiwgSW5jLgIQCHptXG9ik0+6xP1D
// SIG // 4RQYnTAMBggqhkiG9w0CBQUAoFkwGAYJKoZIhvcNAQkD
// SIG // MQsGCSqGSIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMDMw
// SIG // MTMxMDY1NDAwWjAfBgkqhkiG9w0BCQQxEgQQYKGG6EnC
// SIG // CPgrHFtelyyKqTANBgkqhkiG9w0BAQEFAASCAQB+6L8s
// SIG // Cf52OQ4c40IhTqQ1POR1FrkgHWSXw2S4ShLitklZyYk7
// SIG // yvSlIS3LwrG6IttIcoIL/+/4DzFFt8BB8GvRVe4fl/RO
// SIG // KinXsoO9AkujIG83BHNDZHAM9y4jbHdcRufLKxtpc9v4
// SIG // 1fBwBjyKZgQAFKnW6lNWlqlQjthtiMLFQp63L7cmJ/W+
// SIG // u4xjhmOFgCua7MXAlhy3VqLCypaifrbW4zJnKKgl3XTU
// SIG // G77DCtiKXNn+R9YB6mGdB+cmzFxRu8LtuwSfB2ru35R9
// SIG // dz3fN61ok115+tnXfDrOZOk0MM7eLK4wtIC/Zt0tP/Qu
// SIG // sOftR0IQJYRZg6IB5c4+oj0nHOnA
// SIG // End signature block
