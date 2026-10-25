# Draws the SysGadget icon (three meter bars on a dark tile, red to violet like the cards) and writes
# src/SysGadget.App/SysGadget.ico with the sizes 16, 24, 32, 48, 64, 128 and 256, each as a PNG inside
# the ICO (Windows Vista+).
#
#     powershell -NoProfile -File tools/make_icon.ps1

Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "src\SysGadget.App\SysGadget.ico"

function Draw([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.ScaleTransform($size / 256.0, $size / 256.0)

    # the tile: a rounded square in the gadget's dark panel colour
    $tile = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = 56
    $tile.AddArc(8, 8, $r, $r, 180, 90); $tile.AddArc(248 - $r, 8, $r, $r, 270, 90)
    $tile.AddArc(248 - $r, 248 - $r, $r, $r, 0, 90); $tile.AddArc(8, 248 - $r, $r, $r, 90, 90); $tile.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0x1C, 0x1F, 0x27))), $tile)

    # three rounded bars of rising height, coloured like the first, middle and last card
    $bars = @(
        @{ X = 48;  H = 92;  C = [System.Drawing.Color]::FromArgb(0xF7, 0x5E, 0x5E) },
        @{ X = 106; H = 140; C = [System.Drawing.Color]::FromArgb(0x5E, 0xF7, 0x8C) },
        @{ X = 164; H = 180; C = [System.Drawing.Color]::FromArgb(0xA8, 0x5E, 0xF7) }
    )
    foreach ($bar in $bars) {
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $x = $bar.X; $w = 44; $bottom = 216; $top = $bottom - $bar.H; $c = 16
        $path.AddArc($x, $top, $c, $c, 180, 90); $path.AddArc($x + $w - $c, $top, $c, $c, 270, 90)
        $path.AddLine($x + $w, $top + $c, $x + $w, $bottom); $path.AddLine($x + $w, $bottom, $x, $bottom); $path.CloseFigure()
        $g.FillPath((New-Object System.Drawing.SolidBrush $bar.C), $path)
    }

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = @(); foreach ($s in $sizes) { $images += ,(Draw $s) }

# ICO: header, one directory entry per size, then the PNG data
$f = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $f
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $images[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]$len); $w.Write([UInt32]$offset)
    $offset += $len
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
[System.IO.File]::WriteAllBytes($out, $f.ToArray())
Write-Host "wrote $out ($($f.Length) bytes, sizes $($sizes -join ', '))"
