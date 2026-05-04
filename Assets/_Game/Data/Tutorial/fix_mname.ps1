$dirs = @('d:\OuikiDev\Maou-Sama-TD\Assets\_Game\Data\Tutorial\Level1', 'd:\OuikiDev\Maou-Sama-TD\Assets\_Game\Data\Tutorial\Level2')
foreach ($dir in $dirs) {
    if (Test-Path $dir) {
        $files = Get-ChildItem -Path $dir -Filter '*.asset'
        foreach ($file in $files) {
            $baseName = $file.BaseName
            $content = Get-Content $file.FullName -Raw
            $newContent = $content -replace '(?m)^  m_Name: .*$', "  m_Name: $baseName"
            if ($content -ne $newContent) {
                Write-Output "Fixing m_Name in $($file.Name) to $baseName"
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
            }
        }
    }
}
