$ftpHost = 'ftp.purewaveacoustic.co.za'
$ftpBase = '/purewaveacoustic.co.za/wwwroot'
$user = 'purewave'
$pass = 'V:nltDD*766sP8'
$publishDir = 'D:\Development\PureWave\PureWave.Web\bin\Release\net9.0\win-x64\publish'
$cred = New-Object System.Net.NetworkCredential($user, $pass)

function FtpUpload([string]$localPath, [string]$ftpPath) {
    $uri = 'ftp://' + $ftpHost + $ftpPath
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.Credentials = $cred
    $req.UsePassive = $true
    $req.UseBinary = $true
    $req.KeepAlive = $false
    $bytes = [System.IO.File]::ReadAllBytes($localPath)
    $req.ContentLength = $bytes.Length
    $s = $req.GetRequestStream()
    $s.Write($bytes, 0, $bytes.Length)
    $s.Close()
    $req.GetResponse().Close()
}

function FtpDelete([string]$ftpPath) {
    $uri = 'ftp://' + $ftpHost + $ftpPath
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $req.Credentials = $cred
    $req.UsePassive = $true
    $req.KeepAlive = $false
    $req.GetResponse().Close()
}

function FtpFileSize([string]$ftpPath) {
    try {
        $uri = 'ftp://' + $ftpHost + $ftpPath
        $req = [System.Net.FtpWebRequest]::Create($uri)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
        $req.Credentials = $cred
        $req.UsePassive = $true
        $req.UseBinary = $true
        $req.KeepAlive = $false
        $resp = $req.GetResponse()
        $size = $resp.ContentLength
        $resp.Close()
        return $size
    } catch { return -1 }
}

# Step 1: Upload app_offline.htm
Write-Host 'Uploading maintenance page...'
FtpUpload 'D:\Development\PureWave\app_offline.htm' ($ftpBase + '/app_offline.htm')
Write-Host 'Maintenance page live. Waiting 3s for app to shut down...'
Start-Sleep -Seconds 3

# Step 2: Upload changed files only (excluding app_offline.htm)
$files = Get-ChildItem -Path $publishDir -Recurse -File | Where-Object { $_.Name -ne 'app_offline.htm' }
$uploaded = 0
$skipped = 0
$errors = 0

foreach ($file in $files) {
    $relPath = $file.FullName.Substring($publishDir.Length).Replace('\', '/')
    $ftpPath = $ftpBase + $relPath
    $remoteSize = FtpFileSize $ftpPath
    if ($remoteSize -eq $file.Length) {
        $skipped++
        continue
    }
    try {
        FtpUpload $file.FullName $ftpPath
        $uploaded++
    } catch {
        Write-Host ('ERROR: ' + $relPath + ' - ' + $_)
        $errors++
    }
}

Write-Host ('Uploaded ' + $uploaded + ' files, skipped ' + $skipped + ' unchanged. Errors: ' + $errors)

# Step 3: Remove app_offline.htm
Write-Host 'Removing maintenance page...'
try {
    FtpDelete ($ftpBase + '/app_offline.htm')
    Write-Host 'Site is live.'
} catch {
    Write-Host ('WARNING: Could not remove app_offline.htm - remove manually. ' + $_)
}
