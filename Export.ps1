# Export ASP.NET Project Files to Single Text File
# PowerShell 5+ compatible
#
# Purpose:
#   Create a source-oriented project snapshot for LLM / AI code review.
#
# Excludes:
#   bin/, obj/, .git/, .vs/, node_modules/, packages/, IDE caches,
#   compiled binaries, generated build artifacts, compressed artifacts, etc.
#
# Includes:
#   C#, Razor, JS, CSS, HTML, JSON, YAML, MSBuild files, project files,
#   solution files, Docker files, shell scripts, Markdown documentation,
#   and selected project metadata.

param(
    [string]$ProjectPath = ".",
    [string]$OutputFile = "docs/llm/dump.txt"
)

# ============================================================================
# CONFIGURATION
# ============================================================================

# File extensions to include.
$IncludeExtensions = @(
    ".cs",
    ".json",
    ".xml",
    ".csproj",
    ".sln",
    ".slnx",
    ".config",
    ".cshtml",
    ".razor",
    ".js",
    ".css",
    ".scss",
    ".html",
    ".yml",
    ".yaml",
    ".sql",
    ".props",
    ".targets",
    ".sh",
    ".ps1",
    ".md"
)

# Specific files to include even if they have no extension.
$IncludeSpecificFiles = @(
    "Dockerfile",
    ".dockerignore",
    ".editorconfig",
    ".gitignore",
    ".gitattributes"
)

# Directories that should never appear in the export.
#
# These are generally generated, cached, dependency, or version-control
# directories and provide little value to an AI reviewing the source.
$ExcludeDirectories = @(
    "bin",
    "obj",
    ".git",
    ".vs",
    ".vscode",
    ".idea",
    "node_modules",
    "packages",
    "TestResults",
    "coverage",
    ".sass-cache",
    ".parcel-cache",
    ".next",
    "dist",
    "out"
)

# File names/patterns to exclude.
#
# These are usually generated, compiled, temporary, or otherwise noisy.
$ExcludeFilePatterns = @(
    "*.dll",
    "*.exe",
    "*.pdb",
    "*.wasm",
    "*.wasm.gz",
    "*.dll.gz",
    "*.pdb.gz",
    "*.js.gz",
    "*.css.gz",
    "*.json.gz",
    "*.dat",
    "*.dat.gz",
    "*.cache",
    "*.lscache",
    "*.tmp",
    "*.log",
    "*.bak",
    "*.user",
    "*.suo"
)

# Files that are technically source/configuration but are often generated.
# Keep this list conservative. If you need one of these, remove it.
$ExcludeSpecificGeneratedFiles = @(
    "project.assets.json",
    "project.nuget.cache",
    "*.AssemblyInfo.cs",
    "*.GlobalUsings.g.cs",
    "*.g.cs",
    "*.g.i.cs"
)

# Documentation files are useful to the AI, so Markdown is included.
# If you want an ultra-small source dump, remove ".md" from
# $IncludeExtensions above.

# ============================================================================
# HELPERS
# ============================================================================

function Write-OutputLine {
    param(
        [string]$Text = ""
    )

    $Text | Out-File -FilePath $OutputPath -Append -Encoding UTF8
}

