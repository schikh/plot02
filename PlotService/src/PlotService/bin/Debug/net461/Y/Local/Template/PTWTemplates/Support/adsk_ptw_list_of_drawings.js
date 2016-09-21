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
    
    Template element id: adsk_ptw_list_of_drawings
    Publishing content:  label

    Template element id: adsk_ptw_image_description
    Publishing content:  description

    Template element id: adsk_ptw_image
    Publishing content:  image

    Template element id: adsk_ptw_idrop
    Publishing content:  idrop

    Template element id: adsk_ptw_summary_frame
    Publishing content:  drawing summary    
*/

adsk_ptw_list_of_drawings_main();

function adsk_ptw_list_of_drawings_main() {
    var xmle=adsk_ptw_xml.getElementsByTagName("publish_to_web").item(0);
    xmle=xmle.getElementsByTagName("contents").item(0);
    var xmles=xmle.getElementsByTagName("content");
    var e = document.getElementById("adsk_ptw_list_of_drawings");

    table = document.createElement("table");
    tbody = document.createElement("tbody");
    e.appendChild(table);
    table.appendChild(tbody);
    tr = document.createElement("tr");
    tbody.appendChild(tr);
    tr.align="left";
    tr.vAlign="top"

    td = document.createElement("td"); 
    tr.appendChild(td);
    table2=document.createElement("table"); 
    td.appendChild(table2);
    table2.cellPadding=1;
    table2.cellSpacing=5;

    tbody2=document.createElement("tbody");
    table2.appendChild(tbody2);
    td2 = document.createElement("td"); 
    tr.appendChild(td2);
    for (i=0; i < xmles.length; i++) {
        content=xmles.item(i);      
        title = content.getElementsByTagName("title").item(0);
        a=document.createElement("a"); 
        if (null == title.firstChild) {
            a.appendChild(document.createTextNode(" "));
        }
        else {
            a.appendChild(document.createTextNode(title.firstChild.text));
        }
        table2_tr=document.createElement("tr");
        tbody2.appendChild(table2_tr);
        table2_td=document.createElement("td");
        table2_tr.appendChild(table2_td);
        table2_td.appendChild(a);
        a.className="DRAWING_LABEL";
        var fileName = content.getElementsByTagName("image").item(0).firstChild.text;
        a.id=fileName;
        a.value=i;   
        if (adsk_ptw_list_of_drawings_is_image_dwf(fileName)) {  
            a.href="javascript:adsk_ptw_list_of_drawings_onClickViewer()";
        } else {
            a.href="javascript:adsk_ptw_list_of_drawings_onClickImage()";
        }
    }
}

function adsk_ptw_list_of_drawings_createViewerControl (i) {
    dwg_img_desc=parent.adsk_ptw_image_frame.document.getElementById("adsk_ptw_image_description");
	if (null != dwg_img_desc.firstChild)
		dwg_img_desc.removeChild(dwg_img_desc.firstChild);

    var xmle=adsk_ptw_xml.getElementsByTagName("publish_to_web").item(0);
    xmle=xmle.getElementsByTagName("contents").item(0);
    xmles=xmle.getElementsByTagName("content");
    desc = xmles.item(i).getElementsByTagName("description").item(0);
    p = parent.adsk_ptw_image_frame.document.createElement("p");
    if (null == desc.firstChild) {
        p.appendChild(parent.adsk_ptw_image_frame.document.createTextNode(""));
    }
    else {
        p.appendChild(parent.adsk_ptw_image_frame.document.createTextNode(desc.firstChild.text));
    }
    dwg_img_desc.appendChild(p);

    dwg_img=parent.adsk_ptw_image_frame.document.getElementById("adsk_ptw_image");
    if (null != dwg_img_desc.firstChild)
		dwg_img.removeChild(dwg_img.firstChild); 

    viewerInstalled = false;
	IE4plus = (document.all) ? true : false;
	if (IE4plus)
	    // try and catch work fine in IE, but will generate errors in Netscape. So evaluating try and catch block as string and evaluate it usung eval fuction
  	    eval ('try {var xObj = new ActiveXObject("AdView.Adviewer");if (xObj == null) viewerInstalled = false; else viewerInstalled = true; } catch (e) { viewerInstalled = false; }');

    activex = parent.adsk_ptw_image_frame.document.createElement("object");
    dwg_img.appendChild(activex);
    activex.classid="clsid:A662DA7E-CCB7-4743-B71A-D817F6D575DF";

    var fileName=xmles.item(i).getElementsByTagName("image").item(0).firstChild.text;
    activex.id="AdView";
    activex.SourcePath=fileName;
    if (viewerInstalled && activex.Viewer && activex.Viewer.ToolbarVisible)
        activex.Viewer.ToolbarVisible=false;
    activex.border="1";
    activex.width="500";
    activex.height="360";

	if (!viewerInstalled)
		adsk_ptw_onerror(); // Redirects the user to a website where they can download Autodest Express Viewer

    adsk_ptw_list_of_drawings_setiDrop(i);
}

