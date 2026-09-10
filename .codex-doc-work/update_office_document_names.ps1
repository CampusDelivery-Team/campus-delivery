$ErrorActionPreference = 'Stop'

$old = '校园中转分发与跑腿服务管理系统'
$new = '校园综合跑腿与代取服务管理系统'
$shortOld = '校园跑腿系统'
$docs = 'D:\delivery-backend\docs'
$map = @(
    @{ Src = Join-Path $docs "$old-系统需求分析文档.doc"; Dst = Join-Path $docs "$new-系统需求分析文档.doc"; Title = "$new 系统需求分析文档" },
    @{ Src = Join-Path $docs "$old-数据库设计文档.docx"; Dst = Join-Path $docs "$new-数据库设计文档.docx"; Title = "$new 数据库设计文档" },
    @{ Src = Join-Path $docs "$old-系统设计与实现文档.docx"; Dst = Join-Path $docs "$new-系统设计与实现文档.docx"; Title = "$new 系统设计与实现文档" }
)

$backup = 'D:\delivery-backend\.codex-doc-work\name-migration-backup'
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $map[0].Src -Destination (Join-Path $backup 'requirements-before-name-update.doc') -Force
Copy-Item -LiteralPath $map[1].Src -Destination (Join-Path $backup 'database-design-before-name-update.docx') -Force
Copy-Item -LiteralPath $map[2].Src -Destination (Join-Path $backup 'system-design-before-name-update.docx') -Force

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    foreach ($item in $map) {
        if (Test-Path -LiteralPath $item.Dst) {
            throw "目标文件已存在：$($item.Dst)"
        }

        $doc = $word.Documents.Open($item.Src, $false, $false)
        try {
            foreach ($storyType in 1..17) {
                try {
                    $range = $doc.StoryRanges.Item($storyType)
                }
                catch {
                    continue
                }

                while ($null -ne $range) {
                    $find = $range.Find
                    $find.ClearFormatting()
                    $find.Replacement.ClearFormatting()
                    [void]$find.Execute($old, $false, $true, $false, $false, $false, $true, 1, $false, $new, 2)

                    if ($item.Src -like '*系统设计与实现文档*') {
                        $find = $range.Find
                        $find.ClearFormatting()
                        $find.Replacement.ClearFormatting()
                        [void]$find.Execute($shortOld, $false, $true, $false, $false, $false, $true, 1, $false, $new, 2)
                    }

                    $range = $range.NextStoryRange
                }
            }

            try { $doc.BuiltInDocumentProperties.Item('Title').Value = $item.Title } catch {}
            try { $doc.Repaginate() } catch {}
            try { foreach ($toc in $doc.TablesOfContents) { [void]$toc.Update() } } catch {}
            $format = $doc.SaveFormat
            $doc.SaveAs2($item.Dst, $format)
        }
        finally {
            $doc.Close($false)
        }
    }
}
finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) | Out-Null
}

$map | ForEach-Object {
    Get-Item -LiteralPath $_.Dst | Select-Object FullName, Length, LastWriteTime
} | Format-Table -AutoSize
