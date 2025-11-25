# wsaa-cliente-noopenssl.ps1
# Autor: Gustavo Larriera (AFIP, 2019)
#
# Descripción:
#
# Ejemplo de cliente del WSAA.
# Consume el metodo LoginCms ejecutando desde la Powershell de Windows.
# Muestra el login ticket response.
#
# Argumentos de linea de comandos:
#
# $Certificado: Ruta del certificado firmante a usar (formato .pfx, .p12)
# $Password: Clave del certificado
# $ServicioId: ID de servicio a acceder
# $OutXml: Archivo TRA a crear
# $OutCms: Archivo CMS a crear
# $WsaaWsdl: URL del WSDL del WSAA

[CmdletBinding()]
Param(
    [Parameter(Mandatory = $False)]
    [string]$Certificado = "W:\cert.p12",
    [Parameter(Mandatory = $False)]
    [string]$Password = "261194",
    [Parameter(Mandatory = $False)]
    [string]$ServicioId = "wsfe",
    [Parameter(Mandatory = $False)]
    [string]$OutXml = "LoginTicketRequest.xml",
    [Parameter(Mandatory = $False)]
    [string]$OutCms = "LoginTicketRequest.xml.cms",
    [Parameter(Mandatory = $False)]
    [string]$WsaaWsdl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms?wsdl"
)

# Acción preferida si hay algún error
$ErrorActionPreference = "Stop"
# Usar biblioteca de criptografía de .NET Framework
Add-Type -Path "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Security.dll"

$dtNow = Get-Date
$xmlTA = New-Object System.XML.XMLDocument
$xmlTA.LoadXml('<loginTicketRequest><header><uniqueId></uniqueId>
<generationTime></generationTime><expirationTime></expirationTime>
</header><service></service></loginTicketRequest>')
$xmlUniqueId = $xmlTA.SelectSingleNode("//uniqueId")
$xmlGenTime = $xmlTA.SelectSingleNode("//generationTime")
$xmlExpTime = $xmlTA.SelectSingleNode("//expirationTime")
$xmlService = $xmlTA.SelectSingleNode("//service")
$xmlGenTime.InnerText = $dtNow.AddMinutes(-10).ToString("s")
$xmlExpTime.InnerText = $dtNow.AddMinutes(+10).ToString("s")
$xmlUniqueId.InnerText = $dtNow.ToString("yyMMddHHMM")
$xmlService.InnerText = $ServicioId
$seqNr = Get-Date -UFormat "%Y%m%d%H%S"
$xmlTA.InnerXml | Out-File $seqNr-$OutXml -Encoding ASCII
$cer = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Certificado, $Password)
$encoding = [System.Text.Encoding]::UTF8
[System.Byte[]]$msgBytes = $encoding.GetBytes($xmlTA.InnerXml)
[System.Security.Cryptography.Pkcs.ContentInfo]$contentInfo = [System.Security.Cryptography.Pkcs.ContentInfo]::new($msgBytes)
[System.Security.Cryptography.Pkcs.SignedCms]$signedCms = [System.Security.Cryptography.Pkcs.SignedCms]::new($contentInfo)
[System.Security.Cryptography.Pkcs.CmsSigner]$cmsSigner = [System.Security.Cryptography.Pkcs.CmsSigner]::new([System.Security.Cryptography.Pkcs.SubjectIdentifierType]::IssuerAndSerialNumber, $cer)
$cmsSigner.IncludeOption = [System.Security.Cryptography.X509Certificates.X509IncludeOption]::EndCertOnly
$signedCms.ComputeSignature($cmsSigner)
[System.Byte[]]$encodedSignedCms = $signedCms.Encode()
$signedCmsBase64 = [System.Convert]::ToBase64String($encodedSignedCms)