function Test-ExcludedDirectory {
    param(
        [System.IO.FileInfo]$File
    )

    $fullPath = $File.FullName

    foreach ($directory in $ExcludeDirectories) {
        $pattern = [regex]::Escape("\$directory\")

        if ($fullPath -match $pattern) {
            return $true
        }

        # Also catch a directory at the end of the path.
        $endPattern = [regex]::Escape("\$directory") + "$"

        if ($fullPath -match $endPattern) {
            return $true
        }
    }

    return $false
}

function Test-ExcludedFile {
    param(
        [System.IO.FileInfo]$File
    )

    foreach ($pattern in $ExcludeFilePatterns) {
        if ($File.Name -like $pattern) {
            return $true
        }
    }

    foreach ($pattern in $ExcludeSpecificGeneratedFiles) {
        if ($File.Name -like $pattern) {
            return $true
        }
    }

    return $false
}

function Test-IncludedFile {
    param(
        [System.IO.FileInfo]$File
    )

    if (Test-ExcludedDirectory -File $File) {
        return $false
    }

    if (Test-ExcludedFile -File $File) {
        return $false
    }

    # Check extension-based inclusion.
    if ($IncludeExtensions -contains $File.Extension.ToLowerInvariant()) {
        return $true
    }

    # Check extensionless/specific files.
    foreach ($specificFile in $IncludeSpecificFiles) {
        if ($File.Name -like $specificFile) {
            return $true
        }
    }

    return $false
}

function Get-RelativePath {
    param(
        [string]$FullPath,
        [string]$RootPath
    )

    $root = $RootPath.TrimEnd('\', '/')

    return $FullPath.Substring($root.Length).TrimStart('\', '/')
}

# ============================================================================
# INITIALIZATION
# ============================================================================

$ProjectPath = (Resolve-Path $ProjectPath).Path
$OutputPath = Join-Path $ProjectPath $OutputFile

# Ensure output directory exists.
$OutputDirectory = Split-Path $OutputPath -Parent

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

# Important:
# Make sure the output file itself is not included in the export.
$OutputFullPath = [System.IO.Path]::GetFullPath($OutputPath)

Write-Host "Starting project export..." -ForegroundColor Green
Write-Host "Project Path: $ProjectPath" -ForegroundColor Yellow
Write-Host "Output File: $OutputPath" -ForegroundColor Yellow

# Start fresh.
"" | Out-File -FilePath $OutputPath -Encoding UTF8

# ============================================================================
# HEADER
# ============================================================================

$Header = @"
===============================================================================
COLLABSKUS PROJECT SOURCE EXPORT
===============================================================================

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Project Path: $ProjectPath

This is a source-oriented export intended for AI-assisted code review.

Generated/build/dependency directories and compiled artifacts have been
intentionally excluded.

===============================================================================

"@

$Header | Out-File -FilePath $OutputPath -Append -Encoding UTF8

# ============================================================================
# COLLECT FILES
# ============================================================================

Write-Host "Collecting source files..." -ForegroundColor Cyan

$AllFiles = @(
    Get-ChildItem -Path $ProjectPath -Recurse -File -Force |
        Where-Object {
            $_.FullName -ne $OutputFullPath -and
            (Test-IncludedFile -File $_)
        } |
        Sort-Object FullName
)

Write-Host "Found $($AllFiles.Count) source/documentation files." -ForegroundColor Green

# ============================================================================
# DIRECTORY STRUCTURE
# ============================================================================

Write-OutputLine "DIRECTORY STRUCTURE"
Write-OutputLine "==================="
Write-OutputLine ""

# Build the tree from the files we actually intend to export.
#
# This is deliberately NOT the Windows 'tree' command because 'tree /F'
# includes bin/obj/etc. even when our export excludes them.

$relativePaths = @(
    $AllFiles | ForEach-Object {
        Get-RelativePath -FullPath $_.FullName -RootPath $ProjectPath
    }
)

$treeItems = @{}

foreach ($relativePath in $relativePaths) {
    $parts = $relativePath -split '[\\/]'
    $currentPath = ""

    for ($i = 0; $i -lt $parts.Count; $i++) {
        if ($currentPath -eq "") {
            $currentPath = $parts[$i]
        }
        else {
            $currentPath = "$currentPath/$($parts[$i])"
        }

        if (-not $treeItems.ContainsKey($currentPath)) {
            $treeItems[$currentPath] = $true
        }
    }
}

function Write-Tree {
    param(
        [string]$ParentPath = "",
        [int]$Indent = 0
    )

    $prefix = " " * $Indent

    $children = @()

    foreach ($item in $treeItems.Keys) {
        $parent = Split-Path $item -Parent
        if ($null -eq $parent) {
            $parent = ""
        }

        if ($parent -replace "\\", "/" -eq $ParentPath -replace "\\", "/") {
            $children += $item
        }
    }

    $children = $children | Sort-Object

    foreach ($child in $children) {
        $name = Split-Path $child -Leaf

        $isDirectory = $false

        foreach ($candidate in $treeItems.Keys) {
            if ($candidate -ne $child -and
                $candidate.StartsWith("$child/")) {
                $isDirectory = $true
                break
            }
        }

        if ($isDirectory) {
            Write-OutputLine "$prefix$name/"
            Write-Tree -ParentPath $child -Indent ($Indent + 2)
        }
        else {
            Write-OutputLine "$prefix$name"
        }
    }
}

Write-OutputLine "Project/"
Write-Tree -ParentPath "" -Indent 2

Write-OutputLine ""
Write-OutputLine ""

# ============================================================================
# FILE CONTENTS
# ============================================================================

Write-OutputLine "FILE CONTENTS"
Write-OutputLine "============="
Write-OutputLine ""

$fileCount = 0
$totalBytes = 0

foreach ($file in $AllFiles) {

    $fileCount++
    $totalBytes += $file.Length

    $relativePath = Get-RelativePath `
        -FullPath $file.FullName `
        -RootPath $ProjectPath

    Write-Host `
        "Processing ($fileCount/$($AllFiles.Count)): $relativePath" `
        -ForegroundColor White

    Write-OutputLine "-------------------------------------------------------------------------------"
    Write-OutputLine "FILE: $relativePath"
    Write-OutputLine "SIZE: $([math]::Round($file.Length / 1KB, 2)) KB"
    Write-OutputLine "MODIFIED: $($file.LastWriteTime)"
    Write-OutputLine "-------------------------------------------------------------------------------"
    Write-OutputLine ""

    try {

        # Avoid trying to stuff enormous generated/binary-looking files
        # into the dump even if they somehow passed the filters.
        if ($file.Length -gt 2MB) {
            Write-OutputLine "[FILE SKIPPED: larger than 2 MB]"
        }
        else {
            $content = Get-Content `
                -Path $file.FullName `
                -Raw `
                -ErrorAction Stop

            if ([string]::IsNullOrEmpty($content)) {
                Write-OutputLine "[EMPTY FILE]"
            }
            else {
                Write-OutputLine $content
            }
        }

    }
    catch {

        Write-OutputLine "[ERROR READING FILE]"
        Write-OutputLine $_.Exception.Message
    }

    Write-OutputLine ""
    Write-OutputLine ""
}

# ============================================================================
# FOOTER
# ============================================================================

$outputFileInfo = Get-Item $OutputPath

$Footer = @"
===============================================================================
EXPORT COMPLETED
===============================================================================

Completed: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Total Files Exported: $fileCount
Total Source Bytes: $totalBytes
Output File: $OutputPath
Output Size: $([math]::Round($outputFileInfo.Length / 1MB, 2)) MB

Excluded directories:
$($ExcludeDirectories -join ", ")

===============================================================================
"@

$Footer | Out-File -FilePath $OutputPath -Append -Encoding UTF8

Write-Host ""
Write-Host "Export completed successfully!" -ForegroundColor Green
Write-Host "Output file: $OutputPath" -ForegroundColor Yellow
Write-Host "Total files exported: $fileCount" -ForegroundColor Green
Write-Host "Output file size: $([math]::Round($outputFileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
