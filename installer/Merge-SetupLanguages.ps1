param(
    [Parameter(Mandatory)][string]$English,
    [Parameter(Mandatory)][string]$Spanish,
    [Parameter(Mandatory)][string]$Destination
)

$ErrorActionPreference = 'Stop'

function Invoke-MsiMethod($object, [string]$name, [object[]]$arguments) {
    $object.GetType().InvokeMember($name, 'InvokeMethod', $null, $object, $arguments)
}

function Open-MsiView($database, [string]$sql, $record = $null) {
    $view = Invoke-MsiMethod $database OpenView @($sql)
    $null = Invoke-MsiMethod $view Execute @($record)
    $view
}

function Get-MsiField($record, [int]$index) {
    $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, @($index))
}

function Get-LocalizedRows($database, $table, $columns) {
    $quoted = $columns | ForEach-Object { '`{0}`' -f $_ }
    $view = Open-MsiView $database ('SELECT {0} FROM `{1}`' -f ($quoted -join ', '), $table)
    try {
        while ($record = Invoke-MsiMethod $view Fetch) {
            $key = @(for ($index = 1; $index -lt $columns.Count; $index++) { Get-MsiField $record $index })
            [pscustomobject]@{ Id = $key -join '/'; Key = $key; Text = Get-MsiField $record $columns.Count }
        }
    } finally { $null = Invoke-MsiMethod $view Close }
}

function Set-MsiRow($database, [string]$sql, [object[]]$values) {
    $record = Invoke-MsiMethod $installer CreateRecord @($values.Count)
    for ($index = 0; $index -lt $values.Count; $index++) {
        $field = if ($values[$index] -is [int]) { 'IntegerData' } else { 'StringData' }
        $null = $record.GetType().InvokeMember($field, 'SetProperty', $null, $record, @(($index + 1), $values[$index]))
    }
    $view = Open-MsiView $database $sql $record
    $null = Invoke-MsiMethod $view Close
}

Copy-Item -LiteralPath $English -Destination $Destination
$installer = New-Object -ComObject WindowsInstaller.Installer
$base = Invoke-MsiMethod $installer OpenDatabase @($Destination, 1)
$translation = Invoke-MsiMethod $installer OpenDatabase @($Spanish, 0)
$view = Open-MsiView $base 'CREATE TABLE `WiiSetupText` (`Identifier` CHAR(64) NOT NULL, `English` LONGCHAR, `Spanish` LONGCHAR PRIMARY KEY `Identifier`)'
$null = Invoke-MsiMethod $view Close

# Only MSI Formatted columns can refer to runtime language properties.
$tables = [ordered]@{
    Dialog = @('Dialog', 'Title')
    Control = @('Dialog_', 'Control', 'Text')
    RadioButton = @('Property', 'Order', 'Text')
    LaunchCondition = @('Condition', 'Description')
}
$counter = 0
foreach ($table in $tables.Keys) {
    $columns = $tables[$table]
    $spanishRows = @{}
    foreach ($row in Get-LocalizedRows $translation $table $columns) { $spanishRows.Add($row.Id, $row.Text) }

    $where = ($columns[0..($columns.Count - 2)] | ForEach-Object { '`{0}` = ?' -f $_ }) -join ' AND '
    $update = 'UPDATE `{0}` SET `{1}` = ? WHERE {2}' -f $table, $columns[-1], $where
    foreach ($row in @(Get-LocalizedRows $base $table $columns | Sort-Object Id)) {
        if (!$spanishRows.ContainsKey($row.Id)) { throw "The Spanish MSI is missing $table/$($row.Id)." }
        $spanishText = $spanishRows[$row.Id]
        if ($row.Text -ceq $spanishText) { continue }

        # Keep the font marker on the control itself, where Windows Installer parses it.
        $font = [regex]::Match($row.Text, '^\{\\[^}]+\}').Value
        $englishText = $row.Text.Substring($font.Length)
        $spanishText = $spanishText -replace '^\{\\[^}]+\}', ''
        $identifier = 'WII_SETUP_TEXT_{0:D3}' -f (++$counter)
        Set-MsiRow $base 'INSERT INTO `WiiSetupText` (`Identifier`, `English`, `Spanish`) VALUES (?, ?, ?)' @($identifier, $englishText, $spanishText)
        if ($table -eq 'RadioButton') { $row.Key[1] = [int]$row.Key[1] }
        Set-MsiRow $base $update (@("$font[$identifier]") + $row.Key)
        if ($table -eq 'LaunchCondition') {
            # Silent setup skips the UI sequence and needs a usable error message.
            Set-MsiRow $base 'INSERT INTO `Property` (`Property`, `Value`) VALUES (?, ?)' @($identifier, $englishText)
        }
    }
}
if ($counter -eq 0) { throw 'The MSI wizard language catalogs are empty.' }
$null = Invoke-MsiMethod $base Commit
Write-Output "$counter setup strings are available in English and Spanish."
