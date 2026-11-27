# SPDX-License-Identifier: GPL-3.0-or-later
<#
.SYNOPSIS
  Creates update.json for a release and signs it (SPEC §5.5).

.DESCRIPTION
  Reads the published executables OACP-<version>-<rid>.exe from -DistDir, writes update.json
  (version, release notes URL, minimum runtime, download URL + SHA-256 + size per platform) and
  update.json.sig (Base64 ECDSA P-256 / SHA-256 signature, IEEE P1363 format) into -DistDir.

  The private key (PEM) is read from the environment variable UPDATE_SIGNING_KEY (GitHub secret).
  The script fails when the key is missing or does not match a trusted public key in -KeysFile,
  so a release can never ship an update manifest the application would reject.
#>
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $DistDir,
    [Parameter(Mandatory)] [string] $Repository,
    [string] $KeysFile = (Join-Path $PSScriptRoot '..' 'src' 'Obevalon.AudioControlPanel.Core' 'Updates' 'update-signing-keys.pem'),
    [string] $MinimumRuntime = '10.0.0',
    [string[]] $RuntimeIdentifiers = @('win-x64', 'win-arm64')
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:UPDATE_SIGNING_KEY)) {
    throw 'The UPDATE_SIGNING_KEY secret is not set. See docs/RELEASING.md (signing key).'
}

$assets = [ordered]@{}
foreach ($rid in $RuntimeIdentifiers) {
    $file = Join-Path $DistDir "OACP-$Version-$rid.exe"
    if (-not (Test-Path $file)) { throw "Missing $file" }
    $assets[$rid] = [ordered]@{
        url    = "https://github.com/$Repository/releases/download/v$Version/OACP-$Version-$rid.exe"
        sha256 = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()
        size   = (Get-Item $file).Length
    }
}

$manifest = [ordered]@{
    schema          = 1
    version         = $Version
    releaseDate     = [DateTimeOffset]::UtcNow.ToString('o')
    releaseNotesUrl = "https://github.com/$Repository/releases/tag/v$Version"
    minimumRuntime  = $MinimumRuntime
    assets          = $assets
}

# Sign the exact bytes that are published.
$bytes = [System.Text.UTF8Encoding]::new($false).GetBytes(($manifest | ConvertTo-Json -Depth 5))
[System.IO.File]::WriteAllBytes((Join-Path $DistDir 'update.json'), $bytes)

$signer = [System.Security.Cryptography.ECDsa]::Create()
try {
    $signer.ImportFromPem($env:UPDATE_SIGNING_KEY)
    if ($signer.KeySize -ne 256) { throw 'UPDATE_SIGNING_KEY must be an ECDSA P-256 key.' }
    $signature = $signer.SignData($bytes, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
}
finally {
    $signer.Dispose()
}

[System.IO.File]::WriteAllText((Join-Path $DistDir 'update.json.sig'), [Convert]::ToBase64String($signature))

# Self-check: the signature must verify with a public key the application trusts.
$trusted = [regex]::Matches((Get-Content $KeysFile -Raw), '-----BEGIN PUBLIC KEY-----[\s\S]+?-----END PUBLIC KEY-----')
$verified = $false
foreach ($match in $trusted) {
    $verifier = [System.Security.Cryptography.ECDsa]::Create()
    try {
        $verifier.ImportFromPem($match.Value)
        if ($verifier.VerifyData($bytes, $signature, [System.Security.Cryptography.HashAlgorithmName]::SHA256)) {
            $verified = $true
        }
    }
    finally {
        $verifier.Dispose()
    }
}

if (-not $verified) {
    throw "The signing key does not match any trusted public key in $KeysFile."
}

Write-Host "update.json signed for version $Version ($($RuntimeIdentifiers -join ', '))."
