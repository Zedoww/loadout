<#
.SYNOPSIS
    Installs and configures CodeGraph, RTK, and Caveman for the Loadout project.

.DESCRIPTION
    This script sets up three developer efficiency tools optimized for Claude Code:
      - CodeGraph: Semantic code graph (MCP server for AI agents)
      - RTK:       Terminal output compression (60-90% token savings)
      - Caveman:   AI response compression skill (65-75% token savings)

    Run this script from the repository root with PowerShell 5.1+.
    Some steps may require administrator privileges or manual action.

.NOTES
    Prerequisites: Node.js 18+, npm
    Optional:      Rust toolchain (for RTK via cargo) or manual binary download
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $RepoRoot) { $RepoRoot = Get-Location }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Loadout - Dev Tools Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. CodeGraph ──────────────────────────────────────────────────────────────

Write-Host "[1/3] CodeGraph — Semantic code indexing" -ForegroundColor Yellow
Write-Host "        Creates an AST-based knowledge graph of the codebase."
Write-Host "        AI agents query the graph instead of scanning files."
Write-Host ""

$cgInstalled = $null -ne (Get-Command codegraph -ErrorAction SilentlyContinue)

if (-not $cgInstalled) {
    $nodeInstalled = $null -ne (Get-Command node -ErrorAction SilentlyContinue)
    if (-not $nodeInstalled) {
        Write-Host "  [!] Node.js is required. Install it from https://nodejs.org/" -ForegroundColor Red
        Write-Host "      Then re-run this script." -ForegroundColor Red
    } else {
        Write-Host "  Installing @colbymchenry/codegraph globally..." -ForegroundColor Gray
        npm i -g @colbymchenry/codegraph
    }
} else {
    Write-Host "  [OK] CodeGraph is already installed." -ForegroundColor Green
}

# Initialize index if not already done.
$cgInstalled = $null -ne (Get-Command codegraph -ErrorAction SilentlyContinue)
if ($cgInstalled) {
    Push-Location $RepoRoot
    if (-not (Test-Path ".codegraph")) {
        Write-Host "  Indexing the codebase (first run may take a moment)..." -ForegroundColor Gray
        codegraph init -i
    } else {
        Write-Host "  [OK] .codegraph/ index already exists. Re-indexing..." -ForegroundColor Green
        codegraph index
    }

    Write-Host "  Connecting to Claude Code..." -ForegroundColor Gray
    codegraph install --target=claude-code --location=local --yes 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Auto-install failed. Run manually: codegraph install" -ForegroundColor DarkYellow
    }
    Pop-Location
}

Write-Host ""

# ── 2. RTK ────────────────────────────────────────────────────────────────────

Write-Host "[2/3] RTK — Terminal output compression" -ForegroundColor Yellow
Write-Host "        Strips noise from dotnet build/test/git output before"
Write-Host "        sending it to the AI. 60-90% token reduction."
Write-Host ""

$rtkInstalled = $null -ne (Get-Command rtk -ErrorAction SilentlyContinue)

if (-not $rtkInstalled) {
    $cargoInstalled = $null -ne (Get-Command cargo -ErrorAction SilentlyContinue)
    if ($cargoInstalled) {
        Write-Host "  Installing RTK via cargo..." -ForegroundColor Gray
        cargo install --git https://github.com/rtk-ai/rtk
    } else {
        Write-Host "  [!] RTK requires manual installation:" -ForegroundColor DarkYellow
        Write-Host "      Option A: Install Rust (https://rustup.rs), then:" -ForegroundColor Gray
        Write-Host "                cargo install --git https://github.com/rtk-ai/rtk" -ForegroundColor White
        Write-Host "      Option B: Download the binary from:" -ForegroundColor Gray
        Write-Host "                https://github.com/rtk-ai/rtk/releases" -ForegroundColor White
        Write-Host "                Extract rtk.exe to a folder in your PATH." -ForegroundColor Gray
    }
} else {
    Write-Host "  [OK] RTK is already installed." -ForegroundColor Green
}

# Initialize for Claude Code if installed.
$rtkInstalled = $null -ne (Get-Command rtk -ErrorAction SilentlyContinue)
if ($rtkInstalled) {
    Write-Host "  Initializing RTK hooks for Claude Code..." -ForegroundColor Gray
    rtk init -g
    Write-Host "  [OK] RTK hooks configured. Restart Claude Code for changes to take effect." -ForegroundColor Green
}

Write-Host ""

# ── 3. Caveman ────────────────────────────────────────────────────────────────

Write-Host "[3/3] Caveman — AI response compression" -ForegroundColor Yellow
Write-Host "        Forces Claude to respond in ultra-concise style."
Write-Host "        65-75% output token reduction. Activate with /caveman."
Write-Host ""

$npxInstalled = $null -ne (Get-Command npx -ErrorAction SilentlyContinue)
if ($npxInstalled) {
    Write-Host "  Installing caveman skill for Claude Code..." -ForegroundColor Gray
    npx skills add JuliusBrussee/caveman
} else {
    Write-Host "  [!] npx not found. Install Node.js first." -ForegroundColor Red
    Write-Host "      Then run: npx skills add JuliusBrussee/caveman" -ForegroundColor Gray
}

Write-Host ""

# ── Summary ───────────────────────────────────────────────────────────────────

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor White
Write-Host "    1. Restart Claude Code" -ForegroundColor Gray
Write-Host "    2. Type /caveman to enable concise mode" -ForegroundColor Gray
Write-Host "    3. RTK and CodeGraph work automatically" -ForegroundColor Gray
Write-Host ""
Write-Host "  Verify with:" -ForegroundColor White
Write-Host "    codegraph --version" -ForegroundColor Gray
Write-Host "    rtk --version" -ForegroundColor Gray
Write-Host ""
