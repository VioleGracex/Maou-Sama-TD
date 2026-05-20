Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ae = [System.Windows.Automation.AutomationElement]::RootElement
$condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Recovering Scene Backups")
$window = $ae.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)

if ($window -ne $null) {
    Write-Output "Found window!"
    $noCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "No")
    $noElement = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $noCondition)
    if ($noElement -ne $null) {
        $rect = $noElement.Current.BoundingRectangle
        Write-Output "Found 'No' element! BoundingRectangle: $rect"
        
        # Calculate center point
        $x = [int]($rect.Left + $rect.Width / 2)
        $y = [int]($rect.Top + $rect.Height / 2)
        Write-Output "Clicking at ($x, $y)"
        
        # Set cursor position
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
        Start-Sleep -Milliseconds 100
        
        # Click
        $signature = '[DllImport("user32.dll")] public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);'
        $type = Add-Type -MemberDefinition $signature -Name "Win32Mouse" -Namespace "Win32" -PassThru
        $type::mouse_event(0x0002, 0, 0, 0, 0) # LEFTDOWN
        Start-Sleep -Milliseconds 50
        $type::mouse_event(0x0004, 0, 0, 0, 0) # LEFTUP
        
        Write-Output "Clicked!"
    } else {
        Write-Output "'No' element not found"
    }
} else {
    Write-Output "Window not found"
}