try {
    Write-Host "`n=== PREPARANDO LLAMADA SOAP ===" -ForegroundColor Cyan
    
    # Crear el sobre SOAP manualmente
    $soapEnvelope = @"
<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:wsaa="http://wsaa.view.sua.dvadac.desein.afip.gov">
   <soapenv:Header/>
   <soapenv:Body>
      <wsaa:loginCms>
         <wsaa:in0>$signedCmsBase64</wsaa:in0>
      </wsaa:loginCms>
   </soapenv:Body>
</soapenv:Envelope>
"@

    # URL del servicio (sin ?wsdl)
    $wsaaUrl = $WsaaWsdl -replace '\?wsdl$', ''
    
    Write-Host "URL del servicio: $wsaaUrl" -ForegroundColor Yellow
    Write-Host "Enviando petición SOAP..." -ForegroundColor Yellow
    
    # Guardar el SOAP request para debug
    $soapEnvelope | Out-File "$seqNr-SOAP-REQUEST.xml" -Encoding UTF8
    Write-Host "SOAP Request guardado en: $seqNr-SOAP-REQUEST.xml" -ForegroundColor Gray
    
    # Crear la petición HTTP
    $webRequest = [System.Net.WebRequest]::Create($wsaaUrl)
    $webRequest.Method = "POST"
    $webRequest.ContentType = "text/xml; charset=utf-8"
    $webRequest.Headers.Add("SOAPAction", '""')
    
    # Convertir el SOAP a bytes
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($soapEnvelope)
    $webRequest.ContentLength = $bytes.Length
    
    # Enviar el SOAP
    $requestStream = $webRequest.GetRequestStream()
    $requestStream.Write($bytes, 0, $bytes.Length)
    $requestStream.Close()
    
    # Obtener la respuesta
    try {
        $response = $webRequest.GetResponse()
        $responseStream = $response.GetResponseStream()
        $streamReader = New-Object System.IO.StreamReader($responseStream)
        $soapResponse = $streamReader.ReadToEnd()
        $streamReader.Close()
        $responseStream.Close()
        $response.Close()
        
        Write-Host "`n=== RESPUESTA SOAP RECIBIDA ===" -ForegroundColor Green
        
        # Parsear la respuesta SOAP
        [xml]$xmlResponse = $soapResponse
        
        # Verificar si es un SOAP Fault
        if ($xmlResponse.Envelope.Body.Fault) {
            $fault = $xmlResponse.Envelope.Body.Fault
            Write-Host "`n=== SOAP FAULT (ERROR DE AFIP) ===" -ForegroundColor Red
            Write-Host "Código: $($fault.faultcode)" -ForegroundColor Yellow
            Write-Host "Mensaje: $($fault.faultstring)" -ForegroundColor Yellow
            
            if ($fault.detail.exceptionName) {
                Write-Host "Excepción: $($fault.detail.exceptionName)" -ForegroundColor Yellow
            }
            if ($fault.detail.hostname) {
                Write-Host "Servidor: $($fault.detail.hostname)" -ForegroundColor Yellow
            }
            
            # Guardar el fault
            $soapResponse | Out-File "$seqNr-SOAP-FAULT-RESPONSE.xml" -Encoding UTF8
            Write-Host "`nSOAP Fault guardado en: $seqNr-SOAP-FAULT-RESPONSE.xml" -ForegroundColor Gray
            
            throw "SOAP Fault: $($fault.faultstring)"
        }
        
        # Si no es un fault, extraer el loginCmsReturn
        $loginCmsReturn = $xmlResponse.Envelope.Body.loginCmsResponse.loginCmsReturn
        
        # Guardar la respuesta
        $loginCmsReturn | Out-File "$seqNr-loginTicketResponse.xml" -Encoding UTF8
        $soapResponse | Out-File "$seqNr-loginTicketResponse-SOAP.xml" -Encoding UTF8
        
        Write-Host "`nLogin Ticket Response guardado en: $seqNr-loginTicketResponse.xml" -ForegroundColor Green
        Write-Host "Respuesta SOAP completa guardada en: $seqNr-loginTicketResponse-SOAP.xml" -ForegroundColor Green
        
        # Mostrar el resultado
        Write-Host "`n=== CONTENIDO DEL LOGIN TICKET ===" -ForegroundColor Cyan
        Write-Host $loginCmsReturn -ForegroundColor White
        
        $loginCmsReturn
    }
    catch [System.Net.WebException] {
        # Capturar errores HTTP (incluyendo 500 con SOAP Fault)
        Write-Host "`n=== RESPUESTA HTTP CON ERROR ===" -ForegroundColor Red
        
        $errorResponse = $_.Exception.Response
        if ($errorResponse) {
            $errorStream = $errorResponse.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            $errorContent = $errorReader.ReadToEnd()
            $errorReader.Close()
            $errorStream.Close()
            
            Write-Host "Código HTTP: $($errorResponse.StatusCode)" -ForegroundColor Yellow
            
            # Intentar parsear como SOAP Fault
            try {
                [xml]$xmlError = $errorContent
                if ($xmlError.Envelope.Body.Fault) {
                    $fault = $xmlError.Envelope.Body.Fault
                    Write-Host "`n=== SOAP FAULT (ERROR DE AFIP) ===" -ForegroundColor Red
                    Write-Host "Código: $($fault.faultcode)" -ForegroundColor Yellow
                    Write-Host "Mensaje: $($fault.faultstring)" -ForegroundColor Yellow
                    
                    if ($fault.detail.exceptionName) {
                        Write-Host "Excepción: $($fault.detail.exceptionName)" -ForegroundColor Yellow
                    }
                    if ($fault.detail.hostname) {
                        Write-Host "Servidor: $($fault.detail.hostname)" -ForegroundColor Yellow
                    }
                    
                    # Guardar el fault
                    $errorContent | Out-File "$seqNr-SOAP-FAULT-RESPONSE.xml" -Encoding UTF8
                    Write-Host "`nSOAP Fault guardado en: $seqNr-SOAP-FAULT-RESPONSE.xml" -ForegroundColor Gray
                    
                    throw "SOAP Fault: $($fault.faultstring)"
                }
            }
            catch {
                # Si no se puede parsear, mostrar el contenido raw
                Write-Host "`nContenido de la respuesta:" -ForegroundColor Yellow
                Write-Host $errorContent -ForegroundColor White
                
                $errorContent | Out-File "$seqNr-SOAP-ERROR-RESPONSE.xml" -Encoding UTF8
                Write-Host "`nError guardado en: $seqNr-SOAP-ERROR-RESPONSE.xml" -ForegroundColor Gray
            }
        }
        
        throw
    }
}
catch {
    $errMsg = $_.Exception.Message
    Write-Host "`nERROR: $errMsg" -ForegroundColor Red
    $errMsg | Out-File "$seqNr-loginTicketResponse-ERROR.txt" -Encoding UTF8
    
    if ($_.Exception.InnerException) {
        Write-Host "Error interno: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
    
    $errMsg
}