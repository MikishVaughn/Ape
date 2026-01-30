$bytes = New-Object byte[] 32
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
[System.Environment]::SetEnvironmentVariable('MASTER_CREDENTIAL_KEY_ILLUSTRATE', $key, 'User')
pause