function adsk_ptw_list_of_drawings_onClickViewer() { 
    adsk_ptw_list_of_drawings_createViewerControl (document.activeElement.value);
    adsk_ptw_list_of_drawings_set_summary_info(document.activeElement.value);
}

function adsk_ptw_list_of_drawings_createImageElement(i) {
    dwg_img_desc=parent.adsk_ptw_image_frame.document.getElementById("adsk_ptw_image_description");
    if (null != dwg_img_desc.firstChild)
		dwg_img_desc.removeChild(dwg_img_desc.firstChild);

    var xmle=adsk_ptw_xml.getElementsByTagName("publish_to_web").item(0);
    xmle=xmle.getElementsByTagName("contents").item(0);
    var xmles=xmle.getElementsByTagName("content");
    desc = xmles.item(i).getElementsByTagName("description").item(0);
    p = parent.adsk_ptw_image_frame.document.createElement("p");
    if (null == desc.firstChild) {
        p.appendChild(parent.adsk_ptw_image_frame.document.createTextNode(""));
    }
    else {
        p.appendChild(parent.adsk_ptw_image_frame.document.createTextNode(desc.firstChild.text));
    }
    dwg_img_desc.appendChild(p);

    dwg_img=parent.adsk_ptw_image_frame.document.getElementById("adsk_ptw_image");
	dwg_img.removeChild(dwg_img.firstChild); 
    image = parent.adsk_ptw_image_frame.document.createElement("img");
    dwg_img.appendChild(image);
    image.src=xmles.item(i).getElementsByTagName("image").item(0).firstChild.text;
    image.border=1;

    adsk_ptw_list_of_drawings_setiDrop(i);
}

function adsk_ptw_list_of_drawings_onClickImage() {
    adsk_ptw_list_of_drawings_createImageElement(document.activeElement.value);
    adsk_ptw_list_of_drawings_set_summary_info(document.activeElement.value);
}

function adsk_ptw_list_of_drawings_setiDrop(i) {
    dwg_idrop = parent.adsk_ptw_image_frame.document.getElementById("adsk_ptw_idrop");
    if (null != dwg_idrop.firstChild) {
	    dwg_idrop.removeChild(dwg_idrop.firstChild); 
    }
    var xmle=adsk_ptw_xml.getElementsByTagName("publish_to_web").item(0);
    xmle=xmle.getElementsByTagName("contents").item(0);
    var xmles=xmle.getElementsByTagName("content");
    idrop = xmles.item(i).getElementsByTagName("iDropXML").item(0);
    if (null != idrop.firstChild) {
        activex = parent.adsk_ptw_image_frame.document.createElement("object");
        dwg_idrop.appendChild(activex);
        activex.codeBase=xmsg_adsk_ptw_all_idrop_url;
        activex.classid="clsid:21E0CB95-1198-4945-A3D2-4BF804295F78";
        activex.package=idrop.firstChild.text;
        activex.background="iDropButton.gif";
        activex.width="16";
        activex.height="16";
    }
}

