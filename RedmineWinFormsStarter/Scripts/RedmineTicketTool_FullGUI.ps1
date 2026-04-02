#requires -version 5.1
# LINKTAG RMPS001
<#
RedmineTicketTool Full GUI v7.1 (PS5.1 hotfix)
Fix:
- Removed null-coalescing operator '??' (PowerShell 7 syntax). PS5.1 compatible.
Other features kept: robust CSV loader, date header normalization, Pickup ticket from selection, resizable layout, metadata layout fix.
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Windows.Forms.DataVisualization
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic

$ConfigPath = Join-Path $PSScriptRoot 'gui_config.json'

function New-DefaultConfig { @{ apiKey=''; baseUrl=''; lastCsv='' } }

function LoadConfig {
  $cfg = New-DefaultConfig
  if(Test-Path $ConfigPath){
    try {
      $obj = (Get-Content $ConfigPath -Raw | ConvertFrom-Json)
      if($obj){
        $ht=@{}; foreach($p in $obj.PSObject.Properties){ $ht[$p.Name]=$p.Value }
        foreach($k in $cfg.Keys){ if($ht.ContainsKey($k) -and $null -ne $ht[$k]){ $cfg[$k]=[string]$ht[$k] } }
      }
    } catch {}
  }
  return $cfg
}

function SaveConfig($cfg){ try{ ($cfg|ConvertTo-Json) | Set-Content -Encoding UTF8 $ConfigPath } catch {} }

function NormalizeBaseUrl([string]$u){ if([string]::IsNullOrWhiteSpace($u)){return ''}; $u=$u.Trim(); if($u.EndsWith('/')){$u=$u.Substring(0,$u.Length-1)}; return $u }
function MsgE([string]$s){[System.Windows.Forms.MessageBox]::Show($s,'Error','OK','Error')|Out-Null}
function MsgI([string]$s){[System.Windows.Forms.MessageBox]::Show($s,'Info','OK','Information')|Out-Null}

function FindFile($dir,$name){ $p=Join-Path $dir $name; if(Test-Path $p){$p}else{$null} }

function InvokeGet($url,$key){ try{ Invoke-RestMethod -Method Get -Uri $url -Headers @{ 'X-Redmine-API-Key'=$key; 'Accept'='application/json'} } catch { $null } }

# ---------- CSV Grid (TextFieldParser) ----------
$script:csvPath = ''
$script:csvDT = $null
$script:saveHeaders = @()  # normalized headers for output
$script:colNames = @()     # unique column names in DataTable

function Make-UniqueHeaders([string[]]$headers){
  $save=@(); $cols=@();
  $seen=@{}; $dateIdx=0
  foreach($h0 in $headers){
    $raw = ''
    if($null -ne $h0){ $raw = [string]$h0 }

    $h = $raw.Trim()
    $h = $h.TrimStart([char]0xFEFF)
    if([string]::IsNullOrWhiteSpace($h)){ $h = 'col' }

    $isDate = $h -match '^\d{4}[/\-]\d{2}[/\-]\d{2}$'
    $hSave = $h
    if($isDate){ $dateIdx++; $hSave = 'date_' + $dateIdx }

    $base = $hSave
    if(-not $seen.ContainsKey($base)){ $seen[$base]=1; $hCol=$base }
    else { $seen[$base] += 1; $hCol = $base + '_' + $seen[$base] }

    $save += $hSave
    $cols += $hCol
  }
  return @{ save=$save; cols=$cols }
}

