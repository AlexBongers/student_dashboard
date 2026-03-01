$text = [System.IO.File]::ReadAllText("bin\Debug\net8.0-windows\onstage_dump.txt")
$matches = [regex]::Matches($text, '(?si)<tr.*?>.*?</tr>')
$output = ""
if ($matches.Count -gt 0) {
    $output += "Found $($matches.Count) TR elements.`n`n"
    for ($i=0; $i -lt [math]::Min($matches.Count, 30); $i++) {
        $val = $matches[$i].Value -replace '<[^>]+>', '|'
        $val = $val -replace '\|+', '|'
        $val = $val.Trim('|', ' ', "`t", "`r", "`n")
        $output += "ROW ${i}: ${val}`n"
    }
} else {
    $output += "No TR elements found.`n"
    
    $divMatches = [regex]::Matches($text, '(?si)<div[^>]*class=\"[^\"]*(row|mdc-data-table__row|list-item|card)[^\"]*\"[^>]*>.*?</div>')
    $output += "Found $($divMatches.Count) DIV row elements.`n`n"
    for ($i=0; $i -lt [math]::Min($divMatches.Count, 30); $i++) {
        $val = $divMatches[$i].Value -replace '<[^>]+>', '|'
        $val = $val -replace '\|+', '|'
        $val = $val.Trim('|', ' ', "`t", "`r", "`n")
        $output += "DIV ROW ${i}: ${val}`n"
    }
}

[System.IO.File]::WriteAllText("rows.txt", $output)