function adsk_ptw_list_of_drawings_set_summary_info(i) {
    if (null == parent) return;
    if (null == parent.adsk_ptw_summary_frame) return;

    body=parent.adsk_ptw_summary_frame.document.getElementsByTagName("body").item(0);
    n=body.childNodes.length;
    for (index=0; index < n; index++) {
        body.removeChild(body.firstChild);
    }

    var xmle=adsk_ptw_xml.getElementsByTagName("publish_to_web").item(0);
    xmle=xmle.getElementsByTagName("contents").item(0);
    var xmles=xmle.getElementsByTagName("content");
    sum_info = xmles.item(i).getElementsByTagName("summary_info").item(0);

    if (null != sum_info) {
        body=parent.adsk_ptw_summary_frame.document.getElementsByTagName("body").item(0);

        title=sum_info.getElementsByTagName("title").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summaryTitle, title);

        subject=sum_info.getElementsByTagName("subject").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summarySubject, subject);

        author=sum_info.getElementsByTagName("author").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summaryAuthor, author);

        keywords=sum_info.getElementsByTagName("keywords").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summaryKeywords, keywords);

        comments=sum_info.getElementsByTagName("comments").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summaryComments, comments);
        
        hyperlink_base=sum_info.getElementsByTagName("hyperlink_base").item(0);
        adsk_ptw_list_of_drawings_summary(body, xmsg_adsk_ptw_all_summaryHyperlinkBase, hyperlink_base);
    }
}

function adsk_ptw_list_of_drawings_summary(rootNode, nameString, valueNode) {
    if (null == valueNode) return;
    if (null == valueNode.firstChild) return;

    b=parent.adsk_ptw_summary_frame.document.createElement("b");
    div=parent.adsk_ptw_summary_frame.document.createElement("div");
    rootNode.appendChild(div);
    div.appendChild(b);
    str = nameString + valueNode.firstChild.text;
    b.appendChild(parent.adsk_ptw_summary_frame.document.createTextNode(str));
}

function adsk_ptw_list_of_drawings_is_image_dwf(file_name) {
    var ext = file_name.substring(file_name.lastIndexOf('.') + 1, (file_name.length));
    return("dwf" == ext || "dwfx" == ext);
}


