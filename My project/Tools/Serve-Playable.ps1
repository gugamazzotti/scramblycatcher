# Serve the WebGL playable locally with no external requests.
# Usage: powershell -File Tools/Serve-Playable.ps1

param(
    [int]$Port = 8080,
    [string]$Root = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Join-Path $PSScriptRoot "..\Builds\WebGL"
}

$Root = (Resolve-Path $Root).Path
$prefix = "http://127.0.0.1:$Port/"
$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add($prefix)
$listener.Start()
Write-Output "Serving $Root at $prefix"

$mime = @{
    ".html" = "text/html; charset=utf-8"
    ".js"   = "application/javascript; charset=utf-8"
    ".json" = "application/json"
    ".wasm" = "application/wasm"
    ".data" = "application/octet-stream"
    ".br"   = "application/octet-stream"
    ".png"  = "image/png"
    ".css"  = "text/css"
    ".ico"  = "image/x-icon"
    ".svg"  = "image/svg+xml"
}

try {
    while ($listener.IsListening) {
        $ctx = $listener.GetContext()
        $path = [Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath.TrimStart("/"))
        if ([string]::IsNullOrWhiteSpace($path)) { $path = "index.html" }
        $full = [System.IO.Path]::GetFullPath((Join-Path $Root $path))
        $rootFull = [System.IO.Path]::GetFullPath($Root)
        if (-not $full.StartsWith($rootFull)) {
            $ctx.Response.StatusCode = 403
            $ctx.Response.Close()
            continue
        }
        if (-not (Test-Path $full)) {
            $ctx.Response.StatusCode = 404
            $ctx.Response.Close()
            continue
        }
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $ext = [System.IO.Path]::GetExtension($full).ToLowerInvariant()
        $ctx.Response.ContentType = $(if ($mime.ContainsKey($ext)) { $mime[$ext] } else { "application/octet-stream" })
        $ctx.Response.Headers.Add("Cache-Control", "no-store")
        $ctx.Response.ContentLength64 = $bytes.Length
        $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $ctx.Response.Close()
    }
}
finally {
    $listener.Stop()
    $listener.Close()
}