function LoadCsvToGrid([string]$path, $grid){
  if(-not (Test-Path $path)){ MsgE 'CSV file not found.'; return $false }

  $parser = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser($path, [System.Text.Encoding]::UTF8)
  $parser.TextFieldType = [Microsoft.VisualBasic.FileIO.FieldType]::Delimited
  $parser.SetDelimiters(',')
  $parser.HasFieldsEnclosedInQuotes = $true

  # skip leading blank lines
  $headerFields=$null
  while(-not $parser.EndOfData){
    $peek = $parser.ReadFields()
    if($peek -and ($peek | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -gt 0){
      $headerFields = $peek
      break
    }
  }
  if(-not $headerFields){ MsgE 'CSV header not found.'; $parser.Close(); return $false }

  $h = Make-UniqueHeaders $headerFields
  $script:saveHeaders = $h.save
  $script:colNames = $h.cols

  $dt = New-Object System.Data.DataTable
  foreach($c in $script:colNames){ [void]$dt.Columns.Add($c) }

  while(-not $parser.EndOfData){
    $fields = $parser.ReadFields()
    if(-not $fields){ continue }
    $row = $dt.NewRow()
    for($i=0; $i -lt $script:colNames.Count; $i++){
      $val = ''
      if($i -lt $fields.Count){ $val = [string]$fields[$i] }
      $row[$script:colNames[$i]] = $val
    }
    [void]$dt.Rows.Add($row)
  }
  $parser.Close()

  $grid.DataSource = $dt
  $script:csvDT = $dt
  $script:csvPath = $path
  return $true
}

function SaveGridToCsv([string]$path){
  if(-not $script:csvDT){ MsgE 'No CSV loaded.'; return }
  $cols = $script:colNames
  $head = ($script:saveHeaders -join ',')
  $lines = New-Object System.Collections.Generic.List[string]
  $lines.Add($head)
  foreach($row in $script:csvDT.Rows){
    $vals=@()
    for($i=0;$i -lt $cols.Count;$i++){
      $v=[string]$row[$cols[$i]]
      if($v -match '[",\r\n]'){ $v='"'+($v -replace '"','""')+'"' }
      $vals += $v
    }
    $lines.Add(($vals -join ','))
  }
  $bytes = [System.Text.Encoding]::UTF8.GetPreamble() + [System.Text.Encoding]::UTF8.GetBytes(($lines -join "`r`n"))
  [System.IO.File]::WriteAllBytes($path, $bytes)
}

# ---------- Process async runner ----------
$script:proc=$null; $script:evOut=$null; $script:evErr=$null; $script:evExit=$null

function CleanupProcess {
  if($script:evOut){ try{Unregister-Event -SourceIdentifier $script:evOut.Name -ErrorAction SilentlyContinue}catch{}; $script:evOut=$null }
  if($script:evErr){ try{Unregister-Event -SourceIdentifier $script:evErr.Name -ErrorAction SilentlyContinue}catch{}; $script:evErr=$null }
  if($script:evExit){ try{Unregister-Event -SourceIdentifier $script:evExit.Name -ErrorAction SilentlyContinue}catch{}; $script:evExit=$null }
  if($script:proc){ try{$script:proc.Dispose()}catch{}; $script:proc=$null }
}

# ---------- UI ----------
$cfg = LoadConfig

$form = New-Object System.Windows.Forms.Form
$form.Text='Redmine CSV Tool'
$form.Size = New-Object System.Drawing.Size(1280,860)
$form.StartPosition='CenterScreen'

$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock='Fill'
$form.Controls.Add($tabs)

$tabRun=New-Object System.Windows.Forms.TabPage; $tabRun.Text='Run'
$tabMeta=New-Object System.Windows.Forms.TabPage; $tabMeta.Text='Metadata'
$tabs.TabPages.Add($tabRun)
$tabs.TabPages.Add($tabMeta)

# Run layout
$panelTop = New-Object System.Windows.Forms.Panel
$panelTop.Dock='Top'
$panelTop.Height=150
$tabRun.Controls.Add($panelTop)

$splitRun = New-Object System.Windows.Forms.SplitContainer
$splitRun.Dock='Fill'
$splitRun.Orientation='Vertical'
$splitRun.SplitterDistance=720
$tabRun.Controls.Add($splitRun)

$grid = New-Object System.Windows.Forms.DataGridView
$grid.Dock='Fill'
$grid.AllowUserToAddRows=$true
$grid.AllowUserToDeleteRows=$true
$grid.SelectionMode='FullRowSelect'
$grid.MultiSelect=$true
$grid.AutoSizeColumnsMode='DisplayedCells'
$splitRun.Panel1.Controls.Add($grid)

$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Dock='Fill'
$txtLog.Multiline=$true
$txtLog.ScrollBars='Vertical'
$txtLog.ReadOnly=$true
$splitRun.Panel2.Controls.Add($txtLog)

# Run top controls
$lblUrl=New-Object System.Windows.Forms.Label; $lblUrl.Text='Base URL'; $lblUrl.SetBounds(10,10,90,20)
$txtUrl=New-Object System.Windows.Forms.TextBox; $txtUrl.SetBounds(110,8,620,22); $txtUrl.Text=$cfg.baseUrl
$lblKey=New-Object System.Windows.Forms.Label; $lblKey.Text='API Key'; $lblKey.SetBounds(10,40,90,20)
$txtKey=New-Object System.Windows.Forms.TextBox; $txtKey.SetBounds(110,38,620,22); $txtKey.UseSystemPasswordChar=$true; $txtKey.Text=$cfg.apiKey
$lblCsv=New-Object System.Windows.Forms.Label; $lblCsv.Text='CSV'; $lblCsv.SetBounds(10,70,90,20)
$txtCsv=New-Object System.Windows.Forms.TextBox; $txtCsv.SetBounds(110,68,620,22); $txtCsv.Text=$cfg.lastCsv
$btnCsv=New-Object System.Windows.Forms.Button; $btnCsv.Text='Select'; $btnCsv.SetBounds(750,66,90,26)
$btnLoad=New-Object System.Windows.Forms.Button; $btnLoad.Text='Load'; $btnLoad.SetBounds(850,66,70,26)
$btnSave=New-Object System.Windows.Forms.Button; $btnSave.Text='Save'; $btnSave.SetBounds(930,66,70,26)

$lblPick=New-Object System.Windows.Forms.Label; $lblPick.Text='Pickup ticket'; $lblPick.SetBounds(10,105,90,20)
$txtPick=New-Object System.Windows.Forms.TextBox; $txtPick.SetBounds(110,102,420,22)
$btnPick=New-Object System.Windows.Forms.Button; $btnPick.Text='From selection'; $btnPick.SetBounds(540,100,120,26)
$btnPickClear=New-Object System.Windows.Forms.Button; $btnPickClear.Text='Clear'; $btnPickClear.SetBounds(670,100,70,26)

$chkIgnore=New-Object System.Windows.Forms.CheckBox; $chkIgnore.Text='ignore-version'; $chkIgnore.SetBounds(750,102,130,22); $chkIgnore.Checked=$true
$btnDry=New-Object System.Windows.Forms.Button; $btnDry.Text='Dry Run'; $btnDry.SetBounds(900,96,110,30)
$btnApply=New-Object System.Windows.Forms.Button; $btnApply.Text='Apply'; $btnApply.SetBounds(1020,96,110,30)
$btnApply.BackColor=[System.Drawing.Color]::MistyRose
$lblState=New-Object System.Windows.Forms.Label; $lblState.Text=''; $lblState.SetBounds(900,130,240,18)

$panelTop.Controls.AddRange(@($lblUrl,$txtUrl,$lblKey,$txtKey,$lblCsv,$txtCsv,$btnCsv,$btnLoad,$btnSave,$lblPick,$txtPick,$btnPick,$btnPickClear,$chkIgnore,$btnDry,$btnApply,$lblState))

function AppendLog([string]$line){
  $null=$form.BeginInvoke([Action[string]]{param($t) $txtLog.AppendText($t)}, $line)
}
function SetRunning([bool]$running,[string]$msg){
  $null=$form.BeginInvoke([Action] {
    $btnDry.Enabled=-not $running; $btnApply.Enabled=-not $running
    $btnCsv.Enabled=-not $running; $btnLoad.Enabled=-not $running; $btnSave.Enabled=-not $running
    $btnPick.Enabled=-not $running; $btnPickClear.Enabled=-not $running
    $lblState.Text=$msg
  })
}

$btnCsv.Add_Click({
  $d=New-Object System.Windows.Forms.OpenFileDialog
  $d.Filter='CSV|*.csv'
  if($d.ShowDialog() -eq 'OK'){
    $txtCsv.Text=$d.FileName
    $cfg.lastCsv=$d.FileName
    SaveConfig $cfg
    LoadCsvToGrid $d.FileName $grid | Out-Null
  }
})
$btnLoad.Add_Click({
  if(LoadCsvToGrid $txtCsv.Text $grid){ $cfg.lastCsv=$txtCsv.Text; SaveConfig $cfg }
})
$btnSave.Add_Click({
  if([string]::IsNullOrWhiteSpace($txtCsv.Text)){ MsgE 'Select CSV first'; return }
  SaveGridToCsv $txtCsv.Text
  MsgI 'Saved.'
})

$btnPick.Add_Click({
  if(-not $script:csvDT){ MsgE 'Load CSV first'; return }
  # try find id column by name '#' or first column
  $idCol = $script:colNames[0]
  foreach($c in $script:colNames){ if($c -eq '#'){ $idCol=$c; break } }
  $ids=@()
  foreach($r in $grid.SelectedRows){
    $val = [string]$r.Cells[$idCol].Value
    if($val -match '^\d+$'){ $ids += $val }
  }
  $ids = $ids | Sort-Object -Unique
  $txtPick.Text = ($ids -join ',')
})
$btnPickClear.Add_Click({ $txtPick.Text='' })

function StartRun([string]$mode){
  $base = NormalizeBaseUrl $txtUrl.Text
  $key  = $txtKey.Text
  if([string]::IsNullOrWhiteSpace($base) -or [string]::IsNullOrWhiteSpace($key)){ MsgE 'Base URL / API Key required'; return }
  if(-not (Test-Path $txtCsv.Text)){ MsgE 'CSV not selected'; return }

  $cfg.apiKey=$key; $cfg.baseUrl=$base; $cfg.lastCsv=$txtCsv.Text; SaveConfig $cfg

  if($script:csvPath -ne $txtCsv.Text){ LoadCsvToGrid $txtCsv.Text $grid | Out-Null }

  $dir = Split-Path $txtCsv.Text
  $py = FindFile $dir 'python.exe'
  $sc = FindFile $dir 'redmine_update_from_csv.py'
  if(-not $py -or -not $sc){ MsgE 'python.exe or redmine_update_from_csv.py not found in CSV folder'; return }

  CleanupProcess

  $args = '"'+$sc+'" --base-url "'+$base+'" --api-key "'+$key+'" --csv "'+$txtCsv.Text+'" '+$mode
  if($chkIgnore.Checked){ $args += ' --ignore-version' }
  if(-not [string]::IsNullOrWhiteSpace($txtPick.Text)){ $args += ' --only-ids "' + $txtPick.Text.Trim() + '"' }

  AppendLog("`r`n=== RUN $mode ===`r`n$py $args`r`n")
  SetRunning $true 'Running...'

  $p = New-Object System.Diagnostics.Process
  $p.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($py,$args)
  $p.StartInfo.WorkingDirectory=$dir
  $p.StartInfo.UseShellExecute=$false
  $p.StartInfo.CreateNoWindow=$true
  $p.StartInfo.RedirectStandardOutput=$true
  $p.StartInfo.RedirectStandardError=$true
  $p.EnableRaisingEvents=$true
  $script:proc=$p

  $sbLine = { if($EventArgs.Data){ AppendLog($EventArgs.Data + "`r`n") } }
  $script:evOut = Register-ObjectEvent -InputObject $p -EventName OutputDataReceived -Action $sbLine
  $script:evErr = Register-ObjectEvent -InputObject $p -EventName ErrorDataReceived  -Action $sbLine
  $script:evExit= Register-ObjectEvent -InputObject $p -EventName Exited -Action {
    $code=$Event.Sender.ExitCode
    AppendLog("=== EXIT $code ===`r`n")
    SetRunning $false ("Done (exit="+$code+")")
    CleanupProcess
  }

  $null=$p.Start(); $p.BeginOutputReadLine(); $p.BeginErrorReadLine()
}

$btnDry.Add_Click({ StartRun '--dry-run' })
$btnApply.Add_Click({ StartRun '--apply' })

# -------- Metadata tab (layout) --------
$panelMetaTop = New-Object System.Windows.Forms.Panel
$panelMetaTop.Dock='Top'
$panelMetaTop.Height=40
$tabMeta.Controls.Add($panelMetaTop)

$btnMetaLoad=New-Object System.Windows.Forms.Button; $btnMetaLoad.Text='Load'; $btnMetaLoad.SetBounds(10,8,100,26)
$lblMeta=New-Object System.Windows.Forms.Label; $lblMeta.Text=''; $lblMeta.SetBounds(130,12,1000,20)
$panelMetaTop.Controls.AddRange(@($btnMetaLoad,$lblMeta))

$splitMeta=New-Object System.Windows.Forms.SplitContainer
$splitMeta.Dock='Fill'
$splitMeta.Orientation='Vertical'
$splitMeta.SplitterDistance=320
$tabMeta.Controls.Add($splitMeta)

$lvProj=New-Object System.Windows.Forms.ListView
$lvProj.View='Details'; $lvProj.FullRowSelect=$true; $lvProj.GridLines=$true
$lvProj.Columns.Add('Projects',280)|Out-Null
$lvProj.Dock='Fill'
$splitMeta.Panel1.Controls.Add($lvProj)

$lvMeta=New-Object System.Windows.Forms.ListView
$lvMeta.View='Details'; $lvMeta.FullRowSelect=$true; $lvMeta.GridLines=$true
$lvMeta.Columns.Add('Field',320)|Out-Null
$lvMeta.Columns.Add('Info',820)|Out-Null
$lvMeta.Dock='Fill'
$splitMeta.Panel2.Controls.Add($lvMeta)

$script:MetaCache=$null
function AddMetaRow([string]$f,[string]$i){ $it=New-Object System.Windows.Forms.ListViewItem($f); $null=$it.SubItems.Add($i); $lvMeta.Items.Add($it)|Out-Null }

$btnMetaLoad.Add_Click({
  $lvProj.Items.Clear(); $lvMeta.Items.Clear()
  $base=NormalizeBaseUrl $txtUrl.Text; $key=$txtKey.Text
  if([string]::IsNullOrWhiteSpace($base) -or [string]::IsNullOrWhiteSpace($key)){ MsgE 'Base URL / API Key required'; return }

  $st=InvokeGet ($base+'/issue_statuses.json') $key
  $pr=InvokeGet ($base+'/enumerations/issue_priorities.json') $key
  $tr=InvokeGet ($base+'/trackers.json') $key
  $pj=InvokeGet ($base+'/projects.json?limit=100') $key
  $cf=InvokeGet ($base+'/custom_fields.json') $key

  $script:MetaCache=@{ base=$base; key=$key; custom_fields=$cf }

  if($pj -and $pj.projects){ foreach($p in $pj.projects){ $it=New-Object System.Windows.Forms.ListViewItem($p.name); $it.Tag=$p; $lvProj.Items.Add($it)|Out-Null } }
  $lblMeta.Text = ('projects=' + ($(if($pj){$pj.projects.Count}else{0})) + ', statuses=' + ($(if($st){$st.issue_statuses.Count}else{0})) + ', priorities=' + ($(if($pr){$pr.issue_priorities.Count}else{0})) + ', trackers=' + ($(if($tr){$tr.trackers.Count}else{0})) + ', custom_fields=' + ($(if($cf){$cf.custom_fields.Count}else{0})))

  if($st){ foreach($s in $st.issue_statuses){ AddMetaRow 'status' ($s.id.ToString()+':'+$s.name) } }
  if($pr){ foreach($p in $pr.issue_priorities){ AddMetaRow 'priority' ($p.id.ToString()+':'+$p.name) } }
  if($tr){ foreach($t in $tr.trackers){ AddMetaRow 'tracker' ($t.id.ToString()+':'+$t.name) } }

  if($cf){ foreach($c in $cf.custom_fields){ if($c.customized_type -ne 'issue'){continue}; $info='id='+$c.id+' type='+$c.field_format+' multiple='+$c.multiple; if($c.field_format -eq 'list' -and $c.possible_values){ $vals=@(); foreach($pv in $c.possible_values){$vals+=$pv.value}; if($vals.Count -gt 0){$info+=' values='+($vals -join ' | ')} } elseif($c.field_format -eq 'user'){ $info+=' (select from memberships)' }; AddMetaRow ('CF: '+$c.name) $info } }
})

$lvProj.add_SelectedIndexChanged({
  if($lvProj.SelectedItems.Count -le 0){ return }
  $p=$lvProj.SelectedItems[0].Tag
  $base=NormalizeBaseUrl $txtUrl.Text; $key=$txtKey.Text
  $lvMeta.Items.Clear()
  AddMetaRow 'project' ('name='+$p.name+' identifier='+$p.identifier+' id='+$p.id)

  if($p.identifier){
    $m=InvokeGet ($base+'/projects/'+$p.identifier+'/memberships.json?limit=100') $key
    if($m -and $m.memberships){ $names=@(); foreach($mm in $m.memberships){ if($mm.user){$names+=$mm.user.name}; if($mm.group){$names+=('[GROUP] '+$mm.group.name)} }; $names=$names|Sort-Object -Unique; AddMetaRow 'memberships' ($names -join ' | ') }
    $v=InvokeGet ($base+'/projects/'+$p.identifier+'/versions.json?limit=100') $key
    if($v -and $v.versions){ $vals=@(); foreach($vv in $v.versions){$vals+=($vv.id.ToString()+':'+$vv.name)}; AddMetaRow 'versions' ($vals -join ' | ') }
    $c=InvokeGet ($base+'/projects/'+$p.identifier+'/issue_categories.json') $key
    if($c -and $c.issue_categories){ $vals=@(); foreach($cc in $c.issue_categories){$vals+=($cc.id.ToString()+':'+$cc.name)}; AddMetaRow 'categories' ($vals -join ' | ') }
  }

  $cf = $script:MetaCache.custom_fields
  if($cf){ foreach($x in $cf.custom_fields){ if($x.customized_type -ne 'issue'){continue}; $info='id='+$x.id+' type='+$x.field_format+' multiple='+$x.multiple; if($x.field_format -eq 'list' -and $x.possible_values){ $vals=@(); foreach($pv in $x.possible_values){$vals+=$pv.value}; if($vals.Count -gt 0){$info+=' values='+($vals -join ' | ')} } elseif($x.field_format -eq 'user'){ $info+=' (select from memberships)' }; AddMetaRow ('CF: '+$x.name) $info } }
})

$form.add_FormClosed({ CleanupProcess })

if(-not [string]::IsNullOrWhiteSpace($txtCsv.Text) -and (Test-Path $txtCsv.Text)){
  LoadCsvToGrid $txtCsv.Text $grid | Out-Null
}

[void]$form.ShowDialog()