// SIG // Begin signature block
// SIG // MIIUZwYJKoZIhvcNAQcCoIIUWDCCFFQCAQExDjAMBggq
// SIG // hkiG9w0CBQUAMGYGCisGAQQBgjcCAQSgWDBWMDIGCisG
// SIG // AQQBgjcCAR4wJAIBAQQQEODJBs441BGiowAQS9NQkAIB
// SIG // AAIBAAIBAAIBAAIBADAgMAwGCCqGSIb3DQIFBQAEEFUK
// SIG // r19+ijsr0Q3lvHLa8ESggg+1MIIDpjCCAw+gAwIBAgIQ
// SIG // baJ66Skutt3AqAAdR247aTANBgkqhkiG9w0BAQUFADBf
// SIG // MQswCQYDVQQGEwJVUzEXMBUGA1UEChMOVmVyaVNpZ24s
// SIG // IEluYy4xNzA1BgNVBAsTLkNsYXNzIDMgUHVibGljIFBy
// SIG // aW1hcnkgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwHhcN
// SIG // MDExMjAzMDAwMDAwWhcNMTExMjAyMjM1OTU5WjCBpzEX
// SIG // MBUGA1UEChMOVmVyaVNpZ24sIEluYy4xHzAdBgNVBAsT
// SIG // FlZlcmlTaWduIFRydXN0IE5ldHdvcmsxOzA5BgNVBAsT
// SIG // MlRlcm1zIG9mIHVzZSBhdCBodHRwczovL3d3dy52ZXJp
// SIG // c2lnbi5jb20vcnBhIChjKTAxMS4wLAYDVQQDEyVWZXJp
// SIG // U2lnbiBDbGFzcyAzIENvZGUgU2lnbmluZyAyMDAxIENB
// SIG // MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDEEet1
// SIG // 7eeYhJtwl3ZisWLB3nM/Uaa/oNEpjo/sHBfa44OJA2Ip
// SIG // 0hBY7d1lCTzixMdnF4ut3F1NxV5nGJZBNkS6+EQRL7Of
// SIG // fnh/sioWHmzIBQDZCSNsKM0FzB7bbmVq0Y8C6Qwdbodv
// SIG // ZayH3QZGi6BdtWJLFPsu7SyMvEDA3k5sNQIDAQABo4IB
// SIG // GDCCARQwEgYDVR0TAQH/BAgwBgEB/wIBADBEBgNVHSAE
// SIG // PTA7MDkGC2CGSAGG+EUBBxcDMCowKAYIKwYBBQUHAgEW
// SIG // HGh0dHBzOi8vd3d3LnZlcmlzaWduLmNvbS9ycGEwHQYD
// SIG // VR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMDMA4GA1Ud
// SIG // DwEB/wQEAwIBBjARBglghkgBhvhCAQEEBAMCAAEwJAYD
// SIG // VR0RBB0wG6QZMBcxFTATBgNVBAMTDENsYXNzM0NBMS0x
// SIG // MjAdBgNVHQ4EFgQUNSv0ZHOQnANtAw1nYGvdCxA8u2Ew
// SIG // MQYDVR0fBCowKDAmoCSgIoYgaHR0cDovL2NybC52ZXJp
// SIG // c2lnbi5jb20vcGNhMy5jcmwwDQYJKoZIhvcNAQEFBQAD
// SIG // gYEAIlhtOmR6nQz2YFKC7bCzxJYqxTOpAXRvs3bXhAos
// SIG // ZCeXMLoqsrqBmbsjh8UzkYAtN6fctIvOujmUdDuQfrYm
// SIG // EmNwIRb8OfaZb9ifGCUTiSREaeCUbxMsj+ewV5SRmv6j
// SIG // 9d0VA/eBGRMN9xdQJSRfruCThNYDLZBxNxXaMdJaxfgw
// SIG // ggPEMIIDLaADAgECAhBHvxmV341SRkP3221IDTGkMA0G
// SIG // CSqGSIb3DQEBBQUAMIGLMQswCQYDVQQGEwJaQTEVMBMG
// SIG // A1UECBMMV2VzdGVybiBDYXBlMRQwEgYDVQQHEwtEdXJi
// SIG // YW52aWxsZTEPMA0GA1UEChMGVGhhd3RlMR0wGwYDVQQL
// SIG // ExRUaGF3dGUgQ2VydGlmaWNhdGlvbjEfMB0GA1UEAxMW
// SIG // VGhhd3RlIFRpbWVzdGFtcGluZyBDQTAeFw0wMzEyMDQw
// SIG // MDAwMDBaFw0xMzEyMDMyMzU5NTlaMFMxCzAJBgNVBAYT
// SIG // AlVTMRcwFQYDVQQKEw5WZXJpU2lnbiwgSW5jLjErMCkG
// SIG // A1UEAxMiVmVyaVNpZ24gVGltZSBTdGFtcGluZyBTZXJ2
// SIG // aWNlcyBDQTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCC
// SIG // AQoCggEBAKnKsqTMzSCvCn2JrId18LRO8d/BD79nYb2j
// SIG // ZBzau/nKM6uEMIlYfozba902ng+/0ex48nemfm88v5Ov
// SIG // Dbpo9GyUyr1SLatIPfW21V1fGwKf+i9rHqT3o5qmGsgC
// SIG // 4X9MUuMOYOxAHH65Dd4/x7Tfh71femoxLgOZgROoRyDO
// SIG // MXMNVy3NeDQzlRKZErneaC+q5uPCiowqw4shh2a9g1hX
// SIG // b3W/PKomh13KEBU8n4TqVMEKbsT+xUrduQcRlyJ82z4n
// SIG // 0R547J8xyfHmIhnbxLNHQ5oaX6AekORe9e588X2rYgGP
// SIG // 9U0L3tAiVqiVza6Idq7uug3z5E3ZoPtooK4UO7OHwbsC
// SIG // AwEAAaOB2zCB2DA0BggrBgEFBQcBAQQoMCYwJAYIKwYB
// SIG // BQUHMAGGGGh0dHA6Ly9vY3NwLnZlcmlzaWduLmNvbTAS
// SIG // BgNVHRMBAf8ECDAGAQH/AgEAMEEGA1UdHwQ6MDgwNqA0
// SIG // oDKGMGh0dHA6Ly9jcmwudmVyaXNpZ24uY29tL1RoYXd0
// SIG // ZVRpbWVzdGFtcGluZ0NBLmNybDATBgNVHSUEDDAKBggr
// SIG // BgEFBQcDCDAOBgNVHQ8BAf8EBAMCAQYwJAYDVR0RBB0w
// SIG // G6QZMBcxFTATBgNVBAMTDFRTQTIwNDgtMS01MzANBgkq
// SIG // hkiG9w0BAQUFAAOBgQBKa/nqWMJEHDGJeZkrlr+CrAHW
// SIG // HEzNsIpYbt8IKaNeyMqTE+cEUg3vRycvADiw5MmTTprU
// SIG // ImIV9z83IU9wMYDxiziHs+jolwD+z1WWTiTSqSdOeq63
// SIG // YUHzKs7nydle3bsrhT61nbXZ4Vf/vrTFfvXPDJ7wl/4r
// SIG // 0ztSGxs4J/c/SjCCA/8wggLnoAMCAQICEA3pK/DU2CmI
// SIG // GDIFCV6adogwDQYJKoZIhvcNAQEFBQAwUzELMAkGA1UE
// SIG // BhMCVVMxFzAVBgNVBAoTDlZlcmlTaWduLCBJbmMuMSsw
// SIG // KQYDVQQDEyJWZXJpU2lnbiBUaW1lIFN0YW1waW5nIFNl
// SIG // cnZpY2VzIENBMB4XDTAzMTIwNDAwMDAwMFoXDTA4MTIw
// SIG // MzIzNTk1OVowVzELMAkGA1UEBhMCVVMxFzAVBgNVBAoT
// SIG // DlZlcmlTaWduLCBJbmMuMS8wLQYDVQQDEyZWZXJpU2ln
// SIG // biBUaW1lIFN0YW1waW5nIFNlcnZpY2VzIFNpZ25lcjCC
// SIG // ASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALJQ
// SIG // KEjd02h6hBhEZnVdfsS4n2Mm/z1DnHwROBAlVXPZdSdp
// SIG // /U65IFzTCvmgGyrtVVYhYdge2+S8M2vH792jN2WOG5MM
// SIG // tlMeXHxmNV8FikX+dk7fU4CigSCdrohcogj35TD57iI3
// SIG // TEIKzt/GH8TWVemBP7VSoyyqAXryoqqNNf6f5l1qBZ89
// SIG // a+O/lsD+zGD5QOcHoETrgVFupSrytooQKO2P3AaghlCa
// SIG // e0oIDTAdyhCea/fpWK4EqUCZsijojxasPONTb0vTNZ21
// SIG // b2Qds5Ysuz3neettevkW5iatr++ZU7dALJW4ear+1FKr
// SIG // KXR+Quw5HqJqFuZZuyRo2ACAQxCHgGsCAwEAAaOByjCB
// SIG // xzA0BggrBgEFBQcBAQQoMCYwJAYIKwYBBQUHMAGGGGh0
// SIG // dHA6Ly9vY3NwLnZlcmlzaWduLmNvbTAMBgNVHRMBAf8E
// SIG // AjAAMDMGA1UdHwQsMCowKKAmoCSGImh0dHA6Ly9jcmwu
// SIG // dmVyaXNpZ24uY29tL3Rzcy1jYS5jcmwwFgYDVR0lAQH/
// SIG // BAwwCgYIKwYBBQUHAwgwDgYDVR0PAQH/BAQDAgbAMCQG
// SIG // A1UdEQQdMBukGTAXMRUwEwYDVQQDEwxUU0EyMDQ4LTEt
// SIG // NTQwDQYJKoZIhvcNAQEFBQADggEBAId4cNpOUgEgW+B5
// SIG // yYIwxP25GZa9kQDDvc3NxvQO2P/5TcAzYjARxfV0G9SS
// SIG // 3l+cIBOxfEW+UM2D54AXg6cnk2cTRvvKuJhBA8ybUVsF
// SIG // i3+ob/MbUBskLvJpjWwi97vKFpXtDHTAaHfZ65lih8Fz
// SIG // kPiJdHojq6OYe5ex948pcU0udRtIQdrwtQ0gVNZ3oJeC
// SIG // Y2n9Cc+K8HW7CZvZ+RFVJpphMr56ArB7hr6iw4siLHjR
// SIG // NXa8knNc+bnmTBUKI8zk0tQ0LklAFTwPYHokxqVm75bP
// SIG // cOs+5/QNftzRfKN2cWnBnE9HMDUhsaKvGmI8K9mOqioH
// SIG // e9gYs1x74p2lb/48ia0wggQ8MIIDpaADAgECAhAZ5gjM
// SIG // xnvJzW1jPqmYePgyMA0GCSqGSIb3DQEBBQUAMIGnMRcw
// SIG // FQYDVQQKEw5WZXJpU2lnbiwgSW5jLjEfMB0GA1UECxMW
// SIG // VmVyaVNpZ24gVHJ1c3QgTmV0d29yazE7MDkGA1UECxMy
// SIG // VGVybXMgb2YgdXNlIGF0IGh0dHBzOi8vd3d3LnZlcmlz
// SIG // aWduLmNvbS9ycGEgKGMpMDExLjAsBgNVBAMTJVZlcmlT
// SIG // aWduIENsYXNzIDMgQ29kZSBTaWduaW5nIDIwMDEgQ0Ew
// SIG // HhcNMDMwOTA0MDAwMDAwWhcNMDQwOTIyMjM1OTU5WjCB
// SIG // yzELMAkGA1UEBhMCVVMxEzARBgNVBAgTCkNhbGlmb3Ju
// SIG // aWExEzARBgNVBAcTClNhbiBSYWZhZWwxFjAUBgNVBAoU
// SIG // DUF1dG9kZXNrLCBJbmMxPjA8BgNVBAsTNURpZ2l0YWwg
// SIG // SUQgQ2xhc3MgMyAtIE1pY3Jvc29mdCBTb2Z0d2FyZSBW
// SIG // YWxpZGF0aW9uIHYyMSIwIAYDVQQLFBlEZXNpZ24gU29s
// SIG // dXRpb25zIERpdmlzaW9uMRYwFAYDVQQDFA1BdXRvZGVz
// SIG // aywgSW5jMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKB
// SIG // gQC8FpGMN5nviYlfRV2vhL8uoj/WuIxR8uhVonmC85GT
// SIG // c9VaU4GSTwETK9+Vp7GNmjRt3eXpK/8ASV2UKy86xKmF
// SIG // iHwElbBOcgPSwKHTYQhu4Hj8dEte+gHdIqVDMDVnz8qM
// SIG // GzNvDhfjRWCL1nvp1AL7kSprrjwtGzD8JLAFf537SwID
// SIG // AQABo4IBQTCCAT0wCQYDVR0TBAIwADAOBgNVHQ8BAf8E
// SIG // BAMCB4AwQgYDVR0fBDswOTA3oDWgM4YxaHR0cDovL2Ny
// SIG // bC52ZXJpc2lnbi5jb20vQ2xhc3MzQ29kZVNpZ25pbmcy
// SIG // MDAxLmNybDBEBgNVHSAEPTA7MDkGC2CGSAGG+EUBBxcD
// SIG // MCowKAYIKwYBBQUHAgEWHGh0dHBzOi8vd3d3LnZlcmlz
// SIG // aWduLmNvbS9ycGEwEwYDVR0lBAwwCgYIKwYBBQUHAwMw
// SIG // NQYIKwYBBQUHAQEEKTAnMCUGCCsGAQUFBzABhhlodHRw
// SIG // czovL29jc3AudmVyaXNpZ24uY29tMB8GA1UdIwQYMBaA
// SIG // FDUr9GRzkJwDbQMNZ2Br3QsQPLthMBEGCWCGSAGG+EIB
// SIG // AQQEAwIEEDAWBgorBgEEAYI3AgEbBAgwBgEBAAEB/zAN
// SIG // BgkqhkiG9w0BAQUFAAOBgQBjbDo405Mt7DqWF6x0T5/m
// SIG // vS8I64c/D41ZuCWxoU1+DIcHXZVJFUUWy5AuYQBM2WDk
// SIG // d8CduexX8iEXGDjcayzNS344T4wJ9Dq7H8wJU8VjhsoB
// SIG // nPEhq7IrxYn/KUeGSc6obE9eCHvc8YBV4uybtXLDm2UX
// SIG // 1Dl0NOeHCPnVt0ok3TGCBBwwggQYAgEBMIG8MIGnMRcw
// SIG // FQYDVQQKEw5WZXJpU2lnbiwgSW5jLjEfMB0GA1UECxMW
// SIG // VmVyaVNpZ24gVHJ1c3QgTmV0d29yazE7MDkGA1UECxMy
// SIG // VGVybXMgb2YgdXNlIGF0IGh0dHBzOi8vd3d3LnZlcmlz
// SIG // aWduLmNvbS9ycGEgKGMpMDExLjAsBgNVBAMTJVZlcmlT
// SIG // aWduIENsYXNzIDMgQ29kZSBTaWduaW5nIDIwMDEgQ0EC
// SIG // EBnmCMzGe8nNbWM+qZh4+DIwDAYIKoZIhvcNAgUFAKCB
// SIG // sDAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgor
// SIG // BgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAfBgkqhkiG
// SIG // 9w0BCQQxEgQQM7rYDAZEyN3afrDQQY7kYDBUBgorBgEE
// SIG // AYI3AgEMMUYwRKAmgCQAQQB1AHQAbwBkAGUAcwBrACAA
// SIG // QwBvAG0AcABvAG4AZQBuAHShGoAYaHR0cDovL3d3dy5h
// SIG // dXRvZGVzay5jb20gMA0GCSqGSIb3DQEBAQUABIGAckow
// SIG // VI3zQHC6rRfVdboZ1lC8tJ26/jYDrm0AhjWQHJOzoucV
// SIG // a3xmkU7a2bxMfwY/ID0hw5srYsn2f0dgNH9BfJF/U0Nl
// SIG // uGj7qyKFF/sv/9trY1Atcp8Mg7CdzwVOCX2ivVkrDSlB
// SIG // +rt0Gfm9dQOkekDptk3RAKk15+bNM57J5OWhggH/MIIB
// SIG // +wYJKoZIhvcNAQkGMYIB7DCCAegCAQEwZzBTMQswCQYD
// SIG // VQQGEwJVUzEXMBUGA1UEChMOVmVyaVNpZ24sIEluYy4x
// SIG // KzApBgNVBAMTIlZlcmlTaWduIFRpbWUgU3RhbXBpbmcg
// SIG // U2VydmljZXMgQ0ECEA3pK/DU2CmIGDIFCV6adogwDAYI
// SIG // KoZIhvcNAgUFAKBZMBgGCSqGSIb3DQEJAzELBgkqhkiG
// SIG // 9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTA0MDEyNTIzMjMx
// SIG // M1owHwYJKoZIhvcNAQkEMRIEEDq4MpGQwLFO6PP/pooh
// SIG // J7kwDQYJKoZIhvcNAQEBBQAEggEAMNDRofpK8VbgCn8w
// SIG // 90GMpD4wrxdhXtNHlhfOZPcSQ8jgCVnp7x3UTeKg6mlR
// SIG // tqYuUuQHSkolOWZvVtp6DqwWA21TPX5frR0OF8ocOubi
// SIG // fXTbESgbX+yMJtHBMWyaDy2QTO+Ul+bqBlvPQg/raeFx
// SIG // ErBF5A8IkZSluZHAxI9pRG1iUrOpeMpcC9LmrfKcH5Ae
// SIG // ade8TKvjbh++596TpnRtfcT4JpJZnEt4TtSNr59UPZ5r
// SIG // TE3McFY/Om+5ni4nRZvQtfMPKgUbvPF2UmnO/vCq4oPl
// SIG // V/9HS0DyrTUEshGQZlSvLbjKtidmxHOeYnRIxQ6mD+8r
// SIG // AFkDgmPsNMG5OF1hoQ==
// SIG // End signature block
